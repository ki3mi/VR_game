using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using FingerTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace AndesSim
{
    [DefaultExecutionOrder(-500)]
    public sealed class MobileExperience : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }
        public static MobileExperience Instance { get; private set; }
        public bool GyroscopeMode { get; private set; }
        public bool HasGyroscope => AttitudeSensor.current != null;

        const int MousePointer = -1;
        const int NoPointer = -2;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        PointerEventData uiProbe;
        TrainingDirector director;
        ExplorationController explorer;
        MentorPresenter mentor;
        Camera cameraView;
        GameObject menu, settings, controls, historyButton, restartButton, recenterButton;
        Text sensorStatus, playLabel;
        RectTransform safeArea, menuSafeArea;
        Quaternion lastView, sensorZero, viewZero;
        bool started, sensorCalibrated, dragging, touchEnabled;
        float yaw, pitch;
        int pointer = NoPointer;
        Vector2 pressPosition, lastPosition;
        Font font;

        void Awake()
        {
            Instance = this;
            IsPaused = true;
            director = GetComponent<TrainingDirector>();
            mentor = GetComponent<MentorPresenter>();
            explorer = FindAnyObjectByType<ExplorationController>();
            cameraView = explorer.viewCamera != null ? explorer.viewCamera : Camera.main;
            director.enabled = false;
            explorer.enabled = false;
            var legacyUI = GetComponent<UIManager>();
            if (legacyUI != null) legacyUI.enabled = false;
            foreach (var simulator in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator>())
                simulator.gameObject.SetActive(false);
            foreach (var driver in cameraView.GetComponents<Behaviour>())
                if (driver.GetType().Name == "TrackedPoseDriver") driver.enabled = false;
            if (explorer.xrOrigin != null && explorer.xrOrigin.CameraFloorOffsetObject != null)
            {
                explorer.xrOrigin.CameraYOffset = 1.65f;
                explorer.xrOrigin.CameraFloorOffsetObject.transform.localPosition = Vector3.up * 1.65f;
                cameraView.transform.localPosition = Vector3.zero;
            }
            AudioListener.volume = PlayerPrefs.GetFloat("Andes.Volume", .75f);
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            ReadView();
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            touchEnabled = true;
        }

        void Start()
        {
            mentor.Root.gameObject.SetActive(false);
            BuildUI();
            mentor.HistoryClosed += ReturnFromHistory;
            SelectCamera(PlayerPrefs.GetInt("Andes.Gyroscope", 0) == 1);
        }

        void Update()
        {
            if (!CanLook())
            {
                CancelGesture();
                return;
            }

            var touches = FingerTouch.activeTouches;
            if (touches.Count > 0 || pointer >= 0)
            {
                if (pointer == MousePointer) CancelGesture();
                if (pointer == NoPointer)
                {
                    // A second finger cannot take over an existing gesture.
                    if (touches.Count == 1 && touches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
                        BeginGesture(touches[0].finger.index, touches[0].screenPosition);
                    return;
                }

                foreach (var touch in touches)
                {
                    if (touch.finger.index != pointer) continue;
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended) EndGesture(touch.screenPosition);
                    else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled) CancelGesture();
                    else MoveGesture(touch.screenPosition);
                    return;
                }
                CancelGesture();
                return;
            }

#if UNITY_EDITOR
            var mouse = Mouse.current;
            if (mouse == null) return;
            var position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame) BeginGesture(MousePointer, position);
            if (pointer != MousePointer) return;
            if (mouse.leftButton.wasReleasedThisFrame) EndGesture(position);
            else if (mouse.leftButton.isPressed) MoveGesture(position);
            else CancelGesture();
