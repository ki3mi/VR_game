using System;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AndesSim
{
    /// <summary>Camera-centered gaze and discrete exploration, independent of scenario logic.</summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ExplorationController : MonoBehaviour
    {
        public Camera viewCamera;
        public XROrigin xrOrigin;
        public float interactionDistance = 35f;
        public float simulatorEyeHeight = 1.65f;
        public LayerMask raycastMask = ~0;
        public string initialStationId = "TP_Entrada";
        [TextArea] public string simulatorInstructions = "CÁMARA: joystick o giroscopio\nToca los equipos o usa Inspeccionar\nNavegación por teletransporte";

        public ExplorationTarget CurrentTarget { get; private set; }
        public string LastSelectedId { get; private set; }
        public bool PanelVisible => inspectionPanel != null && inspectionPanel.activeSelf;
        public int VisitedCount => visitedStations.Count;
        public int InspectedCount => inspectedObjects.Count;
        public int TeleportsPerformed { get; private set; }
        public Vector3 LastTeleportPosition { get; private set; }
        public RectTransform HudRoot { get; private set; }
        public MentorPresenter NarrativePresenter { get; set; }
        public bool GuidedMode { get; private set; }
        public event Action<ExplorationTarget> TargetActivated;
        public event Action InspectionClosed;

        public readonly struct PanelAction
        {
            public readonly string Label;
            public readonly Action Invoke;

            public PanelAction(string label, Action invoke)
            {
                Label = label;
                Invoke = invoke;
            }
        }

        readonly HashSet<string> visitedStations = new HashSet<string>();
        readonly HashSet<string> inspectedObjects = new HashSet<string>();
        GameObject inspectionPanel;
        TextMeshProUGUI prompt, progress, inspectionTitle, inspectionBody;
        static readonly Key[] ActionKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };
        readonly RectTransform[] actionButtons = new RectTransform[3];
        readonly TextMeshProUGUI[] actionLabels = new TextMeshProUGUI[3];
        readonly PanelAction[] panelActions = new PanelAction[3];
        Image reticle, fade;
        TMP_FontAsset font;
        float inputCooldown;

        void Start()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (xrOrigin == null && viewCamera != null)
                xrOrigin = viewCamera.GetComponentInParent<XROrigin>();
            if (viewCamera == null || xrOrigin == null)
            {
                Debug.LogError("[ANDES-SIM] ExplorationController requiere Camera XR y XROrigin.", this);
                enabled = false;
                return;
            }
#if UNITY_EDITOR
            // XRI 3.6 can restore an uninitialized AR Simulation offset on scene
            // load when AR Foundation is installed alongside another XR loader.
            // Restore this scene's device-origin pose after that callback.
            if (FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator>() != null &&
                xrOrigin.CameraFloorOffsetObject != null)
            {
                xrOrigin.CameraYOffset = simulatorEyeHeight;
                xrOrigin.CameraFloorOffsetObject.transform.localPosition = Vector3.up * simulatorEyeHeight;
            }
#endif
            visitedStations.Add(initialStationId);
            BuildInterface();
            UpdateProgress();
        }

        void Update()
        {
            if (MobileExperience.IsPaused) return;
            if (viewCamera == null) return;
            inputCooldown = Mathf.Max(0, inputCooldown - Time.unscaledDeltaTime);
            if (fade != null)
            {
                Color c = fade.color;
                c.a = Mathf.MoveTowards(c.a, 0, Time.unscaledDeltaTime * 4);
                fade.color = c;
            }

            RefreshGaze();
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame && (NarrativePresenter == null ||
                    NarrativePresenter.IsReadyForAction && !NarrativePresenter.BlocksWorldInput &&
                    !NarrativePresenter.IsScreenPointOverUI(touch.position.ReadValue())) &&
                    !PointerHitsInterface(touch.position.ReadValue()))
                    SelectAtScreenPoint(touch.position.ReadValue());
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                PointerHitsInterface(Mouse.current.position.ReadValue())) return;
            // Narrative controls own the click/space while reading or typing.
            // Checking geometry avoids the one-frame delay of EventSystem selection.
            if (NarrativePresenter != null &&
                (NarrativePresenter.BlocksWorldInput || !NarrativePresenter.IsReadyForAction || NarrativePresenter.PointerOverUI &&
                    Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            bool selected = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                            (mouse != null && mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.isPressed);
            bool close = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;

            if (PanelVisible)
            {
                if (close) CloseInspection();
                else if (inputCooldown <= 0)
                {
                    bool click = mouse != null && mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.isPressed;
                    Vector2 pointer = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
                    if (!TryUsePanelAction(keyboard, click, pointer) && selected) CloseInspection();
                }
            }
            else if (selected && inputCooldown <= 0) ActivateCurrentTarget();
        }

        public void RefreshGaze()
        {
            if (viewCamera == null) return;
            ExplorationTarget next = null;
            if (!PanelVisible && Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                    out RaycastHit hit, interactionDistance, raycastMask, QueryTriggerInteraction.Collide))
                next = hit.collider.GetComponentInParent<ExplorationTarget>();

            if (next != CurrentTarget)
            {
                if (CurrentTarget != null) CurrentTarget.SetHighlighted(false);
                CurrentTarget = next;
                if (CurrentTarget != null) CurrentTarget.SetHighlighted(true);
            }

            if (reticle != null)
                reticle.color = CurrentTarget != null ? new Color(0.27f, 1f, 0.65f) : new Color(0.8f, 0.9f, 0.92f, 0.85f);
            if (prompt != null)
                prompt.text = PanelVisible ? "ESC / ESPACIO  ·  Cerrar ficha" : CurrentTarget != null
                    ? (CurrentTarget.kind == ExplorationTarget.Kind.Inspection ? "INSPECCIONAR  /  " : "TELETRANSPORTE  /  ") + CurrentTarget.title + "\nESPACIO o CLIC para seleccionar"
                    : "Mira un objeto iluminado o una estación de teletransporte";
        }

        public void SelectAtScreenPoint(Vector2 point)
        {
            if (Physics.Raycast(viewCamera.ScreenPointToRay(point), out var hit, interactionDistance, raycastMask, QueryTriggerInteraction.Collide))
            {
                var target = hit.collider.GetComponentInParent<ExplorationTarget>();
                if (target == null) return;
                if (CurrentTarget != null) CurrentTarget.SetHighlighted(false);
                CurrentTarget = target;
                ActivateCurrentTarget();
            }
        }

        bool PointerHitsInterface(Vector2 point)
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events == null) return false;
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = point }, hits);
            return hits.Count > 0;
        }

        public bool ActivateCurrentTarget()
        {
            var target = CurrentTarget;
            if (target == null || PanelVisible || inputCooldown > 0) return false;
            if (target.kind != ExplorationTarget.Kind.Inspection && target.destination == null) return false;
            LastSelectedId = target.uniqueId;
            inputCooldown = 0.25f;

            if (target.kind == ExplorationTarget.Kind.Inspection)
            {
                inspectedObjects.Add(target.uniqueId);
                if (!GuidedMode) ShowInspection(target.title, target.body);
                target.SetHighlighted(false);
                CurrentTarget = null;
            }
            else
            {
                Vector3 up = xrOrigin.transform.up;
                float eyeHeight = Mathf.Max(0.7f, Vector3.Dot(viewCamera.transform.position - xrOrigin.transform.position, up));
                xrOrigin.MatchOriginUpCameraForward(up, target.destination.forward);
                xrOrigin.MoveCameraToWorldLocation(target.destination.position + up * eyeHeight);
                Physics.SyncTransforms();
                visitedStations.Add(string.IsNullOrEmpty(target.stationId) ? target.uniqueId : target.stationId);
                TeleportsPerformed++;
                LastTeleportPosition = target.destination.position;
                if (fade != null) fade.color = new Color(0, 0, 0, 0.65f);
            }

            UpdateProgress();
            TargetActivated?.Invoke(target);
            return true;
        }

        public void CloseInspection()
        {
            if (!PanelVisible) return;
            inspectionPanel.SetActive(false);
            Array.Clear(panelActions, 0, panelActions.Length);
            inputCooldown = 0.2f;
            InspectionClosed?.Invoke();
        }

        public void SetGuidedMode(bool guided)
        {
            GuidedMode = guided;
            CloseInspection();
            if (HudRoot == null) return;
            foreach (string childName in new[] { "Header", "Footer", "Controls", "AttackStatus" })
            {
                var child = HudRoot.Find(childName);
                if (child != null) child.gameObject.SetActive(!guided);
            }
        }

        /// <summary>Accessible alternative to aiming at a floor destination.</summary>
        public bool TravelToStation(ExplorationTarget station)
        {
            if (station == null || station.destination == null || xrOrigin == null || viewCamera == null) return false;
            Vector3 up = xrOrigin.transform.up;
            float height = Mathf.Max(.7f, Vector3.Dot(viewCamera.transform.position - xrOrigin.transform.position, up));
            xrOrigin.MatchOriginUpCameraForward(up, station.destination.forward);
            xrOrigin.MoveCameraToWorldLocation(station.destination.position + up * height);
            Physics.SyncTransforms();
            visitedStations.Add(station.uniqueId);
            TeleportsPerformed++;
            LastTeleportPosition = station.destination.position;
            if (fade != null) fade.color = new Color(0, 0, 0, .65f);
            UpdateProgress();
            inputCooldown = .3f;
            return true;
        }

        public void ShowInspection(string title, string body, PanelAction primary = default, PanelAction secondary = default,
            PanelAction third = default)
        {
            inspectionTitle.text = title;
            inspectionBody.text = body;
            panelActions[0] = primary;
            panelActions[1] = secondary;
            panelActions[2] = third;
            bool threeActions = !string.IsNullOrEmpty(third.Label);
            for (int i = 0; i < panelActions.Length; i++)
            {
                var action = panelActions[i];
                var button = actionButtons[i];
                var label = actionLabels[i];
                button.gameObject.SetActive(!string.IsNullOrEmpty(action.Label));
                button.anchoredPosition = new Vector2(threeActions ? (i - 1) * 258 : i * 388 - 194, -145);
                button.sizeDelta = new Vector2(threeActions ? 246 : 372, 52);
                label.rectTransform.sizeDelta = new Vector2(button.sizeDelta.x - 16, 42);
                label.text = $"{i + 1}  {action.Label}";
                button.GetComponent<Image>().color = action.Invoke == null
                    ? new Color(.11f, .14f, .15f)
                    : i == 0 ? new Color(.055f, .25f, .20f) : new Color(.085f, .17f, .20f);
                label.color = action.Invoke == null ? new Color(.58f, .65f, .66f) : new Color(.82f, .98f, .95f);
            }
            inspectionPanel.SetActive(true);
        }

        bool HitButton(RectTransform button, Vector2 pointer)
        {
            if (!button.gameObject.activeInHierarchy) return false;
            var canvas = HudRoot.GetComponent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : viewCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(button, pointer, camera);
        }

        bool TryUsePanelAction(Keyboard keyboard, bool click, Vector2 pointer)
        {
            for (int i = 0; i < panelActions.Length; i++)
            {
                var button = actionButtons[i];
                if (!button.gameObject.activeInHierarchy) continue;
                bool shortcut = keyboard != null && keyboard[ActionKeys[i]].wasPressedThisFrame;
                if (!shortcut && !(click && HitButton(button, pointer))) continue;
                inputCooldown = 0.2f;
                panelActions[i].Invoke?.Invoke();
                return true;
            }
            return false;
        }

        void UpdateProgress()
        {
            if (progress != null)
                progress.text = "ESTACIONES  " + VisitedCount + " / 6     ·     OBJETOS  " + InspectedCount + " / 5";
        }

        void OnDisable()
        {
            if (CurrentTarget != null) CurrentTarget.SetHighlighted(false);
        }

        void BuildInterface()
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            var root = new GameObject("Exploration_HUD", typeof(RectTransform), typeof(Canvas));
            var canvas = root.GetComponent<Canvas>();
            canvas.worldCamera = viewCamera;
            canvas.sortingOrder = 100;
            var rt = root.GetComponent<RectTransform>();
            HudRoot = rt;
