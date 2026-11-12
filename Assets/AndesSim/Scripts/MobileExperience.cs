using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AndesSim
{
    /// <summary>Single-screen Android training: touch interaction and sensor-relative camera.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class MobileExperience : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }
        public static MobileExperience Instance { get; private set; }
        public bool GyroscopeMode { get; private set; }
        public bool HasGyroscope => AttitudeSensor.current != null;
        TrainingDirector director;
        ExplorationController explorer;
        MentorPresenter mentor;
        Camera cameraView;
        GameObject menu, settings, controls, worldActions;
        MobileLookStick stick;
        Text sensorStatus, playLabel;
        Quaternion lastView, sensorZero, viewZero;
        bool started, sensorCalibrated;
        float yaw, pitch;
        Font font;

        void Awake()
        {
            Instance = this; IsPaused = true;
            director = GetComponent<TrainingDirector>();
            mentor = GetComponent<MentorPresenter>();
            explorer = FindAnyObjectByType<ExplorationController>();
            cameraView = explorer.viewCamera != null ? explorer.viewCamera : Camera.main;
            director.enabled = false; explorer.enabled = false;
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
            lastView = cameraView.transform.rotation;
            yaw = lastView.eulerAngles.y;
        }

        void Start()
        {
            mentor.Root.gameObject.SetActive(false);
            BuildUI();
            SelectCamera(PlayerPrefs.GetInt("Andes.Gyroscope", 0) == 1);
        }

        void LateUpdate()
        {
            if (worldActions != null) worldActions.SetActive(!IsPaused && !mentor.IsTerminalOpen);
            if (IsPaused || !started || mentor.IsTerminalOpen) return;
            if (Quaternion.Angle(lastView, cameraView.transform.rotation) > .1f)
            {
                yaw = cameraView.transform.eulerAngles.y;
                pitch = Mathf.DeltaAngle(0, cameraView.transform.eulerAngles.x);
                sensorCalibrated = false;
            }
            if (GyroscopeMode && HasGyroscope)
            {
                Quaternion sensor = ConvertAttitude(AttitudeSensor.current.attitude.ReadValue());
                if (!sensorCalibrated) { sensorZero = sensor; viewZero = cameraView.transform.rotation; sensorCalibrated = true; }
                var rotation = viewZero * Quaternion.Inverse(sensorZero) * sensor;
                yaw = rotation.eulerAngles.y;
                pitch = Mathf.Clamp(Mathf.DeltaAngle(0, rotation.eulerAngles.x), -80, 80);
            }
            else
            {
                yaw += stick.Value.x * 95 * Time.unscaledDeltaTime;
                pitch = Mathf.Clamp(pitch - stick.Value.y * 75 * Time.unscaledDeltaTime, -80, 80);
            }
            cameraView.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            lastView = cameraView.transform.rotation;
            stick.gameObject.SetActive(!GyroscopeMode);
        }

        static Quaternion ConvertAttitude(Quaternion value) => new Quaternion(value.x, value.y, -value.z, -value.w);

        public void SelectCamera(bool gyro)
        {
            if (gyro && HasGyroscope) InputSystem.EnableDevice(AttitudeSensor.current);
            GyroscopeMode = gyro && HasGyroscope;
            sensorCalibrated = false;
            PlayerPrefs.SetInt("Andes.Gyroscope", GyroscopeMode ? 1 : 0);
            PlayerPrefs.Save();
            if (sensorStatus != null) sensorStatus.text = gyro && !HasGyroscope
                ? "Este dispositivo no ofrece giroscopio. Se utiliza joystick."
                : GyroscopeMode ? "Cámara: GIROSCOPIO · orientación relativa, sin inclinación lateral."
                : "Cámara: JOYSTICK · arrastra el círculo para mirar.";
            if (stick != null) { stick.ResetStick(); stick.gameObject.SetActive(!GyroscopeMode); }
        }

        public void BeginOrResume()
        {
            IsPaused = false; menu.SetActive(false); settings.SetActive(false);
            mentor.Root.gameObject.SetActive(true); controls.SetActive(true);
            if (!started) { explorer.enabled = true; director.enabled = true; started = true; playLabel.text = "Continuar entrenamiento"; }
            sensorCalibrated = false;
        }

        void OpenMenu(bool configuration)
        {
            IsPaused = true; stick.ResetStick(); controls.SetActive(false);
            menu.SetActive(!configuration); settings.SetActive(configuration);
        }

        void Interact()
        {
            if (IsPaused || mentor.BlocksWorldInput || !mentor.IsReadyForAction) return;
            explorer.RefreshGaze(); explorer.ActivateCurrentTarget();
        }

        void BuildUI()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("Mobile_Controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 600;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (EventSystem.current == null)
                new GameObject("Mobile_EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            controls = Panel(root.transform, "TouchControls", Color.clear).gameObject;
            Button(controls.transform, "Menú", new Vector2(600, -35), new Vector2(130, 48), () => OpenMenu(false));
            Button(controls.transform, "Ajustes", new Vector2(750, -35), new Vector2(130, 48), () => OpenMenu(true));
            worldActions = Panel(controls.transform,"WorldTouchActions",Color.clear).gameObject;
            var look = Panel(worldActions.transform, "CameraJoystick", new Color(.03f, .18f, .22f, .9f));
            look.anchorMin = look.anchorMax = new Vector2(0, 0); look.pivot = new Vector2(.5f,.5f);
            look.anchoredPosition = new Vector2(110, 120); look.sizeDelta = new Vector2(160,160);
            look.GetComponent<Image>().raycastTarget = true;
            stick = look.gameObject.AddComponent<MobileLookStick>();
            var knob = Panel(look, "JoystickHandle", new Color(.3f,.85f,.85f)); knob.sizeDelta = new Vector2(58,58);
            knob.anchorMin = knob.anchorMax = new Vector2(.5f,.5f); knob.anchoredPosition = Vector2.zero;
            stick.Handle = knob;
            Button(worldActions.transform, "Inspeccionar", new Vector2(295, -625), new Vector2(175,60), Interact);
            Button(worldActions.transform, "Recentrar cámara", new Vector2(500,-625), new Vector2(195,60), () => { sensorCalibrated = false; });
            controls.SetActive(false);
            menu = Panel(root.transform, "MainMenu", new Color(.01f,.03f,.045f,1)).gameObject;
            Label(menu.transform,"ANDES-SIM\nCENTRO DE DATOS",new Vector2(640,-155),new Vector2(1000,160),42);
            Label(menu.transform,"Entrena con Elliot Alderson. Investiga el incidente y prueba las defensas.\nControles táctiles · Navegación por teletransporte",new Vector2(640,-300),new Vector2(1000,95),22);
            playLabel = Button(menu.transform,"Comenzar entrenamiento",new Vector2(640,-410),new Vector2(480,65),BeginOrResume).GetComponentInChildren<Text>();
            Button(menu.transform,"Configuración",new Vector2(640,-490),new Vector2(480,60),()=>OpenMenu(true));
            Button(menu.transform,"Salir",new Vector2(640,-565),new Vector2(480,55),Application.Quit);
            settings = Panel(root.transform,"Settings",new Color(.01f,.03f,.045f,1)).gameObject;
            Label(settings.transform,"CONFIGURACIÓN",new Vector2(640,-100),new Vector2(1000,70),34);
            Label(settings.transform,"Volumen general",new Vector2(640,-195),new Vector2(700,40),23);
            var sliderRect = Panel(settings.transform,"VolumeSlider",new Color(.15f,.25f,.3f));
            sliderRect.anchorMin=sliderRect.anchorMax=new Vector2(0,1); sliderRect.anchoredPosition=new Vector2(640,-255); sliderRect.sizeDelta=new Vector2(550,40);
            sliderRect.GetComponent<Image>().raycastTarget=true;
            var handle=Panel(sliderRect,"Handle",new Color(.3f,1,.8f)); handle.sizeDelta=new Vector2(35,50);
            var slider=sliderRect.gameObject.AddComponent<Slider>(); slider.handleRect=handle; slider.targetGraphic=handle.GetComponent<Image>(); slider.value=AudioListener.volume;
            slider.onValueChanged.AddListener(value=> { AudioListener.volume=value; PlayerPrefs.SetFloat("Andes.Volume",value); PlayerPrefs.Save(); });
            Button(settings.transform,"Cámara por joystick",new Vector2(440,-365),new Vector2(360,65),()=>SelectCamera(false));
            Button(settings.transform,"Cámara por giroscopio",new Vector2(840,-365),new Vector2(360,65),()=>SelectCamera(true));
            sensorStatus=Label(settings.transform,"",new Vector2(640,-450),new Vector2(1050,75),21);
            Button(settings.transform,"Volver",new Vector2(640,-560),new Vector2(420,65),()=> { if(started) BeginOrResume(); else OpenMenu(false); });
            settings.SetActive(false);
        }

        RectTransform Panel(Transform parent,string name,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false);
            var r=obj.GetComponent<RectTransform>(); r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero;
            obj.GetComponent<Image>().color=color; obj.GetComponent<Image>().raycastTarget=color.a>.95f; return r;
        }
        Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize)
        {
            var obj=new GameObject("Text",typeof(RectTransform),typeof(Text)); obj.transform.SetParent(parent,false);
            var r=obj.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(0,1); r.anchoredPosition=position; r.sizeDelta=size;
            var t=obj.GetComponent<Text>(); t.font=font; t.text=text; t.fontSize=fontSize; t.alignment=TextAnchor.MiddleCenter; t.color=Color.white; t.raycastTarget=false; return t;
        }
        Button Button(Transform parent,string caption,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var r=Panel(parent,caption,new Color(.04f,.25f,.23f)); r.anchorMin=r.anchorMax=new Vector2(0,1); r.anchoredPosition=position; r.sizeDelta=size; r.GetComponent<Image>().raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=r.GetComponent<Image>(); b.onClick.AddListener(action);
            var text=Label(r,caption,Vector2.zero,size-Vector2.one*12,21); text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f); return b;
        }
        void OnDestroy() { if(Instance==this) { Instance=null; IsPaused=false; } }
    }
    public sealed class MobileLookStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Handle;
        public Vector2 Value { get; private set; }
        public void OnPointerDown(PointerEventData e)=>OnDrag(e);
        public void OnDrag(PointerEventData e) { RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var point); Value=Vector2.ClampMagnitude(point/70f,1); Handle.anchoredPosition=Value*48; }
        public void OnPointerUp(PointerEventData e)=>ResetStick();
        public void ResetStick() { Value=Vector2.zero; if(Handle!=null) Handle.anchoredPosition=Vector2.zero; }
    }
}
