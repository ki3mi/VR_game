using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR;

namespace AndesSim
{
    /// <summary>A persistent local conversation with the senior and a separate data terminal.</summary>
    [DefaultExecutionOrder(80)]
    [DisallowMultipleComponent]
    public sealed class MentorPresenter : MonoBehaviour
    {
        public event Action RestartRequested;
        public event Action HistoryClosed;

        public string MentorDisplayName => "Elliot Alderson";
        public bool IsTerminalOpen => terminalPanel != null && terminalPanel.gameObject.activeSelf;
        public bool IsMentorTyping { get; private set; }
        public bool AwaitingNextMessage { get; private set; }
        public int NextAcknowledgementCount { get; private set; }
        public int PendingMessageCount => incoming.Count + (IsMentorTyping ? 1 : 0);
        public bool IsReadyForAction => built && !IsMentorTyping && !AwaitingNextMessage && incoming.Count == 0;
        public bool CanExecuteQuery => IsTerminalOpen && runCommand != null && executeAuthorized && IsReadyForAction &&
            !MobileExperience.IsPaused && !confirmingRestart;
        public bool IsReading => modal || IsTerminalOpen || confirmingRestart || IsHistoryOpen;
        public bool BlocksWorldInput => IsReading;
        public bool PointerOverUI
        {
            get
            {
                if (root == null || !root.gameObject.activeInHierarchy) return false;
                Vector2 pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * .5f, Screen.height * .5f);
                return IsScreenPointOverUI(pointer);
            }
        }
        public bool IsHistoryOpen => historyPanel != null && historyPanel.gameObject.activeSelf;
        public bool IsScreenPointOverUI(Vector2 point) => Contains(dialogue, point) || Contains(objectives, point) ||
            Contains(terminalPanel, point) || Contains(historyPanel, point) || Contains(restartPanel, point);
        public int ChatMessageCount => history.Count;
        public int MessageCount => history.Count;
        public int PlayerMessageCount { get; private set; }
        public int ReceivedMessageCount { get; private set; }
        public int MentorMessageCount => ReceivedMessageCount;
        public string LastPlayerMessage { get; private set; } = string.Empty;
        public string LastDeliveredMessage { get; private set; } = string.Empty;
        public string LastMentorMessage => LastDeliveredMessage;
        public string ChatHistoryText => string.Join("\n\n", history);
        public string CurrentObjective => objectiveLabel != null ? objectiveLabel.text : string.Empty;
        public string CurrentMessage => requestedMessage;
        public string CurrentChapter { get; private set; } = string.Empty;
        public string CurrentTerminalOutput => outputLabel != null ? outputLabel.text : string.Empty;
        public string PrimaryActionLabel => primaryLabel != null ? primaryLabel.text : string.Empty;
        public RectTransform Root => root;
        public RectTransform UiRoot => root;
        public Color CommandTextColor => commandLabel != null ? commandLabel.color : Cyan;
        public Color OutputTextColor => outputLabel != null ? outputLabel.color : Green;
        public AudioSource ReceivedMessageAudioSource => notificationSource;
        public AudioClip ReceivedMessageAudioClip => notificationClip;
        public int NotificationSoundCount { get; private set; }

        static readonly Color Background = new Color(.018f, .032f, .047f, 1f);
        static readonly Color Surface = new Color(.040f, .064f, .085f, 1f);
        static readonly Color Muted = new Color(.58f, .70f, .78f, 1f);
        static readonly Color White = new Color(.93f, .97f, 1f, 1f);
        static readonly Color Mint = new Color(.43f, .98f, .78f, 1f);
        static readonly Color Cyan = new Color(.40f, .83f, 1f, 1f);
        static readonly Color Green = new Color(.66f, 1f, .71f, 1f);
        static readonly Color Coral = new Color(1f, .64f, .53f, 1f);
        const float DeliveryDelay = 1.2f;
        readonly Queue<(string text, bool authorizesQuery)> incoming = new Queue<(string, bool)>();
        readonly List<string> history = new List<string>();
        readonly List<Button> buttons = new List<Button>();
        readonly Dictionary<Button, bool> desiredEnabled = new Dictionary<Button, bool>();
        readonly HashSet<Button> waitsForMentor = new HashSet<Button>();
        Canvas canvas;
        Camera uiCamera;
        Font font;
        RectTransform root, safe, dialogue, objectives, terminalPanel, outputContent, dialogueContent, historyContent, historyPanel, restartPanel;
        ScrollRect dialogueScroll, outputScroll, historyScroll;
        Text statusLabel, objectiveHeading, objectiveLabel, evidenceLabel, typingLabel, primaryLabel, secondaryLabel;
        Text dialogueBody, outputLabel, commandLabel, terminalState;
        Button primaryButton, secondaryButton, confirmButton, cancelButton, executeButton;
        Action primaryAction, secondaryAction;
        Action runCommand;
        bool executeAuthorized;
        AudioSource notificationSource;
        AudioClip notificationClip;
        Coroutine delivery;
        string requestedMessage = string.Empty;
        string primaryCaption = string.Empty, secondaryCaption = string.Empty;
        bool modal, built, confirmingRestart;
        bool resetDialogueScroll, resetHistoryScroll;
        int historyRows;
        float writingBegan;
        int lastActionFrame = -1;