#if UNITY_EDITOR
            // Desktop simulator HUD must remain above world text and geometry.
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
#else
            root.transform.SetParent(viewCamera.transform, false);
            canvas.renderMode = RenderMode.WorldSpace;
            rt.sizeDelta = new Vector2(1600, 900);
            rt.localPosition = new Vector3(0, 0, 1.2f);
            rt.localScale = Vector3.one * 0.00125f;
#endif

            var header = Box(root.transform, "Header", new Vector2(0, 400), new Vector2(1540, 68), new Color(0.015f, 0.045f, 0.055f, 0.92f));
            Label(header, "ANDES-SIM   /   CENTRO DE DATOS", new Vector2(-335, 0), new Vector2(820, 48), 27, new Color(0.44f, 1, 0.77f));
            var sceneMode = Label(header, "EXPLORACIÓN", new Vector2(495, 0), new Vector2(470, 48), 21, new Color(0.63f, 0.78f, 0.82f));
            sceneMode.name = "SceneModeLabel";
            var footer = Box(root.transform, "Footer", new Vector2(0, -385), new Vector2(1540, 115), new Color(0.015f, 0.045f, 0.055f, 0.94f));
            prompt = Label(footer, "", new Vector2(-330, 12), new Vector2(800, 84), 22, new Color(0.9f, 0.98f, 0.96f));
            progress = Label(footer, "", new Vector2(465, 22), new Vector2(560, 42), 20, new Color(0.44f, 1, 0.77f));
