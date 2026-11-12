#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Object = UnityEngine.Object;

namespace AndesSim.Editor
{
    [InitializeOnLoad]
    public static class StageTwoValidation
    {
        const string ActiveKey = "AndesSim.StageTwoValidation.Active";
        const string FolderKey = "AndesSim.StageTwoValidation.Folder";
        const string MaximizeKey = "AndesSim.StageTwoValidation.Maximized";
        const float TestDurationSeconds = 24f;
        static readonly DateTime StartTime = new DateTime(2016, 2, 4, 20, 36, 0);
        static readonly DateTime OrdersDeadline = new DateTime(2016, 2, 4, 22, 6, 0);
        static readonly DateTime EndTime = new DateTime(2016, 2, 5, 3, 59, 0);
        static readonly string[] StationNames =
        {
            "TP_Entrada", "TP_Racks", "TP_Terminal", "TP_Switch", "TP_Impresora", "TP_Controles"
        };
        static readonly Stack<IEnumerator> Steps = new Stack<IEnumerator>();
        static Report report;
        static ExplorationController explorer;
        static XRInteractionSimulator simulator;
        static GameManager game;
        static UIManager ui;
        static ScenarioData originalData;
        static ScenarioData testData;
        static TimelineTrace timeline;
        static string output;
        static double beganAt;
        static double resumeAt;
        static double nextFrameAt;
        static bool recordFrames;
        static bool ending;
        static bool settingsOverridden;
        static bool previousRunInBackground;
        static InputSettings.BackgroundBehavior previousBackgroundBehavior;

        [Serializable]
        sealed class Result
        {
            public string id;
            public bool passed;
            public string observed;
        }

        [Serializable]
        sealed class Frame
        {
            public string file;
            public double elapsedSeconds;
        }

        [Serializable]
        sealed class Report
        {
            public string timestampUtc;
            public string scene;
            public string unityVersion;
            public string simulatorType;
            public string state;
            public float configuredDurationSeconds;
            public float testDurationSeconds;
            public string firstOrderMilestone;
            public string finalClock;
            public string[] evidenceIds;
            public int firstOrders;
            public int secondOrders;
            public int stationsVisited;
            public int objectsInspected;
            public float freeMovementDistance;
            public bool visualReviewRequired;
            public List<Result> tests = new List<Result>();
            public List<string> screenshots = new List<string>();
            public List<string> errors = new List<string>();
            public List<Frame> frames = new List<Frame>();
        }

        sealed class TimelineTrace
        {
            public DateTime? firstTime;
            public DateTime? lastTime;
            public DateTime? fullOrdersAt;
            public int highestOrders;
            public bool crossedMidnight;
            public bool ordered = true;
            public bool captureMilestones;

            public void Observe(GameManager manager)
            {
                if (manager.State == ScenarioState.Preparacion) return;
                DateTime now = manager.CurrentTime;
                int count = manager.OrdersSent;
                if (!firstTime.HasValue) firstTime = now;
                if ((lastTime.HasValue && now < lastTime.Value) || count < highestOrders)
                    ordered = false;
                lastTime = now;
                highestOrders = Mathf.Max(count, highestOrders);
                if (count >= 36 && !fullOrdersAt.HasValue)
                {
                    fullOrdersAt = now;
                    if (captureMilestones) SaveScreenshot("03_36_ordenes.png");
                }
                if (now.Date > StartTime.Date && !crossedMidnight)
                {
                    crossedMidnight = true;
                    if (captureMilestones) SaveScreenshot("05_cambio_de_fecha.png");
                }
            }
        }

        static StageTwoValidation() => EditorApplication.playModeStateChanged += PlayStateChanged;

        [MenuItem("Andes-Sim/Etapa 2/Validar en simulador")]
        public static void StartValidation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "DataCenter_VR")
            {
                Debug.LogWarning("Abre DataCenter_VR en Edit Mode para ejecutar la validación de etapa 2.");
                return;
            }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            output = Path.Combine(root, "Docs", "Evidencias_Etapa_02", "Run_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(Path.Combine(output, "frames"));
            SessionState.SetString(FolderKey, output);
            SessionState.SetBool(ActiveKey, true);
            var view = GetGameView();
            SessionState.SetBool(MaximizeKey, view != null && view.maximized);
            FocusGameView();
            EditorApplication.isPlaying = true;
        }

