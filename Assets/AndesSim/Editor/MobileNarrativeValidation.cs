using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AndesSim.Editor
{
    [InitializeOnLoad]
    public static class MobileNarrativeValidation
    {
        const string Running = "Andes.MobileValidation.Running";
        const string DirectoryKey = "Andes.MobileValidation.Output";
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
        static readonly List<string> checks = new List<string>();
        static readonly List<string> errors = new List<string>();
        static readonly List<string> captures = new List<string>();
        static MobileExperience mobile;
        static ExplorationController explorer;
        static MentorPresenter mentor;
        static TrainingDirector director;
        static Touchscreen touchscreen;
        static Mouse mouse;
        static MouseState? mouseState;
        static TouchState? touchState;
        static int touchId, previousViewSize, previousGyroscope;
        static double waitUntil, deadline;
        static bool previousBackground, finishPending, hadGyroscopePreference;
        static InputSettings.BackgroundBehavior previousInputBackground;
        static InputSettings.EditorInputBehaviorInPlayMode previousInputRouting;
        static EditorWindow gameView;
        static string output;

        static MobileNarrativeValidation() => EditorApplication.playModeStateChanged += OnPlayState;

        [MenuItem("Andes-Sim/Android/Validar diálogo y recorrido táctil")]
        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "DataCenter_VR")
            {
                Debug.LogWarning("Abre DataCenter_VR en Edit Mode para validar el recorrido táctil.");
                return;
            }

            output = Path.GetFullPath(Path.Combine("Build", "Validacion_Movil", DateTime.Now.ToString("yyyyMMdd_HHmmss")));
            Directory.CreateDirectory(output);
            SessionState.SetString(DirectoryKey, output);
            SessionState.SetBool(Running, true);
            gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView.Focus();
            EditorApplication.isPlaying = true;
        }

        static void OnPlayState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Running, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                output = SessionState.GetString(DirectoryKey, "");
                checks.Clear();
                errors.Clear();
                captures.Clear();
                steps.Clear();
                finishPending = false;
                steps.Push(Validate());
                deadline = EditorApplication.timeSinceStartup + 270;
                waitUntil = EditorApplication.timeSinceStartup + 1;
                Application.logMessageReceived += CaptureError;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode && !finishPending)
            {
                errors.Add("El Editor salió de Play Mode antes de finalizar.");
                Finish(false);
            }
            else if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Running, false);
        }

        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup < waitUntil) return;
                Require(EditorApplication.timeSinceStartup < deadline, "La validación superó 270 segundos.");
                if (steps.Count == 0) { Finish(); return; }
                var step = steps.Peek();
                if (!step.MoveNext()) steps.Pop();
                else if (step.Current is IEnumerator nested) steps.Push(nested);
                else if (step.Current is double delay) waitUntil = EditorApplication.timeSinceStartup + delay;
            }
            catch (Exception e)
            {
                errors.Add(e.ToString());
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "fallo.png"));
                Finish();
            }
        }

        static IEnumerator Validate()
        {
            previousBackground = Application.runInBackground;
            previousInputBackground = InputSystem.settings.backgroundBehavior;
            previousInputRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            hadGyroscopePreference = PlayerPrefs.HasKey("Andes.Gyroscope");
            previousGyroscope = PlayerPrefs.GetInt("Andes.Gyroscope", 0);
            gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView.Focus();
            previousViewSize = (int)gameView.GetType().GetProperty("selectedSizeIndex", Members).GetValue(gameView);
            mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            InputSystem.onBeforeUpdate += Inject;
            mobile = Object.FindAnyObjectByType<MobileExperience>();
            explorer = Object.FindAnyObjectByType<ExplorationController>();
            director = Object.FindAnyObjectByType<TrainingDirector>();
            mentor = Object.FindAnyObjectByType<MentorPresenter>();
            Require(mobile != null && explorer != null && director != null && mentor != null, "Faltan los componentes del entrenamiento.");

            SetResolution(1280, 720);
            yield return .5d;
            yield return Click("Configuración");
            yield return Click("Giroscopio");
            if (!mobile.HasGyroscope) Require(!mobile.GyroscopeMode, "El Editor sin sensor debe conservar el arrastre.");
            yield return Click("Arrastre");
            yield return Screenshot("01_ajustes_16_9.png");
            yield return Click("Volver");
            yield return Click("Comenzar entrenamiento");
            yield return Ready();
            Require(mentor.CurrentObjective.StartsWith("Reconstruye") && Visible("MentorDialogue") && !Visible("MentorPhone") &&
                !Visible("GazeReticle") && !Visible("CameraJoystick") && !Visible("Inspeccionar") &&
                !Visible("SeniorHelp") && !Visible("SeniorRepeat") &&
                Object.FindAnyObjectByType<DialoguePortrait>().texture != null, "El HUD móvil inicial no coincide con el rediseño.");
            yield return Screenshot("02_bienvenida_16_9.png");
            yield return Primary();
            yield return Stage(TrainingStage.Orientation);
            Check("Objetivo inicial sustituido al comenzar y retrato cargado");

            Vector2 center = new Vector2(Screen.width * .54f, Screen.height * .61f);
            Quaternion initial = explorer.viewCamera.transform.rotation;
            string selected = explorer.LastSelectedId;
            yield return Swipe(center, center + Vector2.right * 130, true);
            Require(Quaternion.Angle(initial, explorer.viewCamera.transform.rotation) > 10 &&
                explorer.LastSelectedId == selected, "El arrastre táctil debe girar sin seleccionar.");
            initial = explorer.viewCamera.transform.rotation;
            yield return Swipe(Point(mentor.Root.GetComponentsInChildren<RectTransform>().First(r => r.name == "CurrentDialogue")),
                center, true);
            Require(Quaternion.Angle(initial, explorer.viewCamera.transform.rotation) < .1f &&
                explorer.LastSelectedId == selected, "Un arrastre desde el diálogo no debe girar ni seleccionar.");
            yield return Swipe(center, center + Vector2.left * 130, false);
            Require(Quaternion.Angle(initial, explorer.viewCamera.transform.rotation) > 10, "El arrastre de ratón no giró la cámara.");
            Check("Arrastre táctil y con ratón, sin selección accidental ni fuga desde el diálogo");

            yield return Primary();
            yield return Ready();
            yield return SelectWorld("terminal_swift", true);
            yield return WaitUntil(() => mentor.AwaitingNextMessage && !mentor.IsMentorTyping, "Falta Continuar entre las dos indicaciones.");
            Require(mentor.PrimaryActionLabel == "Continuar" && !ExecuteButton().interactable && !mentor.CanExecuteQuery,
                "Solo Continuar debe estar disponible hasta que Elliot indique ejecutar.");
            yield return Click("ExecuteCommand");
            Require(director.Stage == TrainingStage.AccessQuery && director.QueryCount == 0,
                "Un toque antes de la indicación no debe ejecutar la consulta.");
            yield return Click("Ajustes");
            yield return .45d;
            yield return Click("Volver");
            Require(mentor.IsTerminalOpen && mentor.AwaitingNextMessage && !mentor.CanExecuteQuery,
                "Pausar la terminal debe conservar el paso y el bloqueo.");
            int before = mentor.ReceivedMessageCount;
            int replies = mentor.NextAcknowledgementCount;
            CheckSectionLayout();
            yield return Screenshot("03_terminal_espera_16_9.png");
            yield return Primary();
            Require(mentor.LastPlayerMessage == "Ok" && mentor.NextAcknowledgementCount == replies + 1,
                "Continuar debe registrar un solo Ok. Último mensaje: " + mentor.LastPlayerMessage +
                ". Confirmaciones: " + replies + " → " + mentor.NextAcknowledgementCount);
            yield return Ready();
            Require(mentor.ReceivedMessageCount == before + 1 && mentor.LastDeliveredMessage.Contains("«Ejecutar»"),
                "Un Ok debe entregar solo la sección que instruye ejecutar.");
            Check("Continuar entrega una sección y la consulta sigue bloqueada tras pausar antes de la instrucción");
            yield return Screenshot("04_terminal_lista_16_9.png");
            yield return Query(TrainingStage.AccessReview);
            Require(mentor.CurrentTerminalOutput.Contains("LOGIN ACCEPTED"), "Salida de acceso inesperada.");
            string result = mentor.CurrentTerminalOutput;
            yield return Click("Menú");
            yield return Click("Comenzar entrenamiento");
            Require(mentor.IsTerminalOpen && mentor.CurrentTerminalOutput == result && !mentor.CanExecuteQuery,
                "Pausar después de ejecutar debe conservar la salida y el bloqueo.");
            Check("Un botón dentro del monitor, comando de solo lectura y resultado conservado tras pausar");

            yield return Primary();
            yield return Stage(TrainingStage.Transmission);
            Require(!mentor.IsTerminalOpen, "Continuar desde el resultado debe cerrar la terminal.");
            yield return Click("Ajustes");
            var pausedAt = director.game.CurrentTime;
            yield return .65d;
            Require(MobileExperience.IsPaused && director.game.CurrentTime == pausedAt, "Ajustes debe pausar la reproducción.");
            yield return Click("Volver");
            yield return Stage(TrainingStage.PaymentQuery, 18);
            Require(director.game.OrdersSent == 36, "La reconstrucción debe llegar a 36 órdenes.");
            yield return SelectWorld("terminal_swift");
            yield return Query(TrainingStage.PaymentReview);
            Require(mentor.CurrentTerminalOutput.Contains("órdenes enviadas: 36"), "La cola debe confirmar 36 órdenes.");
            yield return Primary();
            yield return Stage(TrainingStage.SessionEnd);
            yield return Primary();
            yield return Stage(TrainingStage.Investigate);

            SetResolution(1600, 720);
            yield return .45d;
            string[] evidence = { LogViewer.EvidenceId, ScenarioData.PrinterEvidenceId, ScenarioData.SwitchEvidenceId };
            string[] evidenceTargets = { "terminal_swift", ScenarioData.PrinterEvidenceId, ScenarioData.SwitchEvidenceId };
            yield return Screenshot("05_objetivos_20_9.png");
            for (int i = 0; i < evidence.Length; i++)
            {
                yield return Primary();
                yield return Ready();
                yield return SelectWorld(evidenceTargets[i]);
                yield return Ready();
                Require(mentor.PrimaryActionLabel == "Guardar evidencia", "Falta la acción para guardar " + evidence[i]);
                yield return Primary();
                yield return Stage(i == evidence.Length - 1 ? TrainingStage.PrepareDefense : TrainingStage.Investigate);
                Require(director.TrainingEvidenceCount == i + 1 &&
                    !ReadText("PendingEvidence").Contains(director.game.data.GetEvidenceTitle(evidence[i])),
                    "Un hallazgo guardado debe salir de la lista.");
            }
            Check("Pausa de reproducción y tres objetivos de evidencia retirados al guardar");

            yield return Primary();
            yield return Ready();
            yield return SelectWorld("panel_controles", true);
            yield return Stage(TrainingStage.Mfa);
            yield return Primary();
            yield return Stage(TrainingStage.Schedule);
            yield return Primary();
            yield return Stage(TrainingStage.Siem);
            yield return Primary();
            yield return Stage(TrainingStage.DefenseReady);
            Require(director.game.DefenseCount == 3, "Se requieren los tres controles de defensa.");
            yield return Primary();
            yield return Ready();
            yield return SelectWorld("terminal_swift");
            yield return Ready();
            yield return Primary();
            yield return Stage(TrainingStage.DefenseResult);
            Require(director.game.ProtectedEventRecorded && director.game.OrdersSent == 0, "El intento protegido debe terminar en cero.");
            yield return Primary();
            yield return Stage(TrainingStage.ProtectedQuery);
            var alarm = Object.FindAnyObjectByType<AlarmSystem>();
            Require(alarm != null && alarm.IsActive && alarm.IsPlaying,
                "La alarma debe sonar hasta abrir la terminal protegida.");
            yield return SelectWorld("terminal_swift");
            Require(mentor.IsTerminalOpen && !alarm.IsPlaying && alarm.IsActive &&
                alarm.redLight != null && alarm.redLight.enabled && alarm.redBeacon != null && alarm.redBeacon.enabled,
                "Abrir la terminal debe silenciar el audio y conservar la luz y la baliza.");
            yield return Query(TrainingStage.ProtectedReview);
            Require(!alarm.IsPlaying && director.game.ProtectedEventRecorded,
                "La consulta debe conservar el evento protegido y el audio silenciado.");
            Check("La terminal protegida silencia la alarma al abrirse y conserva la baliza y el evento");
            yield return Screenshot("06_terminal_20_9.png");
            yield return Primary();
            yield return WaitUntil(() => director.Stage == TrainingStage.Comparison && mentor.AwaitingNextMessage,
                "La comparación no entregó la sección de resultados.");
            Require(director.game.AttackResultOrders == 36 && director.game.DefenseResultOrders == 0 &&
                director.QueryCount == 3 && mentor.LastDeliveredMessage.Contains("36 órdenes") &&
                mentor.LastDeliveredMessage.Contains("0 órdenes"), "La comparación debe reflejar las dos ejecuciones reales.");
            CheckSectionLayout();
            yield return Screenshot("07_comparacion_20_9.png");
            yield return Ready();
            yield return Primary();
            yield return Stage(TrainingStage.Debrief);
            yield return Primary();
            yield return Stage(TrainingStage.Completed);
            Require(mentor.CurrentObjective == "Entrenamiento completado", "Falta el estado final del objetivo.");
            Check("Tres controles y tres consultas con comparación 36 → 0 y cierre");

            yield return Click("Menú");
            yield return Click("Historial");
            Require(mentor.IsHistoryOpen && MobileExperience.IsPaused &&
                mentor.ChatHistoryText.Contains("Soy Elliot Alderson") && mentor.ChatHistoryText.Contains("TÚ: Ok"), "El historial debe conservar los diálogos y respuestas.");
            yield return Screenshot("08_historial_20_9.png");
            yield return Click("CloseHistory");
            yield return Click("Comenzar entrenamiento");
            Require(!mentor.IsHistoryOpen && !MobileExperience.IsPaused && director.TrainingComplete, "Salir del historial debe conservar el progreso.");
            Check("Historial consultable desde pausa y regreso al entrenamiento");

            yield return Primary();
            yield return Stage(TrainingStage.Welcome);
            yield return Primary();
            yield return Stage(TrainingStage.Orientation);
            yield return Primary();
            yield return Ready();
            yield return SelectWorld("terminal_swift", true);
            yield return WaitUntil(() => mentor.AwaitingNextMessage, "Falta la segunda sección de la consulta.");
            yield return Click("Menú");
            yield return Click("Reiniciar entrenamiento");
            yield return Click("ConfirmRestart");
            yield return Stage(TrainingStage.Welcome);
            Require(!mentor.IsTerminalOpen && !mentor.CanExecuteQuery && mentor.PendingMessageCount == 0 && director.QueryCount == 0,
                "Reiniciar debe borrar la consulta y sus instrucciones pendientes.");
            Check("Reiniciar una consulta pendiente limpia sus secciones y la autorización");
        }

        static IEnumerator Primary() => Click("PrimaryAction");

        static IEnumerator Query(TrainingStage next)
        {
            yield return Ready();
            Button button = ExecuteButton();
            Require(mentor.CanExecuteQuery && button.interactable && mentor.LastDeliveredMessage.Contains("«Ejecutar»"),
                "Ejecutar debe habilitarse al entregar la instrucción.");
            var terminal = button.transform.GetComponentInParent<RectTransform>();
            while (terminal.name != "GuidedTerminal") terminal = terminal.parent as RectTransform;
            Require(terminal.GetComponentsInChildren<Button>().Length == 1 &&
                terminal.GetComponentInChildren<InputField>() == null && Visible("MonitorScreen"),
                "El monitor debe tener un único botón y un comando de solo lectura.");
            int before = director.QueryCount;
            yield return Click("ExecuteCommand");
            yield return Click("ExecuteCommand");
            yield return Stage(next);
            Require(director.LastQuerySucceeded && mentor.IsTerminalOpen && director.QueryCount == before + 1 &&
                !button.interactable && !mentor.CanExecuteQuery && mentor.PrimaryActionLabel == "Continuar",
                "Una consulta debe ejecutarse una vez y mantener el resultado hasta Continuar.");
        }

        static Button ExecuteButton() => mentor.Root.GetComponentsInChildren<Button>().Single(b => b.name == "ExecuteCommand");

        static void CheckSectionLayout()
        {
            Text body = mentor.Root.GetComponentsInChildren<Text>().Single(t => t.name == "DialogueBody");
            RectTransform viewport = body.GetComponentInParent<ScrollRect>().viewport;
            Canvas.ForceUpdateCanvases();
            Require(body.preferredHeight + 8 <= viewport.rect.height + 1,
                $"Una sección se recorta en {Screen.width}×{Screen.height}: {body.text}");
        }

        static IEnumerator Stage(TrainingStage stage, double timeout = 6)
        {
            yield return WaitUntil(() => director.Stage == stage, "Etapa esperada " + stage, timeout);
            yield return Ready();
        }

        static IEnumerator Ready()
        {
            double end = EditorApplication.timeSinceStartup + 10;
            while (!mentor.IsReadyForAction)
            {
                Require(EditorApplication.timeSinceStartup < end, "Elliot no terminó de entregar el diálogo: " + director.Stage);
                if (mentor.AwaitingNextMessage)
                {
                    CheckSectionLayout();
                    yield return Primary();
                }
                else yield return .1d;
            }
            CheckSectionLayout();
        }

        static IEnumerator WaitUntil(Func<bool> condition, string message, double seconds = 6)
        {
            double end = EditorApplication.timeSinceStartup + seconds;
            while (!condition() && EditorApplication.timeSinceStartup < end) yield return .12d;
            Require(condition(), message + " (etapa " + director.Stage + ")");
        }

        static IEnumerator SelectWorld(string id, bool touch = false)
        {
            var target = Object.FindObjectsByType<ExplorationTarget>().First(t => t.uniqueId == id);
            Vector2 point = default;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                var camera = explorer.viewCamera;
                var screenPoint = camera.WorldToScreenPoint(target.AimPosition);
                point = screenPoint;
                if (screenPoint.z > 0 && point.x > Screen.width * .06f && point.x < Screen.width * .94f &&
                    point.y > Screen.height * .38f && point.y < Screen.height * .92f && !mentor.IsScreenPointOverUI(point)) break;
                // Aim through the same drag input used by a player.
                Vector3 direction = target.AimPosition - camera.transform.position;
                float heading = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float tilt = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                float dx = Mathf.DeltaAngle(camera.transform.eulerAngles.y, heading) * Screen.height / 145f;
                float dy = -Mathf.DeltaAngle(camera.transform.eulerAngles.x, tilt) * Screen.height / 120f;
                var origin = new Vector2(Screen.width * .55f, Screen.height * .57f);
                var delta = Vector2.ClampMagnitude(new Vector2(dx, dy), Screen.height * .38f);
                yield return Swipe(origin, origin + delta, touch);
            }
            yield return Swipe(point, point, touch);
            Require(explorer.LastSelectedId == id,
                "Toque en " + id + " no activó el equipo. Punto " + point + "; última selección: " + explorer.LastSelectedId);
        }

        static IEnumerator Click(string name)
        {
            var rect = Object.FindObjectsByType<RectTransform>().FirstOrDefault(r => r.name == name);
            Require(rect != null, "No se encontró el control visible " + name);
            yield return Swipe(Point(rect), Point(rect), true);
        }

        static IEnumerator Swipe(Vector2 from, Vector2 to, bool touch)
        {
            if (touch)
            {
                touchId++;
                touchState = new TouchState { touchId = touchId, position = from, phase = UnityEngine.InputSystem.TouchPhase.Began };
                yield return .11d;
                if (from != to)
                {
                    touchState = new TouchState { touchId = touchId, position = to, phase = UnityEngine.InputSystem.TouchPhase.Moved };
                    yield return .11d;
                }
                touchState = new TouchState { touchId = touchId, position = to, phase = UnityEngine.InputSystem.TouchPhase.Ended };
            }
            else
            {
                mouseState = new MouseState { position = from };
                yield return .09d;
                mouseState = new MouseState { position = from }.WithButton(MouseButton.Left);
                yield return .09d;
                if (from != to)
                {
                    mouseState = new MouseState { position = to, delta = to - from }.WithButton(MouseButton.Left);
                    yield return .09d;
                }
                mouseState = new MouseState { position = to };
            }
            yield return .24d;
        }

        static void Inject()
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            if (mouseState.HasValue) { InputSystem.QueueStateEvent(mouse, mouseState.Value); mouseState = null; }
            if (touchState.HasValue) { InputSystem.QueueStateEvent(touchscreen, touchState.Value); touchState = null; }
        }

        static Vector2 Point(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        static bool Visible(string name) => Object.FindObjectsByType<RectTransform>().Any(r => r.name == name);
        static string ReadText(string name) => Object.FindObjectsByType<Text>().FirstOrDefault(t => t.name == name)?.text ?? "";
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static void Check(string result) { checks.Add(result); Debug.Log("[ANDES Móvil] OK · " + result); }

        static IEnumerator Screenshot(string file)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(output, file));
            captures.Add(file);
            yield return .35d;
        }

        static void SetResolution(int width, int height)
        {
            var assembly = typeof(EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
            var groupType = type.GetProperty("currentGroupType", Members).GetValue(singleton);
            var group = type.GetMethod("GetGroup", Members).Invoke(singleton, new[] { groupType });
            var groupClass = group.GetType();
            string name = "ANDES " + width + "x" + height;
            string[] labels = (string[])groupClass.GetMethod("GetDisplayTexts", Members).Invoke(group, null);
            int index = Array.FindIndex(labels, label => label.StartsWith(name, StringComparison.Ordinal));
            if (index < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),
                    Enum.Parse(sizeType, "FixedResolution"), width, height, name);
                groupClass.GetMethod("AddCustomSize", Members).Invoke(group, new[] { size });
                index = labels.Length;
            }
            gameView.GetType().GetProperty("selectedSizeIndex", Members).SetValue(gameView, index);
            gameView.Repaint();
        }

        static void CaptureError(string condition, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(condition + "\n" + stack);
        }

        static void Finish(bool exitPlay = true)
        {
            if (finishPending) return;
            finishPending = true;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= CaptureError;
            InputSystem.onBeforeUpdate -= Inject;
            if (touchscreen != null && touchscreen.added) InputSystem.RemoveDevice(touchscreen);
            if (hadGyroscopePreference) PlayerPrefs.SetInt("Andes.Gyroscope", previousGyroscope);
            else PlayerPrefs.DeleteKey("Andes.Gyroscope");
            PlayerPrefs.Save();
            Application.runInBackground = previousBackground;
            InputSystem.settings.backgroundBehavior = previousInputBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousInputRouting;
            if (gameView != null) gameView.GetType().GetProperty("selectedSizeIndex", Members).SetValue(gameView, previousViewSize);
            string report = (errors.Count == 0 ? "APROBADO" : "FALLÓ") + "\n\n" + string.Join("\n", checks) +
                "\n\nCapturas\n" + string.Join("\n", captures) + "\n\nErrores\n" + string.Join("\n\n", errors);
            File.WriteAllText(Path.Combine(output, "Resultado.txt"), report);
            Debug.Log("[ANDES Móvil] " + output + "\n" + report);
            if (exitPlay) EditorApplication.isPlaying = false;
        }
    }
}