#if UNITY_EDITOR
            Label(footer, "MOVIMIENTO LIBRE + TELETRANSPORTE", new Vector2(465, -23), new Vector2(560, 35), 16, new Color(0.6f, 0.73f, 0.77f));
#else
            Label(footer, "SOLO TELETRANSPORTE  ·  DATOS FICTICIOS", new Vector2(465, -23), new Vector2(560, 35), 16, new Color(0.6f, 0.73f, 0.77f));
#endif

            var help = Box(root.transform, "Controls", new Vector2(-490, 275), new Vector2(560, 154), new Color(0.015f, 0.045f, 0.055f, 0.88f));
            Label(help, simulatorInstructions, Vector2.zero, new Vector2(520, 138), 18, new Color(0.73f, 0.85f, 0.87f));
#if UNITY_EDITOR
            header.anchorMin = header.anchorMax = new Vector2(.5f, 1);
            header.anchoredPosition = new Vector2(0, -55);
            footer.anchorMin = footer.anchorMax = new Vector2(.5f, 0);
            footer.anchoredPosition = new Vector2(0, 80);
            help.anchorMin = help.anchorMax = new Vector2(.5f, 1);
            help.anchoredPosition = new Vector2(-490, -170);
#endif
            var dot = Box(root.transform, "GazeReticle", Vector2.zero, new Vector2(7, 7), Color.white);
            reticle = dot.GetComponent<Image>();
            Box(root.transform, "ReticleTop", new Vector2(0, 15), new Vector2(2, 7), new Color(0.5f, 1, 0.8f, 0.75f));
            Box(root.transform, "ReticleBottom", new Vector2(0, -15), new Vector2(2, 7), new Color(0.5f, 1, 0.8f, 0.75f));
            Box(root.transform, "ReticleLeft", new Vector2(-15, 0), new Vector2(7, 2), new Color(0.5f, 1, 0.8f, 0.75f));
            Box(root.transform, "ReticleRight", new Vector2(15, 0), new Vector2(7, 2), new Color(0.5f, 1, 0.8f, 0.75f));

            inspectionPanel = Box(root.transform, "InspectionPanel", new Vector2(0, 5), new Vector2(860, 470), new Color(0.015f, 0.04f, 0.05f, 1f)).gameObject;
            Box(inspectionPanel.transform, "Accent", new Vector2(-422, 0), new Vector2(5, 470), new Color(0.27f, 1, 0.65f));
            Label(inspectionPanel.transform, "FICHA DE INSPECCIÓN", new Vector2(0, 187), new Vector2(760, 35), 18, new Color(0.27f, 1, 0.65f));
            inspectionTitle = Label(inspectionPanel.transform, "", new Vector2(0, 126), new Vector2(760, 65), 35, Color.white);
            inspectionBody = Label(inspectionPanel.transform, "", new Vector2(0, -15), new Vector2(760, 195), 25, new Color(0.78f, 0.87f, 0.88f));
            inspectionBody.alignment = TextAlignmentOptions.TopLeft;
            inspectionBody.enableAutoSizing = true;
            inspectionBody.fontSizeMin = 19;
            inspectionBody.fontSizeMax = 25;
            for (int i = 0; i < actionButtons.Length; i++)
            {
                var button = Box(inspectionPanel.transform, $"InspectionAction{i + 1}", Vector2.zero,
                    new Vector2(372, 52), Color.clear);
                var label = Label(button, "", Vector2.zero, new Vector2(350, 42), 19, Color.white);
                label.alignment = TextAlignmentOptions.Center;
                actionButtons[i] = button;
                actionLabels[i] = label;
            }
            Label(inspectionPanel.transform, "ESC / ESPACIO  ·  CERRAR Y CONTINUAR", new Vector2(0, -196), new Vector2(760, 33), 18, new Color(0.44f, 1, 0.77f));
            inspectionPanel.SetActive(false);

            var fadeTransform = Box(root.transform, "TeleportFade", Vector2.zero, new Vector2(6000, 6000), Color.clear);
            fade = fadeTransform.GetComponent<Image>();
        }

        internal RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = item.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        internal TextMeshProUGUI Label(Transform parent, string content, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var item = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            var text = item.GetComponent<TextMeshProUGUI>();
            var rect = item.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
    }
}