        void Awake() => EnsureBuilt();

        public void Show(string chapter, string objective, string message, string actionLabel, Action action,
            int currentStep, int totalSteps, bool modal = false)
        {
            EnsureBuilt();
            this.modal = modal;
            CurrentChapter = chapter ?? string.Empty;
            bool complete = totalSteps > 0 && currentStep == totalSteps;
            objectiveHeading.text = complete ? "CASO CERRADO" : "OBJETIVO ACTUAL";
            objectiveLabel.text = complete ? "Entrenamiento completado" : objective ?? string.Empty;
            SetMessage(message);
            SetActions(actionLabel, action);
        }

        public void SetMessage(string message)
        {
            EnsureBuilt();
            if (string.IsNullOrWhiteSpace(message)) return;
            // Blank lines separate authored sections. Single line breaks stay inside a section.
            foreach (var section in message.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(section)) EnqueueSection(section.Trim());
            }
        }

        void EnqueueSection(string text, bool authorizesQuery = false)
        {
            requestedMessage = text;
            incoming.Enqueue((text, authorizesQuery));
            if (delivery == null && !AwaitingNextMessage) delivery = StartCoroutine(DeliverMessages());
            RefreshActions();
        }

        public void CancelPendingMessages()
        {
            if (delivery != null) StopCoroutine(delivery);
            delivery = null;
            incoming.Clear();
            executeAuthorized = false;
            runCommand = null;
            IsMentorTyping = false;
            AwaitingNextMessage = false;
            if (built) { LayoutActions(); RefreshActions(); }
        }

        public void AddPlayerMessage(string message)
        {
            EnsureBuilt();
            if (string.IsNullOrWhiteSpace(message)) return;
            LastPlayerMessage = message;
            PlayerMessageCount++;
            AppendBubble(message, true, false);
        }

        public void AddSystemMessage(string message)
        {
            EnsureBuilt();
            if (!string.IsNullOrWhiteSpace(message)) AppendBubble(message, false, true);
        }

        public void SetStatus(string message)
        {
            EnsureBuilt();
            statusLabel.text = (message ?? string.Empty).Replace(" | ", "  ·  ");
        }

        public void SetPendingEvidence(IReadOnlyList<string> titles)
        {
            EnsureBuilt();
            bool hasEvidence = titles != null && titles.Count > 0;
            evidenceLabel.gameObject.SetActive(hasEvidence);
            evidenceLabel.text = hasEvidence ? "• " + string.Join("\n• ", titles) : string.Empty;
            float offset = hasEvidence ? titles.Count * 24 : 0;
            objectives.sizeDelta = new Vector2(objectives.sizeDelta.x, 137 + offset);
            statusLabel.rectTransform.anchoredPosition = new Vector2(16, -91 - offset);
        }

        public void SetActions(string primaryLabel, Action primary, string secondaryLabel = null, Action secondary = null)
        {
            EnsureBuilt();
            primaryCaption = primaryLabel ?? string.Empty;
            secondaryCaption = secondaryLabel ?? string.Empty;
            primaryAction = primary;
            secondaryAction = secondary;
            desiredEnabled[primaryButton] = primary != null;
            desiredEnabled[secondaryButton] = secondary != null;
            LayoutActions();
            RefreshActions();
        }

        public void SetTerminal(string command, string instruction, Action execute)
        {
            EnsureBuilt();
            commandLabel.text = "$ " + command;
            terminalState.text = "ESPERANDO A ELLIOT";
            outputLabel.text = string.Empty;
            outputLabel.color = Green;
            ScrollToStart(outputScroll);
            executeAuthorized = false;
            runCommand = execute;
            terminalPanel.gameObject.SetActive(true);
            EnqueueSection(instruction, authorizesQuery: true);
        }

        public void SetTerminalOutput(string output, bool success)
        {
            EnsureBuilt();
            outputLabel.text = output ?? string.Empty;
            outputLabel.color = success ? Green : Coral;
            terminalState.text = success ? "RESULTADO DISPONIBLE" : "ERROR DE CONSULTA";
            Canvas.ForceUpdateCanvases();
            ResizeScroll(outputScroll, outputContent, outputLabel);
            ScrollToStart(outputScroll);
        }

        public void CloseTerminal()
        {
            if (!IsTerminalOpen) return;
            executeAuthorized = false;
            runCommand = null;
            terminalPanel.gameObject.SetActive(false);
            ClearUISelection();
            RefreshActions();
        }

        IEnumerator DeliverMessages()
        {
            // Deliver one instruction. A queued instruction requires an explicit
            // acknowledgement, so it cannot replace the one the player is reading.
            if (incoming.Count == 0) yield break;
            var (message, authorizesQuery) = incoming.Dequeue();
            IsMentorTyping = true;
            writingBegan = Time.unscaledTime;
            LayoutActions();
            RefreshActions();
            while (MobileExperience.IsPaused || Time.unscaledTime - writingBegan < DeliveryDelay)
            {
                if (MobileExperience.IsPaused) writingBegan += Time.unscaledDeltaTime;
                yield return null;
            }
            LastDeliveredMessage = message;
            ReceivedMessageCount++;
            AppendBubble(message, false, false);
            dialogueBody.text = message;
            resetDialogueScroll = true;
            if (notificationSource != null && notificationClip != null)
            {
                notificationSource.PlayOneShot(notificationClip);
                NotificationSoundCount++;
            }
            IsMentorTyping = false;
            AwaitingNextMessage = incoming.Count > 0;
            delivery = null;
            if (authorizesQuery && IsTerminalOpen && runCommand != null)
            {
                executeAuthorized = true;
                terminalState.text = "LISTA PARA EJECUTAR";
            }
            LayoutActions();
            RefreshActions();
        }

        void InvokePrimaryAction()
        {
            if (!AwaitingNextMessage) { primaryAction?.Invoke(); return; }
            if (IsMentorTyping || incoming.Count == 0 || confirmingRestart) return;
            AwaitingNextMessage = false;
            AddPlayerMessage("Ok");
            NextAcknowledgementCount++;
            delivery = StartCoroutine(DeliverMessages());
            LayoutActions();
            RefreshActions();
        }

        void Update()
        {
            if (!built) return;

            typingLabel.text = IsMentorTyping
                ? "Elliot está escribiendo" + new string('.', 1 + Mathf.FloorToInt((Time.unscaledTime - writingBegan) * 6) % 3)
                : string.Empty;
            typingLabel.color = IsMentorTyping ? Mint : Muted;
            RefreshActions();
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (IsHistoryOpen) CloseHistory();
                else if (confirmingRestart) CancelRestart();
            }
            if (MobileExperience.Instance == null && !IsTerminalOpen && keyboard.spaceKey.wasPressedThisFrame)
            {
                Button button = ButtonAt(new Vector2(Screen.width * .5f, Screen.height * .5f));
                if (button != null) button.onClick.Invoke();
            }
        }

        void LateUpdate()
        {
            if (!built || !root.gameObject.activeInHierarchy) return;
            if (canvas.renderMode == RenderMode.WorldSpace && uiCamera == null)
            {
                uiCamera = Camera.main;
                AttachWorldCanvas();
            }
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) MobileExperience.FitSafeArea(safe);

            dialogue.gameObject.SetActive(!IsHistoryOpen);
            objectives.gameObject.SetActive(!IsHistoryOpen);
            if (dialogue.gameObject.activeSelf)
            {
                ResizeScroll(dialogueScroll, dialogueContent, dialogueBody);
                if (resetDialogueScroll) ScrollToStart(dialogueScroll);
                resetDialogueScroll = false;
            }
            if (IsTerminalOpen) ResizeScroll(outputScroll, outputContent, outputLabel);
            if (!IsHistoryOpen) return;
            if (resetHistoryScroll)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(historyContent);
                historyScroll.StopMovement();
                historyScroll.verticalNormalizedPosition = 0;
                resetHistoryScroll = false;
            }
        }

        void AppendBubble(string message, bool player, bool system)
        {
            history.Add((system ? "SISTEMA" : player ? "TÚ" : MentorDisplayName) + ": " + message);
        }

        public void SetHistoryVisible(bool visible)
        {
            EnsureBuilt();
            historyPanel.gameObject.SetActive(visible);
            dialogue.gameObject.SetActive(!visible);
            objectives.gameObject.SetActive(!visible);
            if (!visible) return;
            historyPanel.SetAsLastSibling();
            for (; historyRows < history.Count; historyRows++)
            {
                var row = Label(historyContent, history[historyRows], 0, 0, 100, 30, 21, White);
                row.verticalOverflow = VerticalWrapMode.Overflow;
            }
            resetHistoryScroll = true;
        }

        void CloseHistory()
        {
            SetHistoryVisible(false);
            HistoryClosed?.Invoke();
        }

        void RunCommand()
        {
            if (!CanExecuteQuery) return;
            Action execute = runCommand;
            runCommand = null;
            executeAuthorized = false;
            RefreshActions();
            execute();
        }

        public void RequestRestart()
        {
            if (confirmingRestart) return;
            ClearUISelection();
            confirmingRestart = true;
            restartPanel.SetAsLastSibling();
            restartPanel.gameObject.SetActive(true);
            LayoutActions();
        }

        void CancelRestart()
        {
            confirmingRestart = false;
            restartPanel.gameObject.SetActive(false);
            LayoutActions();
        }

        void ConfirmRestart()
        {
            confirmingRestart = false;
            restartPanel.gameObject.SetActive(false);
            dialogueBody.text = string.Empty;
            CloseTerminal();
            RestartRequested?.Invoke();
            LayoutActions();
        }

        public void ClearUISelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void EnsureBuilt()
        {
            if (built) return;
            built = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiCamera = Camera.main;
            var item = new GameObject("Mentor_Training_HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            item.transform.SetParent(transform, false);
            root = item.GetComponent<RectTransform>();
            canvas = item.GetComponent<Canvas>();
            canvas.sortingOrder = 400;
            canvas.worldCamera = uiCamera;
            var scaler = item.GetComponent<CanvasScaler>();
            if (MobileExperience.Instance == null && XRSettings.isDeviceActive && !Application.isEditor)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                root.sizeDelta = new Vector2(1280, 720);
                AttachWorldCanvas();
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
            if (EventSystem.current == null)
            {
                var events = new GameObject("Training_EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            safe = Box(root, "SafeArea", Color.clear);
            Stretch(safe);
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) MobileExperience.FitSafeArea(safe);
            BuildDialogue();
            BuildTerminal();
            BuildNotification();
            terminalPanel.gameObject.SetActive(false);
            LayoutActions();
            RefreshActions();
        }

        void BuildDialogue()
        {
            objectives = Box(safe, "Objectives", Background);
            Place(objectives, 18, 14, 485, 137);
            objectiveHeading = Label(objectives, "OBJETIVO ACTUAL", 16, 11, 445, 22, 14, Mint, FontStyle.Bold);
            objectiveLabel = Label(objectives, "", 16, 37, 451, 53, 21, White, FontStyle.Bold);
            evidenceLabel = Label(objectives, "", 16, 91, 451, 76, 18, White);
            evidenceLabel.name = "PendingEvidence";
            statusLabel = Label(objectives, "", 16, 91, 451, 38, 14, Muted);

            dialogue = Box(safe, "MentorDialogue", Background);
            dialogue.anchorMin = Vector2.zero;
            dialogue.anchorMax = Vector2.right;
            dialogue.offsetMin = new Vector2(18, 14);
            dialogue.offsetMax = new Vector2(-18, 210);

            var portrait = new GameObject("ElliotPortrait", typeof(RectTransform), typeof(DialoguePortrait)).GetComponent<RectTransform>();
            portrait.SetParent(dialogue, false);
            Place(portrait, 18, 24, 94, 94);
            var photo = portrait.GetComponent<DialoguePortrait>();
            photo.texture = Resources.Load<Texture2D>("ElliotPortrait");
            photo.raycastTarget = false;
            photo.color = photo.texture != null ? Color.white : Surface;

            var name = Label(dialogue, MentorDisplayName, 132, 17, 350, 30, 24, Mint, FontStyle.Bold);
            name.name = "DialogueSpeaker";
            var spoken = CreateScroll(dialogue, "CurrentDialogue", out _, out dialogueContent, false);
            Stretch(spoken);
            spoken.offsetMin = new Vector2(124, 34);
            spoken.offsetMax = new Vector2(-326, -51);
            dialogueScroll = spoken.GetComponent<ScrollRect>();
            dialogueBody = ScrollBody(dialogueContent, "DialogueBody", 20, White);
            typingLabel = Label(dialogue, "", 132, 170, 400, 21, 15, Mint);

            var actions = Box(dialogue, "DialogueActions", Surface);
            actions.anchorMin = new Vector2(1, 0);
            actions.anchorMax = Vector2.one;
            actions.offsetMin = new Vector2(-306, 10);
            actions.offsetMax = new Vector2(-10, -10);
            primaryButton = MakeButton(actions, "PrimaryAction", "", InvokePrimaryAction, true);
            primaryLabel = primaryButton.GetComponentInChildren<Text>();
            secondaryButton = MakeButton(actions, "SecondaryAction", "", () => secondaryAction?.Invoke(), false);
            secondaryLabel = secondaryButton.GetComponentInChildren<Text>();
            Place(secondaryButton.transform as RectTransform, 10, 94, 276, 52);

            restartPanel = Box(safe, "ConfirmRestartPanel", Background);
            Center(restartPanel, 580, 215);
            Label(restartPanel, "¿Reiniciar entrenamiento?", 22, 20, 536, 37, 24, White, FontStyle.Bold);
            Label(restartPanel, "Se reiniciarán los objetivos, las evidencias y las defensas. Podrás consultar el historial de esta sesión.",
                22, 67, 536, 61, 19, Muted);
            confirmButton = MakeButton(restartPanel, "ConfirmRestart", "Sí, reiniciar", ConfirmRestart, true, false);
            cancelButton = MakeButton(restartPanel, "CancelRestart", "Continuar sesión", CancelRestart, false, false);
            Place(confirmButton.transform as RectTransform, 22, 143, 254, 51);
            Place(cancelButton.transform as RectTransform, 305, 143, 254, 51);
            restartPanel.gameObject.SetActive(false);

            historyPanel = Box(safe, "SessionHistory", Background);
            Stretch(historyPanel);
            Label(historyPanel, "Historial de la sesión", 24, 24, 500, 40, 28, White, FontStyle.Bold);
            var back = MakeButton(historyPanel, "CloseHistory", "Volver al menú", CloseHistory, false, false);
            var backRect = back.transform as RectTransform;
            Place(backRect, -204, 17, 180, 48);
            backRect.anchorMin = backRect.anchorMax = Vector2.one;
            var historyRegion = CreateScroll(historyPanel, "HistoryScroll", out _, out historyContent, true);
            Stretch(historyRegion);
            historyRegion.offsetMin = new Vector2(24, 24);
            historyRegion.offsetMax = new Vector2(-24, -76);
            historyScroll = historyRegion.GetComponent<ScrollRect>();
            var historyLayout = historyContent.gameObject.AddComponent<VerticalLayoutGroup>();
            historyLayout.padding = new RectOffset(12, 12, 12, 12);
            historyLayout.spacing = 16;
            historyLayout.childControlWidth = historyLayout.childControlHeight = true;
            historyLayout.childForceExpandWidth = true;
            historyLayout.childForceExpandHeight = false;
            historyContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            historyPanel.gameObject.SetActive(false);
        }

        Text ScrollBody(RectTransform content, string name, int size, Color color)
        {
            var label = Label(content, "", 0, 0, 100, 100, size, color);
            label.name = name;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(label.rectTransform, 8, 4);
            return label;
        }

        static void ResizeScroll(ScrollRect scroll, RectTransform content, Text label)
        {
            float height = Mathf.Max(scroll.viewport.rect.height, label.preferredHeight + 12);
            if (!Mathf.Approximately(content.sizeDelta.y, height)) content.sizeDelta = new Vector2(0, height);
        }

        static void ScrollToStart(ScrollRect scroll)
        {
            scroll.StopMovement();
            scroll.content.anchoredPosition = Vector2.zero;
            scroll.verticalNormalizedPosition = 1;
        }

        static void Center(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        void BuildTerminal()
        {
            Font mono = Resources.Load<Font>("RobotoMono-Regular");
            terminalPanel = Box(safe, "GuidedTerminal", new Color(.16f, .22f, .27f));
            terminalPanel.anchorMin = new Vector2(.07f, 0);
            terminalPanel.anchorMax = new Vector2(.93f, 1);
            terminalPanel.offsetMin = new Vector2(0, 223);
            terminalPanel.offsetMax = new Vector2(0, -160);

            var bezel = Box(terminalPanel, "MonitorBezel", new Color(.052f, .072f, .092f));
            Stretch(bezel, 2, 2);
            var screen = Box(bezel, "MonitorScreen", new Color(.012f, .027f, .044f));
            Stretch(screen, 12, 10);
            var header = Box(screen, "MonitorHeader", new Color(.055f, .11f, .15f));
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = Vector2.one;
            header.offsetMin = new Vector2(0, -35);
            header.offsetMax = Vector2.zero;
            var title = Label(header, "TERMINAL ANDES", 15, 8, 280, 22, 16, Mint, FontStyle.Bold);
            title.font = mono != null ? mono : font;

            terminalState = Label(header, "", 0, 0, 100, 22, 14, White);
            Stretch(terminalState.rectTransform, 15, 7);
            terminalState.alignment = TextAnchor.MiddleRight;
            terminalState.font = title.font;

            var commandArea = Box(screen, "MonitorCommand", new Color(.028f, .065f, .097f));
            commandArea.anchorMin = new Vector2(0, 1);
            commandArea.anchorMax = Vector2.one;
            commandArea.offsetMin = new Vector2(14, -90);
            commandArea.offsetMax = new Vector2(-14, -49);
            commandLabel = Label(commandArea, "", 0, 0, 100, 30, 18, Cyan);
            Stretch(commandLabel.rectTransform, 10, 4);
            commandLabel.alignment = TextAnchor.MiddleLeft;
            commandLabel.font = title.font;

            var output = CreateScroll(screen, "CommandOutput", out _, out outputContent, false);
            Stretch(output);
            output.offsetMin = new Vector2(14, 62);
            output.offsetMax = new Vector2(-14, -101);
            outputScroll = output.GetComponent<ScrollRect>();
            outputLabel = ScrollBody(outputContent, "OutputLabel", 17, Green);
            outputLabel.font = title.font;

            executeButton = MakeButton(screen, "ExecuteCommand", "Ejecutar", RunCommand, true);
            var runRect = executeButton.transform as RectTransform;
            runRect.anchorMin = runRect.anchorMax = new Vector2(1, 0);
            runRect.pivot = new Vector2(1, 0);
            runRect.anchoredPosition = new Vector2(-14, 10);
            runRect.sizeDelta = new Vector2(162, 43);
        }

        void BuildNotification()
        {
            notificationSource = gameObject.AddComponent<AudioSource>();
            notificationSource.playOnAwake = false;
            notificationSource.spatialBlend = 0;
            notificationSource.volume = .15f;
            notificationSource.ignoreListenerPause = true;
            const int sampleRate = 22050;
            const int count = 2646;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float time = (float)i / sampleRate;
                float frequency = time < .055f ? 740 : 1046;
                float envelope = Mathf.Sin(Mathf.PI * i / count) * Mathf.Exp(-time * 12);
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * envelope * .5f;
            }
            notificationClip = AudioClip.Create("Senior_Message_Received", count, 1, sampleRate, false);
            notificationClip.SetData(samples, 0);
            notificationSource.clip = notificationClip;
        }

        void LayoutActions()
        {
            if (!built || primaryButton == null) return;
            bool showContinue = !IsMentorTyping && AwaitingNextMessage;
            bool showActions = IsReadyForAction;
            bool showSecondary = showActions && !string.IsNullOrWhiteSpace(secondaryCaption);
            primaryLabel.text = showContinue ? "Continuar" : primaryCaption;
            secondaryLabel.text = secondaryCaption;
            Place(primaryButton.transform as RectTransform, 10, showSecondary ? 28 : 57, 276, 56);
            primaryButton.gameObject.SetActive(!confirmingRestart &&
                (showContinue || showActions && !string.IsNullOrWhiteSpace(primaryCaption)));
            secondaryButton.gameObject.SetActive(!confirmingRestart && showSecondary);
        }

        void RefreshActions()
        {
            foreach (var pair in desiredEnabled)
            {
                if (pair.Key == null) continue;
                if (pair.Key == executeButton)
                {
                    pair.Key.interactable = CanExecuteQuery;
                    continue;
                }
                bool canUse = pair.Key == primaryButton && AwaitingNextMessage
                    ? !IsMentorTyping
                    : pair.Value && (!waitsForMentor.Contains(pair.Key) || IsReadyForAction);
                pair.Key.interactable = canUse && (!confirmingRestart || pair.Key == confirmButton || pair.Key == cancelButton);
            }
        }

        void AttachWorldCanvas()
        {
            if (uiCamera == null || canvas == null || canvas.renderMode != RenderMode.WorldSpace) return;
            root.SetParent(uiCamera.transform, false);
            root.localPosition = new Vector3(0, 0, 1.4f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * .0015f;
            canvas.worldCamera = uiCamera;
        }

        Button MakeButton(Transform parent, string name, string caption, Action callback, bool primary, bool wait = true)
        {
            var rect = Box(parent, name, primary ? new Color(.075f, .30f, .24f) : Surface);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.25f, 1.25f);
            colors.pressedColor = new Color(.7f, .9f, .95f);
            colors.disabledColor = new Color(.48f, .55f, .60f, .8f);
            button.colors = colors;
            var text = Label(rect, caption, 8, 3, 100, 30, 15, primary ? Mint : White, FontStyle.Bold);
            Stretch(text.rectTransform, 8, 3);
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 19;
            button.onClick.AddListener(() =>
            {
                if (!button.interactable || lastActionFrame == Time.frameCount) return;
                lastActionFrame = Time.frameCount;
                callback?.Invoke();
            });
            buttons.Add(button);
            desiredEnabled[button] = true;
            if (wait) waitsForMentor.Add(button);
            return button;
        }

        RectTransform CreateScroll(Transform parent, string name, out RectTransform viewport, out RectTransform content, bool alwaysShow)
        {
            var scroll = Box(parent, name, new Color(.009f, .022f, .033f));
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25;
            viewport = Box(scroll, "Viewport", new Color(1, 1, 1, .001f));
            viewport.GetComponent<Image>().raycastTarget = true;
            Stretch(viewport, 5, 5);
            viewport.offsetMax = new Vector2(-18, -5);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            var track = Box(scroll, "ReadingScrollbar", new Color(.10f, .18f, .22f));
            track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one; track.pivot = new Vector2(1, .5f);
            track.offsetMin = new Vector2(-10, 5); track.offsetMax = new Vector2(-3, -5);
            track.GetComponent<Image>().raycastTarget = true;
            var handle = Box(track, "Handle", Muted);
            Stretch(handle);
            handle.GetComponent<Image>().raycastTarget = true;
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = alwaysShow ? ScrollRect.ScrollbarVisibility.Permanent : ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        Button ButtonAt(Vector2 point)
        {
            for (int i = buttons.Count - 1; i >= 0; i--)
            {
                var button = buttons[i];
                if (button != null && button.interactable && button.gameObject.activeInHierarchy && Contains(button.transform as RectTransform, point)) return button;
            }
            return null;
        }

        bool Contains(RectTransform rect, Vector2 point) => rect != null && rect.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(rect, point, canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? uiCamera : null);

        static RectTransform Box(Transform parent, string name, Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            var image = item.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return item.GetComponent<RectTransform>();
        }

        Text Label(Transform parent, string value, float x, float y, float width, float height, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            var item = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            var label = item.GetComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            label.raycastTarget = false;
            Place(item.GetComponent<RectTransform>(), x, y, width, height);
            return label;
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static void Stretch(RectTransform rect, float horizontal = 0, float vertical = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }

        void OnDestroy()
        {
            CancelPendingMessages();
            if (root != null) Destroy(root.gameObject);
            if (notificationClip != null) Destroy(notificationClip);
        }
    }

    // Circular UV mesh keeps the original square artwork reusable.
    public sealed class DialoguePortrait : RawImage
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            mesh.AddVert(rect.center, color, new Vector2(.5f, .5f));
            const int segments = 64;
            for (int segment = 0; segment <= segments; segment++)
            {
                float angle = segment * 2 * Mathf.PI / segments;
                var uv = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .5f + Vector2.one * .5f;
                mesh.AddVert(new Vector2(rect.xMin + uv.x * rect.width, rect.yMin + uv.y * rect.height), color, uv);
                if (segment > 0) mesh.AddTriangle(0, segment, segment + 1);
            }
        }
    }
}
