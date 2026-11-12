using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public event Action HelpRequested;
        public event Action RepeatRequested;
        public event Action RestartRequested;
        public event Action TerminalClosed;

        public string MentorDisplayName => "Elliot Alderson";
        public bool IsTerminalOpen => terminalPanel != null && terminalPanel.gameObject.activeSelf;
        public bool IsMentorTyping { get; private set; }
        public bool AwaitingNextMessage { get; private set; }
        public int NextAcknowledgementCount { get; private set; }
        public int PendingMessageCount => incoming.Count + (IsMentorTyping ? 1 : 0);
        public bool IsReadyForAction => built && !IsMentorTyping && !AwaitingNextMessage && incoming.Count == 0;
        public bool IsTyping => commandInput != null && commandInput.isFocused;
        public bool IsReading => modal || IsTerminalOpen || confirmingRestart;
        public bool BlocksWorldInput => IsReading || IsTyping;
        public bool PointerOverUI
        {
            get
            {
                if (root == null || !root.gameObject.activeInHierarchy) return false;
                Vector2 pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * .5f, Screen.height * .5f);
                return Contains(phone, pointer) || Contains(terminalPanel, pointer);
            }
        }
        public bool IsScreenPointOverUI(Vector2 point) => Contains(phone, point) || Contains(terminalPanel, point);
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
        public string CurrentChapter => chapterLabel != null ? chapterLabel.text : string.Empty;
        public string CurrentTerminalOutput => outputLabel != null ? outputLabel.text : string.Empty;
        public string OriginalTerminalExplanation => terminalExplanationOriginal;
        public string PrimaryActionLabel => primaryLabel != null ? primaryLabel.text : string.Empty;
        public InputField CommandInput => commandInput;
        public RectTransform Root => root;
        public RectTransform UiRoot => root;
        public RectTransform PhoneRoot => phone;
        public ScrollRect ChatScroll => chatScroll;
        public bool IsFollowingLatest => !reviewingHistory;
        public Color CommandTextColor => commandInput != null ? commandInput.textComponent.color : Cyan;
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
        readonly Queue<string> incoming = new Queue<string>();
        readonly List<string> history = new List<string>();
        readonly List<Button> buttons = new List<Button>();
        readonly Dictionary<Button, bool> desiredEnabled = new Dictionary<Button, bool>();
        readonly HashSet<Button> waitsForMentor = new HashSet<Button>();
        Canvas canvas;
        Camera uiCamera;
        Font font, commandFont;
        RectTransform root, phone, screen, terminalPanel, chatContent, chatViewport, outputContent, outputScroll, progressFill;
        ScrollRect chatScroll;
        Text statusLabel, chapterLabel, objectiveLabel, stepLabel, worldHint, typingLabel, primaryLabel, secondaryLabel, terminalContinueLabel, outputLabel, terminalTitle;
        Button primaryButton, secondaryButton, terminalContinueButton, latestButton, confirmButton, cancelButton;
        InputField commandInput;
        Action primaryAction, secondaryAction;
        Action<string> runCommand;
        AudioSource notificationSource;
        AudioClip notificationClip;
        Coroutine delivery;
        string requestedMessage = string.Empty, exampleCommand = string.Empty, terminalExplanationOriginal = string.Empty;
        string primaryCaption = string.Empty, secondaryCaption = string.Empty;
        bool modal, built, confirmingRestart;
        bool followChat = true;
        float followChatPosition;
        int scrollAfterLayoutFrames;
        bool reviewingHistory, trackingHistoryDrag;
        Vector2 historyDragStart;
        float lastRunTime = -1, writingBegan;
        int lastActionFrame = -1;

        void Awake() => EnsureBuilt();

        public void Show(string chapter, string objective, string message, string actionLabel, Action action,
            int currentStep, int totalSteps, bool modal = false)
        {
            EnsureBuilt();
            this.modal = modal;
            chapterLabel.text = chapter ?? string.Empty;
            objectiveLabel.text = objective ?? string.Empty;
            stepLabel.text = $"OBJETIVO {Mathf.Clamp(currentStep, 1, Mathf.Max(1, totalSteps)):00} / {Mathf.Max(1, totalSteps):00}";
            progressFill.anchorMax = new Vector2(totalSteps > 0 ? Mathf.Clamp01((float)currentStep / totalSteps) : 0, 1);
            SetMessage(message);
            SetActions(actionLabel, action);
        }

        public void SetMessage(string message)
        {
            EnsureBuilt();
            requestedMessage = message ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestedMessage)) return;
            incoming.Enqueue(requestedMessage);
            if (delivery == null && !AwaitingNextMessage) delivery = StartCoroutine(DeliverMessages());
            RefreshActions();
        }

        public void CancelPendingMessages()
        {
            if (delivery != null) StopCoroutine(delivery);
            delivery = null;
            incoming.Clear();
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

        public void SetWorldHint(string message)
        {
            EnsureBuilt();
            worldHint.text = message ?? string.Empty;
        }

        public void SetActions(string primaryLabel, Action primary, string secondaryLabel = null, Action secondary = null)
        {
            EnsureBuilt();
            primaryCaption = primaryLabel ?? string.Empty;
            secondaryCaption = secondaryLabel ?? string.Empty;
            primaryAction = primary;
            secondaryAction = secondary;
            desiredEnabled[primaryButton] = desiredEnabled[terminalContinueButton] = primary != null;
            desiredEnabled[secondaryButton] = secondary != null;
            LayoutActions();
            RefreshActions();
        }

        public void SetTerminal(string title, string explanation, string commandExample, Action<string> onRun)
        {
            EnsureBuilt();
            terminalTitle.text = title ?? "TERMINAL ANDES / LABORATORIO";
            terminalExplanationOriginal = explanation ?? string.Empty;
            exampleCommand = commandExample ?? string.Empty;
            commandInput.text = exampleCommand;
            runCommand = onRun;
            SetTerminalOutput("ANDES LAB / SESSION CONNECTED\nstatus: READY\nresult: —", true);
            terminalPanel.gameObject.SetActive(true);
            if (!string.IsNullOrWhiteSpace(terminalExplanationOriginal)) SetMessage(terminalExplanationOriginal);
            LayoutActions();
        }

        public void SetTerminalOutput(string output, bool success)
        {
            EnsureBuilt();
            outputLabel.text = output ?? string.Empty;
            outputLabel.color = success ? Green : Coral;
            Canvas.ForceUpdateCanvases();
            outputContent.sizeDelta = new Vector2(0, Mathf.Max(outputScroll.rect.height - 12, outputLabel.preferredHeight + 12));
            outputContent.anchoredPosition = Vector2.zero;
            outputScroll.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
        }

        public void SetTerminalVisible(bool visible)
        {
            EnsureBuilt();
            terminalPanel.gameObject.SetActive(visible);
            if (!visible) ReleaseCommandFocus();
            LayoutActions();
        }

        public void CloseTerminal()
        {
            if (!IsTerminalOpen) return;
            SetTerminalVisible(false);
            TerminalClosed?.Invoke();
        }

        IEnumerator DeliverMessages()
        {
            // Deliver one instruction. A queued instruction requires an explicit
            // acknowledgement, so it cannot replace the one the player is reading.
            if (incoming.Count == 0) yield break;
            string message = incoming.Dequeue();
            IsMentorTyping = true;
            writingBegan = Time.unscaledTime;
            RefreshActions();
            while (MobileExperience.IsPaused || Time.unscaledTime - writingBegan < DeliveryDelay)
            {
                if (MobileExperience.IsPaused) writingBegan += Time.unscaledDeltaTime;
                yield return null;
            }
            LastDeliveredMessage = message;
            ReceivedMessageCount++;
            AppendBubble(message, false, false);
            if (notificationSource != null && notificationClip != null)
            {
                notificationSource.PlayOneShot(notificationClip);
                NotificationSoundCount++;
            }
            IsMentorTyping = false;
            AwaitingNextMessage = incoming.Count > 0;
            delivery = null;
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
            DetectHistoryGesture();
            typingLabel.text = IsMentorTyping
                ? "Elliot está escribiendo" + new string('.', 1 + Mathf.FloorToInt((Time.unscaledTime - writingBegan) * 6) % 3)
                : AwaitingNextMessage ? "Pulsa Siguiente para recibir la otra indicación"
                : "Mensajes locales · historial de la sesión";
            typingLabel.color = IsMentorTyping ? Mint : Muted;
            RefreshActions();
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (confirmingRestart) CancelRestart();
                else if (IsTerminalOpen) CloseTerminal();
            }
            if (IsTerminalOpen && IsTyping && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) RunCommand();
            else if (!IsTyping && keyboard.spaceKey.wasPressedThisFrame)
            {
                Button button = ButtonAt(new Vector2(Screen.width * .5f, Screen.height * .5f));
                if (button != null) button.onClick.Invoke();
            }
        }

        void LateUpdate()
        {
            if (!built) return;
            if (canvas.renderMode == RenderMode.WorldSpace && uiCamera == null)
            {
                uiCamera = Camera.main;
                AttachWorldCanvas();
            }
            if (followChat && chatScroll != null)
            {
                // Scroll after all row sizes are settled, and repeat on the next
                // frame so ScrollRect cannot restore an older layout position.
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
                ScrollToLatestRow();
                if (--scrollAfterLayoutFrames <= 0) followChat = false;
            }
        }

        void AppendBubble(string message, bool player, bool system)
        {
            history.Add((system ? "SISTEMA" : player ? "TÚ" : MentorDisplayName) + ": " + message);
            float rowWidth = Mathf.Max(300, chatViewport.rect.width - 8);
            float bubbleWidth = system ? rowWidth : rowWidth - (player ? 22 : 10);
            float textWidth = bubbleWidth - 22;
            int fontSize = system ? 12 : 16;
            var row = new GameObject("ChatMessage_" + history.Count.ToString("0000"), typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(chatContent, false);
            var rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = system ? 35 : 59;
            var bubble = Box(row.transform, system ? "SessionNotice" : player ? "PlayerBubble" : "MentorBubble",
                system ? new Color(.10f, .14f, .17f) : player ? new Color(.065f, .24f, .34f) : new Color(.075f, .14f, .17f));
            bubble.anchorMin = bubble.anchorMax = player ? Vector2.one : new Vector2(0, 1);
            bubble.pivot = player ? Vector2.one : new Vector2(0, 1);
            bubble.anchoredPosition = Vector2.zero;
            bubble.sizeDelta = new Vector2(bubbleWidth, rowLayout.preferredHeight);
            if (!system)
                Label(bubble, player ? "TÚ / INGENIERO EN ENTRENAMIENTO" : "ELLIOT ALDERSON", 11, 9, textWidth, 15, 10, player ? Cyan : Mint, FontStyle.Bold);
            float bodyTop = system ? 8 : 30;
            var body = Label(bubble, message, 11, bodyTop, textWidth, 1, fontSize, system ? Muted : White);
            body.name = "ChatMessageBody";
            body.verticalOverflow = VerticalWrapMode.Overflow;
            // Text.preferredHeight uses the actual Canvas scale, pixel-adjusted
            // width and font settings that render this label. Measuring a separate
            // TextGenerator can omit a wrapped line at non-integer canvas scales.
            Canvas.ForceUpdateCanvases();
            float bodyHeight = Mathf.Ceil(body.preferredHeight) + 6;
            body.rectTransform.sizeDelta = new Vector2(textWidth, bodyHeight);
            float height = Mathf.Max(system ? 35 : 59, bodyTop + bodyHeight + 10);
            rowLayout.preferredHeight = height;
            bubble.sizeDelta = new Vector2(bubbleWidth, height);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
            // Every new message takes focus, even after the player browsed history.
            // Previously delivered messages remain available by scrolling up.
            reviewingHistory = false;
            trackingHistoryDrag = false;
            chatScroll.StopMovement();
            ScrollToLatestRow();
            scrollAfterLayoutFrames = 2;
            followChat = true;
        }

        void JumpToLatest()
        {
            reviewingHistory = false;
            trackingHistoryDrag = false;
            Canvas.ForceUpdateCanvases();
            ScrollToLatestRow();
            scrollAfterLayoutFrames = 2;
            followChat = true;
        }

        void ScrollToLatestRow()
        {
            var last = chatContent.childCount > 0 ? chatContent.GetChild(chatContent.childCount - 1) as RectTransform : null;
            if (last != null && last.rect.height > chatViewport.rect.height)
            {
                float top = -last.anchoredPosition.y - last.rect.height * (1 - last.pivot.y);
                float scrollable = Mathf.Max(1, chatContent.rect.height - chatViewport.rect.height);
                followChatPosition = Mathf.Clamp01(1 - Mathf.Max(0, top) / scrollable);
            }
            else followChatPosition = 0;
            chatScroll.verticalNormalizedPosition = followChatPosition;
            chatScroll.velocity = Vector2.zero;
        }

        void DetectHistoryGesture()
        {
            var mouse = Mouse.current;
            if (mouse == null || chatScroll == null) return;
            Vector2 pointer = mouse.position.ReadValue();
            bool overHistory = Contains(chatScroll.transform as RectTransform, pointer);
            bool scroll = overHistory && mouse.scroll.ReadValue().sqrMagnitude > .01f;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                trackingHistoryDrag = overHistory;
                historyDragStart = pointer;
                if (overHistory && chatScroll.verticalScrollbar != null &&
                    Contains(chatScroll.verticalScrollbar.transform as RectTransform, pointer)) scroll = true;
            }
            if (trackingHistoryDrag && mouse.leftButton.isPressed && Vector2.Distance(pointer, historyDragStart) > 3) scroll = true;
            if (mouse.leftButton.wasReleasedThisFrame) trackingHistoryDrag = false;
            if (!scroll) return;
            // A generated scroll position is never interpreted as a user gesture.
            // In particular, aligning a long message to its first line must keep
            // subsequent incoming messages following the active conversation.
            reviewingHistory = true;
            followChat = false;
        }

        void RunCommand()
        {
            if (!IsTerminalOpen || !IsReadyForAction || runCommand == null || Time.unscaledTime - lastRunTime < .18f) return;
            lastRunTime = Time.unscaledTime;
            runCommand(commandInput.text);
        }

        void RequestRestart()
        {
            if (confirmingRestart) return;
            ReleaseCommandFocus();
            confirmingRestart = true;
            AddSystemMessage("¿Comenzar un entrenamiento nuevo? Se reiniciarán los objetivos, controles, alarma y resultados. Este historial de conversación seguirá disponible.");
            JumpToLatest();
            LayoutActions();
        }

        void CancelRestart()
        {
            confirmingRestart = false;
            AddSystemMessage("Continúas en el entrenamiento actual.");
            LayoutActions();
        }

        void ConfirmRestart()
        {
            confirmingRestart = false;
            SetTerminalVisible(false);
            RestartRequested?.Invoke();
            LayoutActions();
        }

        void ReleaseCommandFocus()
        {
            commandInput?.DeactivateInputField();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void EnsureBuilt()
        {
            if (built) return;
            built = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            commandFont = font;
            uiCamera = Camera.main;
            var item = new GameObject("Mentor_Training_HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            item.transform.SetParent(transform, false);
            root = item.GetComponent<RectTransform>();
            canvas = item.GetComponent<Canvas>();
            canvas.sortingOrder = 400;
            canvas.worldCamera = uiCamera;
            var scaler = item.GetComponent<CanvasScaler>();
            if (XRSettings.isDeviceActive && !Application.isEditor)
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
            var brand = Box(root, "TrainingBrand", Background);
            Place(brand, 24, 20, 810, 50);
            Label(brand, "ANDES-SIM / CENTRO DE DATOS", 16, 14, 778, 25, 20, Mint, FontStyle.Bold);
            BuildPhone();
            BuildTerminal();
            BuildNotification();
            terminalPanel.gameObject.SetActive(false);
            LayoutActions();
            RefreshActions();
        }

        void BuildPhone()
        {
            phone = Box(root, "MentorPhone", new Color(.004f, .009f, .015f));
            phone.anchorMin = phone.anchorMax = Vector2.one;
            phone.pivot = Vector2.one;
            phone.anchoredPosition = new Vector2(-20, -20);
            phone.sizeDelta = new Vector2(395, 680);
            var outline = phone.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.18f, .29f, .34f);
            outline.effectDistance = new Vector2(2, -2);
            screen = Box(phone, "PhoneScreen", Background);
            Place(screen, 8, 8, 379, 664);
            Label(screen, "ANDES-SIM  /  CHAT LOCAL", 18, 12, 242, 18, 10, Muted, FontStyle.Bold);
            var restart = MakeButton(screen, "RestartTraining", "Reiniciar", RequestRestart, false, false);
            Place(restart.transform as RectTransform, 284, 8, 79, 25);
            Label(screen, MentorDisplayName, 18, 33, 343, 25, 20, White, FontStyle.Bold);
            Label(screen, "● Conectado · Ingeniero senior de ciberseguridad", 18, 61, 343, 15, 10, Mint);
            statusLabel = Label(screen, "", 18, 81, 343, 26, 10, Muted);
            statusLabel.resizeTextForBestFit = true; statusLabel.resizeTextMinSize = 9; statusLabel.resizeTextMaxSize = 10;
            chapterLabel = Label(screen, "", 18, 112, 241, 15, 10, Mint, FontStyle.Bold);
            chapterLabel.resizeTextForBestFit = true; chapterLabel.resizeTextMinSize = 9; chapterLabel.resizeTextMaxSize = 10;
            objectiveLabel = Label(screen, "", 18, 134, 343, 43, 16, White, FontStyle.Bold);
            objectiveLabel.resizeTextForBestFit = true; objectiveLabel.resizeTextMinSize = 13; objectiveLabel.resizeTextMaxSize = 16;
            worldHint = Label(screen, "", 18, 183, 343, 24, 10, Cyan);
            worldHint.resizeTextForBestFit = true; worldHint.resizeTextMinSize = 9; worldHint.resizeTextMaxSize = 10;
            stepLabel = Label(screen, "", 265, 112, 96, 15, 9, Muted, FontStyle.Bold);
            var track = Box(screen, "ProgressTrack", new Color(.12f, .21f, .26f));
            Place(track, 18, 212, 343, 3);
            progressFill = Box(track, "ProgressFill", Mint);
            progressFill.anchorMin = Vector2.zero; progressFill.anchorMax = new Vector2(0, 1);
            progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
            var scroll = CreateScroll(screen, "ChatHistory", out chatViewport, out chatContent, true);
            Place(scroll, 14, 223, 351, 291);
            chatScroll = scroll.GetComponent<ScrollRect>();
            var layout = chatContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(2, 2, 5, 5);
            layout.spacing = 9;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = chatContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            typingLabel = Label(screen, "", 18, 522, 225, 19, 11, Muted);
            latestButton = MakeButton(screen, "LatestMessages", "Ver últimos ↓", JumpToLatest, false, false);
            Place(latestButton.transform as RectTransform, 250, 519, 111, 24);
            primaryButton = MakeButton(screen, "PrimaryAction", "", InvokePrimaryAction, true);
            primaryLabel = primaryButton.GetComponentInChildren<Text>();
            terminalContinueButton = MakeButton(screen, "TerminalContinue", "", InvokePrimaryAction, true);
            terminalContinueLabel = terminalContinueButton.GetComponentInChildren<Text>();
            secondaryButton = MakeButton(screen, "SecondaryAction", "", () => secondaryAction?.Invoke(), false);
            secondaryLabel = secondaryButton.GetComponentInChildren<Text>();
            var help = MakeButton(screen, "SeniorHelp", "Guíame", () => HelpRequested?.Invoke(), false);
            Place(help.transform as RectTransform, 18, 614, 166, 32);
            var repeat = MakeButton(screen, "SeniorRepeat", "Repetir", () => RepeatRequested?.Invoke(), false);
            Place(repeat.transform as RectTransform, 195, 614, 166, 32);
            confirmButton = MakeButton(screen, "ConfirmRestart", "Sí, reiniciar", ConfirmRestart, true, false);
            cancelButton = MakeButton(screen, "CancelRestart", "Continuar sesión", CancelRestart, false, false);
            Place(confirmButton.transform as RectTransform, 18, 550, 166, 52);
            Place(cancelButton.transform as RectTransform, 195, 550, 166, 52);
            var home = Box(phone, "HomeIndicator", Muted);
            Place(home, 151, 674, 93, 2);
        }

        void BuildTerminal()
        {
            terminalPanel = Box(root, "GuidedTerminal", Background);
            Place(terminalPanel, 24, 88, 810, 600);
            terminalTitle = Label(terminalPanel, "TERMINAL ANDES / LABORATORIO", 22, 18, 766, 50, 22, Mint, FontStyle.Bold);
            Label(terminalPanel, "COMANDO SIMULADO", 22, 80, 766, 22, 12, Cyan, FontStyle.Bold);
            var field = Box(terminalPanel, "CommandInput", new Color(.009f, .022f, .036f));
            Place(field, 22, 110, 766, 58);
            field.GetComponent<Image>().raycastTarget = true;
            commandInput = field.gameObject.AddComponent<InputField>();
            var text = Label(field, "", 13, 14, 740, 32, 18, Cyan);
            text.font = commandFont;
            commandInput.textComponent = text;
            commandInput.lineType = InputField.LineType.SingleLine;
            commandInput.characterLimit = 180;
            commandInput.customCaretColor = true;
            commandInput.caretColor = Cyan;
            commandInput.selectionColor = new Color(.10f, .36f, .48f, .8f);
            var run = MakeButton(terminalPanel, "ExecuteCommand", "Ejecutar", RunCommand, true);
            Place(run.transform as RectTransform, 564, 183, 224, 43);
            var example = MakeButton(terminalPanel, "UseCommandExample", "Usar ejemplo", () => { commandInput.text = exampleCommand; commandInput.ActivateInputField(); }, false, false);
            Place(example.transform as RectTransform, 22, 183, 230, 43);
            Label(terminalPanel, "SALIDA / DATOS DEL ESCENARIO", 22, 247, 766, 23, 12, Green, FontStyle.Bold);
            outputScroll = CreateScroll(terminalPanel, "CommandOutput", out _, out outputContent, false);
            Place(outputScroll, 22, 280, 766, 230);
            outputLabel = Label(outputContent, "", 0, 0, 100, 100, 17, Green);
            outputLabel.name = "OutputLabel";
            outputLabel.font = commandFont;
            outputLabel.supportRichText = false;
            outputLabel.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(outputLabel.rectTransform);
            var back = MakeButton(terminalPanel, "CloseTerminal", "Volver a la sala", CloseTerminal, false, false);
            Place(back.transform as RectTransform, 22, 533, 230, 43);
            var help = MakeButton(terminalPanel, "TerminalHelp", "Consultar al senior →", () => HelpRequested?.Invoke(), false);
            Place(help.transform as RectTransform, 269, 533, 260, 43);
            Label(terminalPanel, "DATOS FICTICIOS\nEjecución local", 565, 537, 223, 39, 12, Muted);
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
            if (!built || primaryButton == null || terminalContinueButton == null) return;
            primaryLabel.text = terminalContinueLabel.text = AwaitingNextMessage ? "Siguiente" : primaryCaption;
            secondaryLabel.text = secondaryCaption;
            bool hasPrimary = AwaitingNextMessage || !string.IsNullOrWhiteSpace(primaryCaption);
            bool hasSecondary = !AwaitingNextMessage && !string.IsNullOrWhiteSpace(secondaryCaption);
            primaryButton.gameObject.SetActive(!confirmingRestart && !IsTerminalOpen && hasPrimary);
            terminalContinueButton.gameObject.SetActive(!confirmingRestart && IsTerminalOpen && hasPrimary);
            secondaryButton.gameObject.SetActive(!confirmingRestart && hasSecondary);
            float primaryWidth = hasSecondary ? 166 : 343;
            Place(primaryButton.transform as RectTransform, 18, 550, primaryWidth, 52);
            Place(terminalContinueButton.transform as RectTransform, 18, 550, primaryWidth, 52);
            Place(secondaryButton.transform as RectTransform, 195, 550, 166, 52);
            confirmButton.gameObject.SetActive(confirmingRestart);
            cancelButton.gameObject.SetActive(confirmingRestart);
        }

        void RefreshActions()
        {
            foreach (var pair in desiredEnabled)
            {
                if (pair.Key == null) continue;
                pair.Key.interactable = pair.Value && (!waitsForMentor.Contains(pair.Key) || IsReadyForAction) &&
                    (!confirmingRestart || pair.Key == confirmButton || pair.Key == cancelButton || pair.Key == latestButton);
                if ((pair.Key == primaryButton || pair.Key == terminalContinueButton) && AwaitingNextMessage)
                    pair.Key.interactable = !IsMentorTyping && !confirmingRestart;
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
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 15;
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
}
