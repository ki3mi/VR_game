#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Object = UnityEngine.Object;

namespace AndesSim.Editor
{
    /// <summary>
    /// Replays stage-one exploration through the simulator and real Input System
    /// events. It never calls selection/teleport callbacks directly.
    /// </summary>
    [InitializeOnLoad]
    public static class StageOneValidation
    {
        const string ActiveKey = "AndesSim.StageOneValidation.Active";
        const string DirectoryKey = "AndesSim.StageOneValidation.Directory";
        const string GameViewMaximizedKey = "AndesSim.StageOneValidation.GameViewMaximized";
        const string EvidenceRoot = "Docs/Evidencias_Etapa_01";
        static IEnumerator routine;
        static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        static double resumeAt;
        static double nextFrameAt;
        static double startedAt;
        static int frameNumber;
        static string runDirectory;
        static ValidationReport report;
        static ExplorationController controller;
        static XRInteractionSimulator simulator;
        static InputSettings.BackgroundBehavior oldBackgroundBehavior;
        static bool backgroundChanged;
        static bool finalizing;
        static readonly List<string> runtimeErrors = new List<string>();

        [Serializable]
        public class TestResult
        {
            public string id;
            public string description;
            public bool passed;
            public string observed;
        }

        [Serializable]
        public class FrameRecord
        {
            public string file;
            public double elapsedSeconds;
        }

        [Serializable]
        public class MovementRecord
        {
            public string key;
            public Vector3 before;
            public Vector3 after;
            public float distance;
            public float intendedDirectionProjection;
            public bool passed;
        }

        [Serializable]
        public class ValidationReport
        {
            public string timestampUtc;
            public string scene;
            public string unityVersion;
            public string simulatorType;
            public string inputMethod;
            public string state;
            public bool visualReviewRequired = true;
            public int teleports;
            public int visited;
            public int inspected;
            public bool ambiencePlaying;
            public int animatedRacks;
            public int ledsPerRack;
            public int overflowingInspectionTexts;
            public bool freeMovementEnabled;
            public Vector3 simulatorTranslationSpeeds;
            public float simulatorBodyMultiplier;
            public float mouseLookDegrees;
            public List<MovementRecord> movement = new List<MovementRecord>();
            public List<TestResult> tests = new List<TestResult>();
            public List<string> errors = new List<string>();
            public List<string> screenshots = new List<string>();
            public List<FrameRecord> frames = new List<FrameRecord>();
        }

        sealed class Delay
        {
            public readonly double seconds;
            public Delay(double value) { seconds = value; }
        }