#endif
        }

        bool CanLook() => started && !IsPaused && !mentor.BlocksWorldInput && !explorer.PanelVisible;

        void BeginGesture(int id, Vector2 position)
        {
            if (HitsInterface(position)) return;
            pointer = id;
            pressPosition = lastPosition = position;
            dragging = false;
            SyncExternalView();
        }

        void MoveGesture(Vector2 position)
        {
            var scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
            if (!dragging)
            {
                float threshold = Mathf.Max(5f, scale * .012f);
                if ((position - pressPosition).sqrMagnitude < threshold * threshold) return;
                dragging = true;
            }
            var delta = position - lastPosition;
            lastPosition = position;
            if (GyroscopeMode || delta == Vector2.zero) return;
            SyncExternalView();
            yaw = Mathf.Repeat(yaw + delta.x * 145f / scale, 360f);
            pitch = Mathf.Clamp(pitch - delta.y * 120f / scale, -80f, 80f);
            ApplyView();
        }

        void EndGesture(Vector2 position)
        {
            MoveGesture(position);
            bool select = !dragging && mentor.IsReadyForAction && !HitsInterface(position);
            CancelGesture();
            if (select) explorer.SelectAtScreenPoint(position);
        }

        void CancelGesture()
        {
            pointer = NoPointer;
            dragging = false;
        }

        bool HitsInterface(Vector2 position)
        {
            if (mentor.IsScreenPointOverUI(position) || !Screen.safeArea.Contains(position)) return true;
            var events = EventSystem.current;
            if (events == null) return false;
            if (uiProbe == null) uiProbe = new PointerEventData(events);
            uiProbe.position = position;
            events.RaycastAll(uiProbe, uiHits);
            bool hit = uiHits.Count > 0;
            uiHits.Clear();
            return hit;
        }

        void LateUpdate()
        {
            if (safeArea != null) FitSafeArea(safeArea);
            if (menuSafeArea != null) FitSafeArea(menuSafeArea);
            if (recenterButton != null)
                recenterButton.SetActive(GyroscopeMode && !mentor.BlocksWorldInput);
            if (!CanLook())
            {
                sensorCalibrated = false;
                return;
            }
            SyncExternalView();
            if (!GyroscopeMode) return;
            if (!HasGyroscope)
            {
                SelectCamera(true);
                return;
            }

            Quaternion sensor = AttitudeSensor.current.attitude.ReadValue();
            sensor = new Quaternion(sensor.x, sensor.y, -sensor.z, -sensor.w);
            if (Quaternion.Dot(sensor, sensor) < .5f) return;
            if (!sensorCalibrated)
            {
                sensorZero = sensor;
                viewZero = lastView;
                sensorCalibrated = true;
            }
            var rotation = viewZero * Quaternion.Inverse(sensorZero) * sensor;
            yaw = rotation.eulerAngles.y;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0, rotation.eulerAngles.x), -80f, 80f);
            ApplyView();
        }

        void SyncExternalView()
        {
            if (Quaternion.Angle(lastView, cameraView.transform.rotation) <= .1f) return;
            ReadView();
            sensorCalibrated = false;
        }

        void ReadView()
        {
            lastView = cameraView.transform.rotation;
            yaw = lastView.eulerAngles.y;
            pitch = Mathf.DeltaAngle(0, lastView.eulerAngles.x);
        }

        void ApplyView()
        {
            lastView = Quaternion.Euler(pitch, yaw, 0);
            cameraView.transform.rotation = lastView;
        }

        public void SelectCamera(bool gyro)
        {
            CancelGesture();
            if (gyro && HasGyroscope) InputSystem.EnableDevice(AttitudeSensor.current);
            GyroscopeMode = gyro && HasGyroscope;
            sensorCalibrated = false;
            PlayerPrefs.SetInt("Andes.Gyroscope", gyro ? 1 : 0);
            PlayerPrefs.Save();
            if (sensorStatus == null) return;
            sensorStatus.text = gyro && !HasGyroscope
                ? "El sensor no está disponible. Puedes mirar arrastrando sobre la sala."
                : GyroscopeMode ? "Giroscopio activo. Mueve el teléfono para mirar."
                : "Arrastre activo. Desliza sobre la sala para mirar.";
        }

        public void BeginOrResume()
        {
            CancelGesture();
            IsPaused = false;
            menu.SetActive(false);
            settings.SetActive(false);
            mentor.Root.gameObject.SetActive(true);
            controls.SetActive(true);
            if (!started)
            {
                explorer.enabled = true;
                director.enabled = true;
                started = true;
                playLabel.text = "Continuar entrenamiento";
                historyButton.SetActive(true);
                restartButton.SetActive(true);
            }
            sensorCalibrated = false;
        }

        void OpenMenu(bool configuration)
        {
            CancelGesture();
            IsPaused = true;
            mentor.ClearUISelection();
            controls.SetActive(false);
            menu.SetActive(!configuration);
            settings.SetActive(configuration);
        }

        void OpenHistory()
        {
            menu.SetActive(false);
            mentor.SetHistoryVisible(true);
        }

        void ReturnFromHistory() => OpenMenu(false);

        void RestartTraining()
        {
            BeginOrResume();
            mentor.RequestRestart();
        }

        void BuildUI()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("Mobile_Controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (EventSystem.current == null)
                new GameObject("Mobile_EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            menuSafeArea = Panel(root.transform, "MenuSafeArea", Color.clear);
            safeArea = Panel(root.transform, "SafeArea", Color.clear);
            FitSafeArea(safeArea);
            controls = Panel(safeArea, "TouchControls", Color.clear).gameObject;
            var controlPanel = controls.GetComponent<RectTransform>();
            Button(controlPanel, "Menú", Vector2.one, new Vector2(-177, -37), new Vector2(94, 48), () => OpenMenu(false));
            Button(controlPanel, "Ajustes", Vector2.one, new Vector2(-68, -37), new Vector2(104, 48), () => OpenMenu(true));
            recenterButton = Button(controlPanel, "Recentrar cámara", Vector2.one, new Vector2(-94, -94),
                new Vector2(164, 46), () => sensorCalibrated = false).gameObject;
            controls.SetActive(false);

            FitSafeArea(menuSafeArea);
            menu = Panel(menuSafeArea, "MainMenu", new Color(.018f, .034f, .049f)).gameObject;
            var center = new Vector2(.5f, .5f);
            Label(menu.transform, "ANDES-SIM\nCENTRO DE DATOS", center, new Vector2(0, 205), new Vector2(710, 112), 42);
            Label(menu.transform, "Investiga el incidente junto a Elliot Alderson y comprueba las defensas.",
                center, new Vector2(0, 126), new Vector2(820, 60), 23);
            playLabel = Button(menu.transform, "Comenzar entrenamiento", center, new Vector2(0, 45),
                new Vector2(410, 59), BeginOrResume).GetComponentInChildren<Text>();
            Button(menu.transform, "Configuración", center, new Vector2(0, -20), new Vector2(410, 54), () => OpenMenu(true));
            historyButton = Button(menu.transform, "Historial", center, new Vector2(0, -81), new Vector2(410, 52), OpenHistory).gameObject;
            restartButton = Button(menu.transform, "Reiniciar entrenamiento", center, new Vector2(0, -139),
                new Vector2(410, 52), RestartTraining).gameObject;
            Button(menu.transform, "Salir", center, new Vector2(0, -203), new Vector2(410, 52), Application.Quit);
            historyButton.SetActive(false);
            restartButton.SetActive(false);

            settings = Panel(menuSafeArea, "Settings", new Color(.018f, .034f, .049f)).gameObject;
            Label(settings.transform, "Configuración", center, new Vector2(0, 195), new Vector2(640, 72), 34);
            Label(settings.transform, "Volumen general", center, new Vector2(0, 108), new Vector2(500, 44), 23);
            var sliderRect = Panel(settings.transform, "VolumeSlider", new Color(.15f, .25f, .3f));
            Place(sliderRect, center, new Vector2(0, 52), new Vector2(520, 45));
            var handle = Panel(sliderRect, "Handle", new Color(.3f, 1, .8f));
            Place(handle, center, Vector2.zero, new Vector2(35, 50));
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.value = AudioListener.volume;
            slider.onValueChanged.AddListener(value =>
            {
                AudioListener.volume = value;
                PlayerPrefs.SetFloat("Andes.Volume", value);
                PlayerPrefs.Save();
            });
            Button(settings.transform, "Arrastre", center, new Vector2(-155, -38), new Vector2(285, 60), () => SelectCamera(false));
            Button(settings.transform, "Giroscopio", center, new Vector2(155, -38), new Vector2(285, 60), () => SelectCamera(true));
            sensorStatus = Label(settings.transform, "", center, new Vector2(0, -114), new Vector2(760, 70), 21);
            Button(settings.transform, "Volver", center, new Vector2(0, -194), new Vector2(410, 56),
                () => { if (started) BeginOrResume(); else OpenMenu(false); });
            settings.SetActive(false);
        }

        internal static void FitSafeArea(RectTransform rect)
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static RectTransform Panel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = color.a > .95f;
            return rect;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        Text Label(Transform parent, string caption, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Place(obj.GetComponent<RectTransform>(), anchor, position, size);
            var label = obj.GetComponent<Text>();
            label.font = font;
            label.text = caption;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        Button Button(Transform parent, string caption, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(parent, caption, new Color(.055f, .23f, .22f));
            Place(rect, anchor, position, size);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            Label(rect, caption, new Vector2(.5f, .5f), Vector2.zero, size - Vector2.one * 10, 19);
            return button;
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            CancelGesture();
            sensorCalibrated = false;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) OnApplicationFocus(false);
        }

        void OnDisable()
        {
            CancelGesture();
            if (touchEnabled) EnhancedTouchSupport.Disable();
            touchEnabled = false;
        }

        void OnDestroy()
        {
            if (mentor != null) mentor.HistoryClosed -= ReturnFromHistory;
            if (Instance != this) return;
            Instance = null;
            IsPaused = false;
        }
    }
}