        static void PlayStateChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                output = SessionState.GetString(FolderKey, "");
                report = new Report
                {
                    timestampUtc = DateTime.UtcNow.ToString("o"),
                    scene = SceneManager.GetActiveScene().path,
                    unityVersion = Application.unityVersion,
                    simulatorType = typeof(XRInteractionSimulator).FullName,
                    testDurationSeconds = TestDurationSeconds,
                    state = "running"
                };
                beganAt = EditorApplication.timeSinceStartup;
                resumeAt = beganAt + 1.5;
                ending = false;
                recordFrames = false;
                timeline = null;
                Steps.Clear();
                Steps.Push(Validate());
                Application.logMessageReceived += CaptureError;
                EditorApplication.update += Tick;
                FocusGameView();
            }
            else if (change == PlayModeStateChange.ExitingPlayMode && !ending && report != null)
            {
                report.errors.Add("El recorrido se interrumpió antes de completar los ocho casos.");
                Finish(false, false);
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(ActiveKey, false);
                var view = GetGameView();
                if (view != null) view.maximized = SessionState.GetBool(MaximizeKey, false);
                Debug.Log("ANDES-SIM: resultados de etapa 2 en " + SessionState.GetString(FolderKey, ""));
            }
        }

        static void Tick()
        {
            if (ending || !EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            try
            {
                Require(now - beganAt < 180, "La prueba superó su límite de 180 segundos.");
                timeline?.Observe(game);
                if (recordFrames && now >= nextFrameAt)
                {
                    string filename = "frames/frame_" + report.frames.Count.ToString("D5") + ".png";
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, filename));
                    report.frames.Add(new Frame { file = filename, elapsedSeconds = now - beganAt });
                    nextFrameAt = now + .25;
                }
                if (now < resumeAt) return;
                if (Steps.Count == 0)
                {
                    Finish(true);
                    return;
                }
                IEnumerator step = Steps.Peek();
                if (!step.MoveNext()) Steps.Pop();
                else if (step.Current is IEnumerator nested) Steps.Push(nested);
                else if (step.Current is double delay) resumeAt = now + delay;
            }
            catch (Exception error)
            {
                report.errors.Add(error.ToString());
                Finish(false);
            }
        }

        static IEnumerator Validate()
        {
            previousRunInBackground = Application.runInBackground;
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settingsOverridden = true;
            explorer = Object.FindAnyObjectByType<ExplorationController>();
            simulator = Object.FindAnyObjectByType<XRInteractionSimulator>();
            game = GameManager.I;
            ui = Object.FindAnyObjectByType<UIManager>();
            Require(explorer != null && simulator != null && game != null && ui != null, "Faltan componentes del escenario o del simulador.");
            Require(InputSystem.GetDevice<XRSimulatedHMD>() != null && Keyboard.current != null && Mouse.current != null,
                "El simulador debe generar el dispositivo XRSimulatedHMD y recibir teclado y ratón.");
            Require(game.data != null, "GameManager no tiene el asset ScenarioData.");
            originalData = game.data;
            report.configuredDurationSeconds = originalData.durationSeconds;
            testData = Object.Instantiate(originalData);
            testData.durationSeconds = TestDurationSeconds;
            game.data = testData;
            Require(game.mode == SimMode.Ataque && game.State == ScenarioState.Preparacion,
                "El escenario debe abrir en preparación del ataque.");

            yield return Press(Key.H);
            Vector3 initialPosition = explorer.viewCamera.transform.position;
            yield return Press(Key.W, .25);
            report.freeMovementDistance = Vector3.Distance(initialPosition, explorer.viewCamera.transform.position);
            yield return Press(Key.S, .25);
            explorer.xrOrigin.transform.position += initialPosition - explorer.viewCamera.transform.position;
            Physics.SyncTransforms();

            yield return Select("Portal_Entrada");
            foreach (string station in StationNames) yield return GoTo(station, true);
            yield return GoTo("TP_Racks");
            yield return Select("Rack_01");
            Require(explorer.PanelVisible, "Rack_01 debe conservar su ficha.");
            yield return Press(Key.Escape);
            yield return GoTo("TP_Controles");
            yield return Select("Panel_Controles");
            Require(explorer.PanelVisible, "Panel_Controles debe conservar su ficha.");
            yield return Press(Key.Escape);
            yield return GoTo("TP_Terminal");
            yield return Select("Terminal_SWIFT");
            bool stayedReady = game.State == ScenarioState.Preparacion && game.OrdersSent == 0 &&
                Minute(game.CurrentTime) == StartTime;
            Require(stayedReady && PanelText().IndexOf("Iniciar reconstrucci", StringComparison.OrdinalIgnoreCase) >= 0,
                "La terminal debe mostrar la acción de inicio durante preparación.");
            SaveScreenshot("01_preparacion_terminal.png");
            recordFrames = true;
            nextFrameAt = EditorApplication.timeSinceStartup;
            yield return .65d;

            var first = new TimelineTrace { captureMilestones = true };
            timeline = first;
            yield return Press(Key.Digit1);
            Require(game.State == ScenarioState.Ejecucion && !explorer.PanelVisible,
                "La tecla 1 de la terminal debe iniciar la reconstrucción y cerrar su ficha.");
            Add("E2-P01", stayedReady && first.firstTime.HasValue && Minute(first.firstTime.Value) == StartTime,
                "Preparación se mantuvo hasta el inicio. Primer minuto observado: " + Stamp(first.firstTime) + ".");
            SaveScreenshot("02_reconstruccion_en_curso.png");

            yield return Select("Terminal_SWIFT");
            Require(ui.LogVisible, "La terminal debe mostrar el log durante el ataque.");
            string log = PanelText();
            bool gapVisible = Regex.IsMatch(log, "borrad|hueco", RegexOptions.IgnoreCase) && Regex.IsMatch(log, @"\d{2}:\d{2}");
            yield return Press(Key.Digit1);
            bool logRegistered = game.HasEvidence("log_huecos") && game.EvidenceCount == 1;
            yield return Press(Key.Digit1);
            Add("E2-P04", gapVisible && logRegistered && game.EvidenceCount == 1,
                "El log muestra horas y huecos de registros. Dos pulsaciones dejaron una evidencia log_huecos.");
            SaveScreenshot("04_log_con_huecos.png");
            yield return .65d;
            yield return Press(Key.Escape);

            yield return RecordObject("TP_Switch", "Switch_Barato", "switch_barato", 2);
            yield return RecordObject("TP_Impresora", "Impresora", "impresora_apagada", 3);
            report.evidenceIds = new[] { "log_huecos", "switch_barato", "impresora_apagada" }
                .Where(game.HasEvidence).ToArray();
            SaveScreenshot("06_evidencias_registradas.png");

            yield return AwaitResult();
            report.firstOrders = game.OrdersSent;
            report.firstOrderMilestone = Stamp(first.fullOrdersAt);
            report.finalClock = Stamp(game.CurrentTime);
            Add("E2-P02", first.fullOrdersAt.HasValue && Minute(first.fullOrdersAt.Value) <= OrdersDeadline &&
                first.highestOrders == 36 && first.ordered && report.firstOrders == 36,
                "Primera observación de 36 órdenes: " + report.firstOrderMilestone + ". Máximo observado: " + first.highestOrders + ".");
            DateTime frozenClock = game.CurrentTime;
            yield return .35d;
            Add("E2-P03", first.crossedMidnight && Minute(frozenClock) == EndTime && game.CurrentTime == frozenClock,
                "Cruce al 05/02 observado y reloj detenido en " + report.finalClock + ".");
            report.stationsVisited = explorer.VisitedCount;
            report.objectsInspected = explorer.InspectedCount;
            timeline = null;

            // Confirm that the log is still accessible after the clock stops.
            yield return GoTo("TP_Terminal");
            yield return Select("Terminal_SWIFT");
            Require(ui.LogVisible && PanelText().IndexOf("borrad", StringComparison.OrdinalIgnoreCase) >= 0,
                "Los huecos del log deben seguir disponibles después del cierre.");
            yield return Press(Key.Digit1);
            bool confirmationVisible = Regex.IsMatch(ui.StatusText, @"EVIDENCIAS\s+3\s*/\s*3") &&
                ui.StatusText.IndexOf("Evidencia ya registrada", StringComparison.OrdinalIgnoreCase) >= 0;
            Add("E2-P05", report.evidenceIds.Length == 3 && game.EvidenceCount == 3 && confirmationVisible,
                "Tres evidencias únicas y confirmación en el HUD después de repetir cada registro y reabrir el log tras el cierre: " +
                string.Join(", ", report.evidenceIds) + ".");
            SaveScreenshot("06b_confirmacion_en_log.png");
            yield return .4d;
            yield return Press(Key.Escape);
            if (!ui.SummaryVisible) yield return Press(Key.F);
            string summary = PanelText();
            bool completeSummary = ui.SummaryVisible && Regex.IsMatch(summary, @"\b36\b") &&
                Regex.IsMatch(summary, @"US\$\s*951", RegexOptions.IgnoreCase) && summary.Contains("20:36") &&
                summary.Contains("03:59") && Regex.IsMatch(summary, "intent", RegexOptions.IgnoreCase);
            Add("E2-P06", completeSummary, "Resumen visible con horas, 36 órdenes intentadas y US$951 millones. " + Flatten(summary));
            SaveScreenshot("07_resumen_ataque.png");
            yield return 1.2d;
            recordFrames = false;

            yield return Press(Key.Digit1);
            bool reset = game.State == ScenarioState.Preparacion && game.OrdersSent == 0 && game.EvidenceCount == 0 &&
                game.DefenseCount == 0 && Minute(game.CurrentTime) == StartTime && !ui.SummaryVisible;
            Require(reset, "La tecla 1 del resumen debe restablecer preparación, reloj, órdenes, evidencias y defensas.");
            SaveScreenshot("08_reinicio.png");
            yield return GoTo("TP_Terminal");
            yield return Select("Terminal_SWIFT");
            var second = new TimelineTrace();
            timeline = second;
            yield return ClickPanelButton("InspectionAction1");
            Require(game.State == ScenarioState.Ejecucion && !explorer.PanelVisible,
                "El clic en la acción de la terminal debe iniciar el segundo ataque y cerrar la ficha.");
            yield return AwaitResult();
            report.secondOrders = game.OrdersSent;
            Add("E2-P07", reset && report.secondOrders == 36 && second.highestOrders == 36 && second.ordered &&
                game.EvidenceCount == 0 && Minute(game.CurrentTime) == EndTime,
                "Reinicio por tecla 1 con cero órdenes y evidencias. Segundo ataque iniciado por clic: " +
                report.secondOrders + " órdenes y " + game.EvidenceCount + " evidencias.");
            if (!ui.SummaryVisible) yield return Press(Key.F);
            SaveScreenshot("09_segundo_resultado.png");
            yield return .5d;
            timeline = null;
            Add("E2-P08", report.stationsVisited == 6 && report.objectsInspected == 5 &&
                report.freeMovementDistance > .05f && report.errors.Count == 0,
                "Regresión con portal, " + report.stationsVisited + " estaciones, " + report.objectsInspected +
                " objetos y " + report.freeMovementDistance.ToString("F3") + " m con W. Errores de ejecución: " + report.errors.Count + ".");
        }

        static IEnumerator RecordObject(string station, string name, string evidence, int count)
        {
            yield return GoTo(station);
            yield return Select(name);
            Require(explorer.PanelVisible, "No se abrió la ficha de " + name + ".");
            yield return Press(Key.Digit1);
            Require(game.HasEvidence(evidence) && game.EvidenceCount == count, "No se registró " + evidence + ".");
            yield return Press(Key.Digit1);
            yield return Press(Key.Escape);
            yield return Select(name);
            yield return Press(Key.Digit1);
            Require(game.EvidenceCount == count, "La evidencia " + evidence + " se duplicó.");
            yield return Press(Key.Escape);
        }

        static IEnumerator AwaitResult()
        {
            double deadline = EditorApplication.timeSinceStartup + TestDurationSeconds + 8;
            while (game.State == ScenarioState.Ejecucion && EditorApplication.timeSinceStartup < deadline)
                yield return .15d;
            Require(game.State == ScenarioState.Resultado, "El reloj no completó la reconstrucción.");
        }

        static IEnumerator GoTo(string name, bool force = false)
        {
            var target = FindTarget(name);
            Vector3 destination = target.destination.position;
            if (!force && HorizontalDistance(explorer.viewCamera.transform.position, destination) < .12f) yield break;
            yield return Select(name);
            Require(HorizontalDistance(explorer.viewCamera.transform.position, destination) < .15f,
                "La cámara no llegó a " + name + ".");
        }

        static IEnumerator Select(string name)
        {
            var target = FindTarget(name);
            Require(!explorer.PanelVisible, "Cierra la ficha antes de seleccionar " + name + ".");
            yield return AimAt(target.AimPosition);
            Require(explorer.CurrentTarget == target && target.IsHighlighted,
                "La mirada no alcanzó " + name + ". Objetivo actual: " + (explorer.CurrentTarget == null ? "ninguno" : explorer.CurrentTarget.name) + ".");
            yield return Press(Key.Space);
            Require(explorer.LastSelectedId == target.uniqueId, "Espacio no seleccionó " + name + ".");
            yield return .2d;
        }

        static IEnumerator AimAt(Vector3 point)
        {
            Transform camera = explorer.viewCamera.transform;
            Vector3 direction = point - camera.position;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            // Yaw aligns the test rig. Pitch still passes through the real simulator.
            explorer.xrOrigin.transform.rotation = Quaternion.Euler(0, yaw - camera.localEulerAngles.y, 0);
            yield return .1d;
            for (int i = 0; i < 8; i++)
            {
                direction = point - camera.position;
                float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(camera.localEulerAngles.x, pitch);
                if (Mathf.Abs(delta) < .35f) break;
                float invert = simulator.rotateYInvert ? 1 : -1;
                var mouse = new MouseState
                {
                    position = new Vector2(Screen.width / 2f, Screen.height / 2f),
                    delta = new Vector2(0, delta / Mathf.Max(.001f, simulator.rotateYSensitivity) / invert)
                };
                InputSystem.QueueStateEvent(Mouse.current, mouse.WithButton(MouseButton.Right));
                yield return .08d;
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = mouse.position });
                yield return .08d;
            }
            Physics.SyncTransforms();
            yield return .15d;
        }

        static IEnumerator Press(Key key, double seconds = .12)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
            yield return seconds;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            yield return .16d;
        }

        static IEnumerator ClickPanelButton(string name)
        {
            var button = GameObject.Find(name)?.GetComponent<RectTransform>();
            Require(button != null, "No se encontró el botón visible " + name + ".");
            var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : explorer.viewCamera;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, button.TransformPoint(button.rect.center));
            var mouse = new MouseState { position = point };
            InputSystem.QueueStateEvent(Mouse.current, mouse.WithButton(MouseButton.Left));
            yield return .12d;
            InputSystem.QueueStateEvent(Mouse.current, mouse);
            yield return .16d;
        }

        static ExplorationTarget FindTarget(string name)
        {
            var target = GameObject.Find(name)?.GetComponent<ExplorationTarget>();
            Require(target != null, "No se encontró ExplorationTarget en " + name + ".");
            return target;
        }

        static string PanelText()
        {
            var panel = GameObject.Find("InspectionPanel");
            return panel == null ? "" : string.Join("\n", panel.GetComponentsInChildren<TMP_Text>()
                .Where(text => text.enabled).Select(text => text.text));
        }

        static float HorizontalDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        static DateTime Minute(DateTime time) => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
        static string Stamp(DateTime? time) => time.HasValue ? time.Value.ToString("dd/MM/yyyy HH:mm") : "sin observación";
        static string Flatten(string value) => value.Replace("\r", " ").Replace("\n", " ").Replace("|", "/");

        static void SaveScreenshot(string filename)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(output, filename));
            report.screenshots.Add(filename);
        }

        static void Add(string id, bool passed, string observed) => report.tests.Add(new Result { id = id, passed = passed, observed = observed });

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        static void CaptureError(string message, string stackTrace, LogType kind)
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                report.errors.Add(kind + ": " + message + "\n" + stackTrace);
        }

        static void Finish(bool complete, bool leavePlayMode = true)
        {
            if (ending) return;
            ending = true;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= CaptureError;
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            if (Mouse.current != null) InputSystem.QueueStateEvent(Mouse.current, new MouseState());
            if (settingsOverridden)
            {
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
                Application.runInBackground = previousRunInBackground;
                settingsOverridden = false;
            }
            if (testData != null)
            {
                if (game != null && game.data == testData) game.data = originalData;
                Object.Destroy(testData);
            }
            bool passed = complete && report.tests.Count == 8 && report.tests.All(test => test.passed) && report.errors.Count == 0;
            report.state = passed ? "functional_checks_passed_visual_review_pending" : "failed";
            report.visualReviewRequired = passed;
            report.tests.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            WriteReport();
            if (leavePlayMode) EditorApplication.isPlaying = false;
        }

        static void WriteReport()
        {
            File.WriteAllText(Path.Combine(output, "resultados.json"), JsonUtility.ToJson(report, true), Encoding.UTF8);
            var md = new StringBuilder("# Validación de etapa 2 con XR Interaction Simulator\n\n");
            md.AppendLine("- Fecha UTC: " + report.timestampUtc);
            md.AppendLine("- Responsable: validación automatizada ejecutada por Codex mediante Unity MCP.");
            md.AppendLine("- Escena: `" + report.scene + "`.");
            md.AppendLine("- Unity: `" + report.unityVersion + "`.");
            md.AppendLine("- Duración del recurso: " + report.configuredDurationSeconds + " s. Duración de prueba en copia temporal: " + report.testDurationSeconds + " s.");
            md.AppendLine("- Estado: `" + report.state + "`.\n");
            md.AppendLine("Las acciones se ejecutaron con eventos del Input System en Game View. El visor y las acciones se seleccionaron mediante la mirada simulada y las teclas indicadas por el escenario.\n");
            md.AppendLine("| Caso | Resultado | Observación |\n| --- | --- | --- |");
            foreach (var test in report.tests)
                md.AppendLine("| " + test.id + " | " + (test.passed ? "Cumple" : "Falla") + " | " + Flatten(test.observed) + " |");
            md.AppendLine("\n## Capturas\n");
            foreach (var filename in report.screenshots)
                md.AppendLine("- [" + filename + "](" + Path.Combine(output, filename).Replace('\\', '/') + ")");
            md.AppendLine("\nLa primera ejecución produjo " + report.frames.Count + " fotogramas para el video de validación.\n");
            if (report.errors.Count > 0)
            {
                md.AppendLine("## Incidencias\n");
                foreach (string error in report.errors) md.AppendLine("```text\n" + error + "\n```\n");
            }
            File.WriteAllText(Path.Combine(output, "Resultados_Etapa_02.md"), md.ToString(), Encoding.UTF8);
        }

        static EditorWindow GetGameView()
        {
            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            return type != null ? EditorWindow.GetWindow(type) : null;
        }

        static void FocusGameView()
        {
            var view = GetGameView();
            if (view == null) return;
            view.maximized = true;
            view.Focus();
        }
    }
}
#endif