        static StageOneValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Andes-Sim/Etapa 1/Validar en simulador")]
        public static void StartValidation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Sal del modo Play antes de iniciar la validación de la etapa 1.");
                return;
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "DataCenter_VR")
            {
                Debug.LogError("Abre DataCenter_VR antes de iniciar la validación.");
                return;
            }
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            runDirectory = Path.Combine(projectRoot, EvidenceRoot,
                "Run_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(Path.Combine(runDirectory, "frames"));
            SessionState.SetString(DirectoryKey, runDirectory);
            SessionState.SetBool(ActiveKey, true);
            var gameView = GetGameView();
            SessionState.SetBool(GameViewMaximizedKey, gameView != null && gameView.maximized);
            FocusGameView();
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                runDirectory = SessionState.GetString(DirectoryKey, "");
                runtimeErrors.Clear();
                report = new ValidationReport
                {
                    timestampUtc = DateTime.UtcNow.ToString("o"),
                    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                    unityVersion = Application.unityVersion,
                    simulatorType = typeof(XRInteractionSimulator).FullName,
                    inputMethod = "InputSystem.QueueStateEvent para teclado y ratón; movimiento libre WASD/QE y tracking del XR Interaction Simulator; selección por mirada con Space",
                    state = "running"
                };
                startedAt = EditorApplication.timeSinceStartup;
                nextFrameAt = startedAt + 2;
                frameNumber = 0;
                resumeAt = startedAt + 1.5;
                routines.Clear();
                routine = RunValidation();
                routines.Push(routine);
                finalizing = false;
                Application.logMessageReceived += OnRuntimeLog;
                EditorApplication.update += Tick;
                FocusGameView();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(ActiveKey, false);
                EditorApplication.update -= Tick;
                Application.logMessageReceived -= OnRuntimeLog;
                var gameView = GetGameView();
                if (gameView != null)
                    gameView.maximized = SessionState.GetBool(GameViewMaximizedKey, false);
                // A domain reload may discard the in-memory report. The report is
                // written before requesting exit from Play Mode.
                Debug.Log("ANDES-SIM: validación terminada. Evidencias: " +
                    SessionState.GetString(DirectoryKey, ""));
            }
        }

        static void Tick()
        {
            if (finalizing || !EditorApplication.isPlaying)
                return;
            double now = EditorApplication.timeSinceStartup;
            if (now - startedAt > 240)
            {
                runtimeErrors.Add("La validación excedió su límite de 240 segundos.");
                Finish(false);
                return;
            }
            if (controller != null && now >= nextFrameAt && frameNumber < 600)
            {
                string relative = "frames/frame_" + frameNumber.ToString("D5") + ".png";
                ScreenCapture.CaptureScreenshot(Path.Combine(runDirectory, relative));
                report.frames.Add(new FrameRecord { file = relative, elapsedSeconds = now - startedAt });
                frameNumber++;
                nextFrameAt = now + 0.25;
            }
            if (now < resumeAt)
                return;
            try
            {
                if (routines.Count == 0)
                {
                    Finish(true);
                    return;
                }
                var current = routines.Peek();
                if (!current.MoveNext())
                {
                    routines.Pop();
                    return;
                }
                if (current.Current is IEnumerator nested)
                    routines.Push(nested);
                else if (current.Current is Delay delay)
                    resumeAt = now + delay.seconds;
            }
            catch (Exception exception)
            {
                runtimeErrors.Add(exception.ToString());
                Finish(false);
            }
        }

        static IEnumerator RunValidation()
        {
            Application.runInBackground = true;
            oldBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            backgroundChanged = true;
            controller = Object.FindAnyObjectByType<ExplorationController>();
            simulator = Object.FindAnyObjectByType<XRInteractionSimulator>();
            double timeout = EditorApplication.timeSinceStartup + 15;
            while ((controller == null || simulator == null || InputSystem.GetDevice<XRSimulatedHMD>() == null) &&
                   EditorApplication.timeSinceStartup < timeout)
            {
                yield return new Delay(0.2);
                controller = Object.FindAnyObjectByType<ExplorationController>();
                simulator = Object.FindAnyObjectByType<XRInteractionSimulator>();
            }
            Require(controller != null, "No se encontró ExplorationController.");
            Require(simulator != null, "No se encontró XR Interaction Simulator.");
            Require(InputSystem.GetDevice<XRSimulatedHMD>() != null, "El simulador no creó un XRSimulatedHMD.");
            Require(Keyboard.current != null && Mouse.current != null, "Faltan dispositivos de teclado o ratón.");
            Require(controller.viewCamera != null && controller.xrOrigin != null, "Faltan cámara u origen XR.");

            // Exercise actual simulator input before orienting the rig for the
            // deterministic tour. Rotation remains driven by simulated tracking.
            yield return Press(Key.H);
            Quaternion before = controller.viewCamera.transform.localRotation;
            SendKeys(Key.RightArrow);
            yield return new Delay(0.25);
            SendKeys();
            yield return new Delay(0.2);
            float headRotation = Quaternion.Angle(before, controller.viewCamera.transform.localRotation);
            Quaternion mouseBefore = controller.viewCamera.transform.localRotation;
            var lookState = new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f), delta = new Vector2(12, 0) };
            InputSystem.QueueStateEvent(Mouse.current, lookState.WithButton(MouseButton.Right));
            yield return new Delay(0.1);
            InputSystem.QueueStateEvent(Mouse.current,
                new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f) });
            yield return new Delay(0.15);
            report.mouseLookDegrees = Quaternion.Angle(mouseBefore, controller.viewCamera.transform.localRotation);
            Add("E1-P01", "Inicio y orientación con el simulador", headRotation > 0.5f && report.mouseLookDegrees > 0.1f,
                "XRSimulatedHMD presente; giro rastreado por entrada RightArrow: " + headRotation.ToString("F2") +
                "°; giro por clic derecho y ratón: " + report.mouseLookDegrees.ToString("F2") +
                "°; altura de cámara: " + controller.viewCamera.transform.position.y.ToString("F2") + " m.");
            yield return Capture("01_entrada.png");

            report.simulatorTranslationSpeeds = new Vector3(simulator.translateXSpeed, simulator.translateYSpeed, simulator.translateZSpeed);
            report.simulatorBodyMultiplier = simulator.bodyTranslateMultiplier;
            report.freeMovementEnabled = report.simulatorTranslationSpeeds.x > 0 &&
                report.simulatorTranslationSpeeds.y > 0 && report.simulatorTranslationSpeeds.z > 0 &&
                Object.FindObjectsByType<XRInteractionSimulator>().Length == 1;
            Vector3 movementStart = controller.viewCamera.transform.position;
            foreach (Key key in new[] { Key.W, Key.S, Key.A, Key.D, Key.E, Key.Q })
            {
                Vector3 expectedDirection;
                Vector3 forward = Vector3.ProjectOnPlane(controller.viewCamera.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(controller.viewCamera.transform.right, Vector3.up).normalized;
                switch (key)
                {
                    case Key.W: expectedDirection = forward; break;
                    case Key.S: expectedDirection = -forward; break;
                    case Key.A: expectedDirection = -right; break;
                    case Key.D: expectedDirection = right; break;
                    case Key.E: expectedDirection = Vector3.up; break;
                    default: expectedDirection = Vector3.down; break;
                }
                var record = new MovementRecord { key = key.ToString(), before = controller.viewCamera.transform.position };
                SendKeys(key);
                yield return new Delay(0.25);
                SendKeys();
                yield return new Delay(0.15);
                record.after = controller.viewCamera.transform.position;
                Vector3 displacement = record.after - record.before;
                record.distance = displacement.magnitude;
                record.intendedDirectionProjection = Vector3.Dot(displacement, expectedDirection);
                record.passed = record.distance > 0.05f && record.intendedDirectionProjection > 0.04f;
                report.movement.Add(record);
            }
            yield return Capture("movimiento_libre.png");
            // Opposing key pairs restore the pose approximately. Remove only
            // the tiny timing remainder as deterministic setup for the tour;
            // all displacement measurements above come from simulator input.
            controller.xrOrigin.transform.position += movementStart - controller.viewCamera.transform.position;
            Physics.SyncTransforms();
            bool freeMovementPassed = report.freeMovementEnabled && report.movement.All(item => item.passed);

            var portal = FindTarget("Portal_Entrada");
            yield return AimAndSelect(portal);
            int portalTeleports = controller.TeleportsPerformed;
            Require(portalTeleports > 0, "El portal no produjo un teletransporte.");
            yield return Capture("02_portal.png");

            var destinationNames = new[]
            {
                "TP_Entrada", "TP_Racks", "TP_Terminal", "TP_Switch", "TP_Impresora", "TP_Controles"
            };
            int destinationsReached = 0;
            foreach (string destinationName in destinationNames)
            {
                var target = FindTarget(destinationName);
                yield return AimAndSelect(target);
                Vector3 destination = GetDestination(target);
                Vector3 actual = controller.viewCamera.transform.position;
                float horizontalError = Vector2.Distance(new Vector2(actual.x, actual.z),
                    new Vector2(destination.x, destination.z));
                Require(horizontalError < 0.15f, destinationName + " no alcanzado; error horizontal " + horizontalError);
                destinationsReached++;
                yield return Capture(destinationName + ".png");
            }
            yield return AimAndSelect(FindTarget("TP_Racks"));
            yield return AimAndSelect(FindTarget("TP_Entrada"));
            Add("E1-P02", "Portal, seis destinos y regreso", destinationsReached == 6,
                "Portal usado; " + destinationsReached + "/6 destinos alcanzados por selección con Space; regreso a TP_Entrada.");
            Add("E1-P03", "Movimiento libre y teletransporte", freeMovementPassed && controller.TeleportsPerformed >= 7,
                "Un único simulador; velocidades XYZ: " + report.simulatorTranslationSpeeds.ToString("F2") +
                "; multiplicador corporal: " + report.simulatorBodyMultiplier.ToString("F1") +
                "; desplazamientos reales: " + string.Join(", ", report.movement.Select(item => item.key + "=" + item.distance.ToString("F3") + " m")) +
                "; teletransportes acumulados: " + controller.TeleportsPerformed + ".");

            string[] inventory = { "Rack_01", "Rack_02", "Rack_03", "Rack_04", "Terminal_SWIFT", "Switch_Barato", "Impresora" };
            bool inventoryPresent = inventory.All(name => GameObject.Find(name) != null);
            var ambience = Object.FindAnyObjectByType<ServerAmbience>();
            report.ambiencePlaying = ambience != null && ambience.GetComponent<AudioSource>().isPlaying;
            var rackLights = Object.FindObjectsByType<LedBlink>();
            report.animatedRacks = rackLights.Length;
            report.ledsPerRack = rackLights.Length == 0 ? 0 : rackLights.Min(item => item.leds != null ? item.leds.Length : 0);
            inventoryPresent &= report.ambiencePlaying && report.animatedRacks == 4 && report.ledsPerRack >= 10;
            Add("E1-P04", "Inventario del centro de datos", inventoryPresent,
                string.Join(", ", inventory.Select(name => name + ": " + (GameObject.Find(name) != null ? "presente" : "ausente"))) +
                "; audio ambiental reproduciéndose: " + report.ambiencePlaying + "; racks animados: " + report.animatedRacks +
                "; mínimo LEDs/rack: " + report.ledsPerRack + ".");

            string[] inspectNames = { "Rack_01", "Terminal_SWIFT", "Switch_Barato", "Impresora", "Panel_Controles" };
            string[] inspectStations = { "TP_Racks", "TP_Terminal", "TP_Switch", "TP_Impresora", "TP_Controles" };
            bool inspectedAll = true;
            bool panelsClosed = true;
            for (int i = 0; i < inspectNames.Length; i++)
            {
                if (i == 0)
                    yield return AimAndSelect(FindTarget("TP_Racks"));
                else
                    yield return AimAndSelect(FindTarget(inspectStations[i]));
                var target = FindTarget(inspectNames[i]);
                yield return AimAndSelect(target);
                bool visible = controller.PanelVisible;
                string expectedId = GetStringField(target, "uniqueId");
                bool selected = controller.LastSelectedId == expectedId;
                inspectedAll &= visible && selected;
                Transform panel = controller.viewCamera.transform.Find("Exploration_HUD/InspectionPanel");
                if (panel != null)
                {
                    foreach (var text in panel.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        if (text.isTextOverflowing)
                            report.overflowingInspectionTexts++;
                    }
                }
                yield return Capture("inspeccion_" + inspectNames[i] + ".png");
                yield return new Delay(0.7);
                yield return Press(Key.Escape);
                panelsClosed &= !controller.PanelVisible;
                Require(!controller.PanelVisible, "Escape no cerró la inspección de " + inspectNames[i]);
            }
            Add("E1-P05", "Cinco objetos seleccionados con mirada", inspectedAll && controller.InspectedCount >= 5,
                "Objetos: " + string.Join(", ", inspectNames) + "; inspecciones únicas: " + controller.InspectedCount + ".");
            Add("E1-P06", "Apertura y cierre de fichas", inspectedAll && panelsClosed && report.overflowingInspectionTexts == 0,
                "Las cinco fichas se abrieron y cerraron con Escape; textos TMP desbordados: " +
                report.overflowingInspectionTexts + ". Capturas guardadas para revisión visual de legibilidad.");
            yield return AimAndSelect(FindTarget("TP_Racks"));
            yield return AimAt(FindTarget("Terminal_SWIFT"));
            yield return Capture("04_interior.png");
            yield return AimAndSelect(FindTarget("TP_Entrada"));
            yield return Capture("03_regreso.png");
            yield return new Delay(0.8);
        }

        static IEnumerator AimAndSelect(ExplorationTarget target)
        {
            Require(!controller.PanelVisible, "Se intentó navegar con una ficha abierta.");
            yield return AimAt(target);
            Require(controller.CurrentTarget == target,
                "La mirada no alcanza " + target.name + ". Objetivo actual: " +
                (controller.CurrentTarget != null ? controller.CurrentTarget.name : "ninguno") + ". " + DescribeAim(target));
            Require(target.IsHighlighted, "El objetivo no ofrece respuesta visual al apuntarlo: " + target.name);
            int teleportsBefore = controller.TeleportsPerformed;
            yield return Press(Key.Space);
            yield return new Delay(0.25);
            Require(controller.LastSelectedId == GetStringField(target, "uniqueId"),
                "La pulsación Space no seleccionó " + target.name);
            // Inspections preserve the current position; navigation must change
            // the telemetry counter even when selecting the current station.
            if (target.name.StartsWith("TP_") || target.name == "Portal_Entrada")
                Require(controller.TeleportsPerformed > teleportsBefore, "No se procesó el teletransporte " + target.name);
        }

        static IEnumerator AimAt(ExplorationTarget target)
        {
            Vector3 aim = GetAimPoint(target);
            Transform camera = controller.viewCamera.transform;
            Transform origin = controller.xrOrigin.transform;
            Vector3 direction = aim - camera.position;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            // Rotation of the origin is test setup. Pitch is produced by mouse
            // input consumed by the actual XR Interaction Simulator.
            float localYaw = camera.localEulerAngles.y;
            origin.rotation = Quaternion.Euler(0, yaw - localYaw, 0);
            yield return new Delay(0.1);
            for (int correction = 0; correction < 8; correction++)
            {
                direction = aim - camera.position;
                float wantedPitch = -Mathf.Atan2(direction.y,
                    new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                float currentPitch = Mathf.DeltaAngle(0, camera.localEulerAngles.x);
                float delta = Mathf.DeltaAngle(currentPitch, wantedPitch);
                if (Mathf.Abs(delta) < 0.35f)
                    break;
                float sign = simulator.rotateYInvert ? 1 : -1;
                float mouseDelta = delta / Mathf.Max(0.001f, simulator.rotateYSensitivity) / sign;
                var state = new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f), delta = new Vector2(0, mouseDelta) };
                state = state.WithButton(MouseButton.Right);
                InputSystem.QueueStateEvent(Mouse.current, state);
                yield return new Delay(0.08);
                InputSystem.QueueStateEvent(Mouse.current,
                    new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f) });
                yield return new Delay(0.08);
            }
            Physics.SyncTransforms();
            controller.RefreshGaze();
            yield return new Delay(0.15);
        }

        static IEnumerator Press(Key key)
        {
            SendKeys(key);
            yield return new Delay(0.12);
            SendKeys();
            yield return new Delay(0.14);
        }

        static void SendKeys(params Key[] keys)
        {
            if (Keyboard.current != null)
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
        }

        static IEnumerator Capture(string filename)
        {
            yield return new Delay(0.2);
            ScreenCapture.CaptureScreenshot(Path.Combine(runDirectory, filename));
            report.screenshots.Add(filename);
            yield return new Delay(0.35);
        }

        static ExplorationTarget FindTarget(string objectName)
        {
            GameObject gameObject = GameObject.Find(objectName);
            Require(gameObject != null, "No existe el objeto " + objectName);
            var target = gameObject.GetComponent<ExplorationTarget>();
            Require(target != null, "Falta ExplorationTarget en " + objectName);
            return target;
        }

        static string DescribeAim(ExplorationTarget target)
        {
            var camera = controller.viewCamera.transform;
            string hitDescription = "ninguno";
            if (Physics.Raycast(camera.position, camera.forward, out var hit, 50, ~0, QueryTriggerInteraction.Collide))
                hitDescription = hit.collider.name + " @ " + hit.point.ToString("F3");
            return "cameraPos=" + camera.position.ToString("F3") + "; cameraWorldEuler=" + camera.eulerAngles.ToString("F3") +
                "; cameraLocalEuler=" + camera.localEulerAngles.ToString("F3") + "; originEuler=" +
                controller.xrOrigin.transform.eulerAngles.ToString("F3") + "; aim=" + GetAimPoint(target).ToString("F3") +
                "; primerCollider=" + hitDescription;
        }

        static Vector3 GetAimPoint(ExplorationTarget target)
        {
            var field = typeof(ExplorationTarget).GetField("aimPoint", BindingFlags.Public | BindingFlags.Instance);
            object value = field?.GetValue(target);
            if (value is Transform aimTransform)
                return aimTransform.position;
            if (value is Vector3 point)
                return point;
            Collider collider = target.GetComponent<Collider>();
            return collider != null ? collider.bounds.center : target.transform.position;
        }

        static Vector3 GetDestination(ExplorationTarget target)
        {
            var field = typeof(ExplorationTarget).GetField("destination", BindingFlags.Public | BindingFlags.Instance);
            object value = field?.GetValue(target);
            if (value is Transform destinationTransform)
                return destinationTransform.position;
            if (value is Vector3 point)
                return point;
            return target.transform.position;
        }

        static string GetStringField(ExplorationTarget target, string name)
        {
            return typeof(ExplorationTarget).GetField(name)?.GetValue(target)?.ToString() ?? "";
        }

        static void Add(string id, string description, bool passed, string observed)
        {
            report.tests.Add(new TestResult { id = id, description = description, passed = passed, observed = observed });
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        static void OnRuntimeLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                runtimeErrors.Add(type + ": " + condition + "\n" + stackTrace);
        }

        static void Finish(bool completed)
        {
            if (finalizing)
                return;
            finalizing = true;
            SendKeys();
            if (Mouse.current != null)
                InputSystem.QueueStateEvent(Mouse.current, new MouseState());
            if (backgroundChanged)
            {
                InputSystem.settings.backgroundBehavior = oldBackgroundBehavior;
                backgroundChanged = false;
            }
            if (report != null)
            {
                Add("E1-P07", "Ejecución del recorrido sin errores", completed && runtimeErrors.Count == 0,
                    "Recorrido terminado: " + completed + "; errores capturados en Play Mode: " + runtimeErrors.Count + ". Consola del Editor se revisa además por MCP después de salir.");
                report.teleports = controller != null ? controller.TeleportsPerformed : 0;
                report.visited = controller != null ? controller.VisitedCount : 0;
                report.inspected = controller != null ? controller.InspectedCount : 0;
                report.errors = new List<string>(runtimeErrors);
                report.state = completed && report.tests.Count == 7 && report.tests.All(test => test.passed)
                    ? "functional_checks_passed_visual_review_pending" : "failed";
                WriteReport();
            }
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnRuntimeLog;
            EditorApplication.isPlaying = false;
        }

        static void WriteReport()
        {
            Directory.CreateDirectory(runDirectory);
            File.WriteAllText(Path.Combine(runDirectory, "resultados.json"), JsonUtility.ToJson(report, true), Encoding.UTF8);
            var text = new StringBuilder();
            text.AppendLine("# Validación de etapa 1 con XR Interaction Simulator\n");
            text.AppendLine("- Fecha UTC: " + report.timestampUtc);
            text.AppendLine("- Responsable: validación automatizada ejecutada por Codex mediante Unity MCP.");
            text.AppendLine("- Escena: `" + report.scene + "`.");
            text.AppendLine("- Unity: `" + report.unityVersion + "`.");
            text.AppendLine("- Entrada: " + report.inputMethod + ".");
            text.AppendLine("- Estado: `" + report.state + "`.");
            text.AppendLine("- Teletransportes: " + report.teleports + "; destinos visitados: " + report.visited + "; objetos inspeccionados: " + report.inspected + ".\n");
            text.AppendLine("## Resultados observados\n");
            text.AppendLine("| Caso | Resultado | Observación |");
            text.AppendLine("| --- | --- | --- |");
            foreach (var test in report.tests)
                text.AppendLine("| " + test.id + " | " + (test.passed ? "Cumple funcionalmente" : "Falla") + " | " + Escape(test.observed) + " |");
            text.AppendLine("\n## Evidencias visuales\n");
            text.AppendLine("Las capturas y los frames proceden de Game View durante la ejecución real. La legibilidad de las fichas requiere revisar las imágenes. La comodidad en visor y Android no se certifican con este simulador.\n");
            foreach (string screenshot in report.screenshots)
                text.AppendLine("- [" + screenshot + "](" + Path.Combine(runDirectory, screenshot).Replace('\\', '/') + ")");
            text.AppendLine("\nSe guardaron " + report.frames.Count + " frames con sus tiempos en `resultados.json`, para elaborar un video del recorrido.\n");
            if (runtimeErrors.Count > 0)
            {
                text.AppendLine("## Errores capturados\n");
                foreach (string error in runtimeErrors)
                    text.AppendLine("```text\n" + error + "\n```\n");
            }
            File.WriteAllText(Path.Combine(runDirectory, "Resultados_Etapa_01.md"), text.ToString(), Encoding.UTF8);
        }

        static string Escape(string value) => value.Replace("|", "/").Replace("\r", " ").Replace("\n", " ");

        static void FocusGameView()
        {
            var gameView = GetGameView();
            if (gameView == null) return;
            gameView.maximized = true;
            gameView.Focus();
        }

        static EditorWindow GetGameView()
        {
            Type gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            return gameView != null ? EditorWindow.GetWindow(gameView) : null;
        }
    }
}
#endif
