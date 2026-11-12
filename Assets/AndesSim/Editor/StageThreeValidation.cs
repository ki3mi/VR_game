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
    public static class StageThreeValidation
    {
        const string ActiveKey = "AndesSim.StageThreeValidation.Active";
        const string FolderKey = "AndesSim.StageThreeValidation.Folder";
        const string MaximizeKey = "AndesSim.StageThreeValidation.Maximized";
        const float TestDuration = 24f;
        static readonly DateTime StartTime = new DateTime(2016, 2, 4, 20, 36, 0);
        static readonly DateTime EndTime = new DateTime(2016, 2, 5, 3, 59, 0);
        static readonly (string id, string title, Key button)[] Controls =
        {
            ("mfa", "MFA", Key.Digit1), ("fuera_horario", "fuera de horario", Key.Digit2), ("siem", "SIEM", Key.Digit3)
        };
        static readonly Stack<IEnumerator> Steps = new Stack<IEnumerator>();
        static ExplorationController explorer;
        static XRInteractionSimulator simulator;
        static GameManager game;
        static UIManager ui;
        static AlarmSystem alarm;
        static ScenarioData originalData, testData;
        static Report report;
        static string output;
        static double beganAt, resumeAt;
        static bool ending, settingsOverridden, previousRunInBackground;
        static InputSettings.BackgroundBehavior previousBackgroundBehavior;
        static InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;

        [Serializable]
        sealed class Result
        {
            public string id;
            public bool passed;
            public string observed;
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
            public int firstAttackOrders;
            public int defenseOrders;
            public int secondAttackOrders;
            public int largestDefenseCountOfOrders;
            public bool alarmSound;
            public bool redLight;
            public bool redBeacon;
            public bool sessionBlocked;
            public bool protectedEvent;
            public string defenseClock;
            public int stationsVisited;
            public int objectsInspected;
            public bool visualReviewRequired;
            public List<int> incompleteAttempts = new List<int>();
            public List<Result> tests = new List<Result>();
            public List<string> screenshots = new List<string>();
            public List<string> errors = new List<string>();
        }

        static StageThreeValidation() => EditorApplication.playModeStateChanged += PlayStateChanged;

        [MenuItem("Andes-Sim/Etapa 3/Validar en simulador")]
        public static void StartValidation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "DataCenter_VR")
            {
                Debug.LogWarning("Abre DataCenter_VR en Edit Mode para ejecutar la validación de etapa 3.");
                return;
            }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            output = Path.Combine(root, "Docs", "Evidencias_Etapa_03", "Run_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(output);
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
                    testDurationSeconds = TestDuration,
                    state = "running"
                };
                beganAt = EditorApplication.timeSinceStartup;
                resumeAt = beganAt + 1.5;
                ending = false;
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
                Debug.Log("ANDES-SIM · Evidencias de etapa 3 en " + SessionState.GetString(FolderKey, ""));
            }
        }

        static void Tick()
        {
            if (ending || !EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            try
            {
                Require(now - beganAt < 200, "La prueba superó su límite de 200 segundos.");
                if (game != null && game.mode == SimMode.Defensa)
                    report.largestDefenseCountOfOrders = Mathf.Max(report.largestDefenseCountOfOrders, game.OrdersSent);
                if (now < resumeAt) return;
                if (Steps.Count == 0)
                {
                    Finish(true);
                    return;
                }
                var step = Steps.Peek();
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
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settingsOverridden = true;
            explorer = Object.FindAnyObjectByType<ExplorationController>();
            simulator = Object.FindAnyObjectByType<XRInteractionSimulator>();
            game = GameManager.I;
            ui = Object.FindAnyObjectByType<UIManager>();
            alarm = Object.FindAnyObjectByType<AlarmSystem>();
            Require(explorer != null && simulator != null && game != null && ui != null && alarm != null,
                "Faltan el explorador, simulador, GameManager, UIManager o alarma.");
            Require(InputSystem.GetDevice<XRSimulatedHMD>() != null && Keyboard.current != null && Mouse.current != null,
                "Se necesitan XRSimulatedHMD, teclado y ratón.");
            originalData = game.data;
            Require(originalData != null, "GameManager no tiene el recurso ScenarioData.");
            report.configuredDurationSeconds = originalData.durationSeconds;
            testData = Object.Instantiate(originalData);
            testData.durationSeconds = TestDuration;
            game.data = testData;
            Require(game.mode == SimMode.Ataque && IsCleanPreparation(), "La escena debe iniciar en preparación limpia del ataque.");
            Require(!game.AttackResultOrders.HasValue && !game.DefenseResultOrders.HasValue,
                "No debe existir historial al abrir la escena.");

            yield return Press(Key.H);
            yield return Select("Portal_Entrada");
            foreach (string station in new[] { "TP_Entrada", "TP_Racks", "TP_Terminal", "TP_Switch", "TP_Impresora", "TP_Controles" })
                yield return GoTo(station, true);
            yield return InspectAndClose("Panel_Controles");
            yield return GoTo("TP_Racks");
            yield return InspectAndClose("Rack_01");
            yield return GoTo("TP_Switch");
            yield return InspectAndClose("Switch_Barato");
            yield return GoTo("TP_Impresora");
            yield return InspectAndClose("Impresora");
            yield return OpenTerminal();
            report.stationsVisited = explorer.VisitedCount;
            report.objectsInspected = explorer.InspectedCount;
            Add("E3-P07", report.stationsVisited == 6 && report.objectsInspected == 5,
                "Portal, seis destinos alcanzados por teletransporte y cinco fichas abiertas por mirada.");

            yield return Press(Key.Digit1);
            Require(game.State == ScenarioState.Ejecucion && game.mode == SimMode.Ataque, "La terminal no inició el ataque.");
            yield return Select("Terminal_SWIFT");
            Require(ui.LogVisible, "No se abrió el registro del ataque.");
            yield return Press(Key.Digit1);
            Require(game.HasEvidence("log_huecos") && game.EvidenceCount == 1, "Falta evidencia previa para verificar su limpieza.");
            yield return Press(Key.Escape);
            yield return AwaitAttackResult();
            report.firstAttackOrders = game.OrdersSent;
            yield return OpenComparison();
            bool firstComparison = ComparisonMatches(@"actual", @"referencia") && game.AttackResultOrders == 36 && !game.DefenseResultOrders.HasValue;
            yield return Screenshot("01_ataque_con_defensa_de_referencia.png");
            yield return Press(Key.Digit1);
            bool firstReset = IsCleanPreparation() && game.AttackResultOrders == 36 && !game.DefenseResultOrders.HasValue;
            Require(firstReset, "El primer reinicio debe limpiar la ejecución y conservar el resultado histórico del ataque.");

            yield return OpenTerminal();
            yield return Press(Key.Digit2);
            Require(game.mode == SimMode.Defensa && game.State == ScenarioState.Preparacion && explorer.PanelVisible,
                "El selector de la terminal no dejó Defensa en preparación.");
            bool controlsUnique = true;
            for (int i = 0; i < Controls.Length; i++)
            {
                yield return Press(Key.Digit1);
                string notices = ui.StatusText + "\n" + TextOf("InspectionPanel");
                bool staysReady = game.State == ScenarioState.Preparacion && game.OrdersSent == 0 &&
                    game.DefenseCount == i && !game.SessionBlocked && !game.ProtectedEventRecorded && !alarm.IsActive;
                bool listsMissing = Regex.IsMatch(notices, "falt|pendient", RegexOptions.IgnoreCase) &&
                    Controls.Skip(i).All(control => notices.IndexOf(control.title, StringComparison.OrdinalIgnoreCase) >= 0);
                Require(staysReady && listsMissing, "El intento incompleto debe permanecer en preparación e indicar los controles pendientes. Controles activos " + i + ". " + Flatten(notices));
                report.incompleteAttempts.Add(i);
                if (i == 2) yield return Screenshot("02_control_pendiente.png");
                yield return Press(Key.Escape);
                yield return GoTo("TP_Controles");
                yield return Select("Panel_Controles");
                yield return Press(Controls[i].button);
                controlsUnique &= game.HasDefense(Controls[i].id) && game.DefenseCount == i + 1;
                yield return Press(Controls[i].button);
                controlsUnique &= game.DefenseCount == i + 1;
                if (i == Controls.Length - 1) yield return Screenshot("03_tres_controles_activos.png");
                yield return Press(Key.Escape);
                yield return OpenTerminal();
            }
            Add("E3-P01", controlsUnique && Controls.All(control => game.HasDefense(control.id)),
                "MFA, fuera de horario y SIEM activos. Cada pulsación repetida conservó el contador.");
            Add("E3-P02", report.incompleteAttempts.SequenceEqual(new[] { 0, 1, 2 }),
                "Los intentos con cero, uno y dos controles indicaron pendientes y mantuvieron el estado en preparación y las órdenes en cero.");

            yield return Press(Key.Digit1);
            report.defenseClock = game.CurrentTime.ToString("dd/MM/yyyy HH:mm");
            report.defenseOrders = game.OrdersSent;
            report.sessionBlocked = game.SessionBlocked;
            report.protectedEvent = game.ProtectedEventRecorded;
            report.alarmSound = alarm.IsActive && alarm.IsPlaying;
            Light light = alarm.redLight;
            report.redLight = light != null && light.enabled && light.intensity > 0 && light.color.r > light.color.g && light.color.r > light.color.b;
            report.redBeacon = alarm.redBeacon != null && alarm.redBeacon.enabled;
            bool blockedNotice = Regex.IsMatch(ui.StatusText, "bloquead", RegexOptions.IgnoreCase);
            Add("E3-P03", report.alarmSound && report.redLight && report.redBeacon && report.sessionBlocked && blockedNotice &&
                game.State == ScenarioState.Resultado && Minute(game.CurrentTime) == StartTime && report.defenseOrders == 0,
                "Intento a las " + report.defenseClock + ". Sonido, luz, baliza roja y mensaje de sesión bloqueada activos con cero órdenes.");
            yield return Screenshot("04_alarma_roja_y_bloqueo.png");

            yield return OpenTerminal();
            string protectedLog = TextOf("InspectionPanel");
            Add("E3-P04", report.defenseOrders == 0 && report.protectedEvent && ui.LogVisible &&
                Regex.IsMatch(protectedLog, "SIEM", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(protectedLog, "protegid", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(protectedLog, "bloquead", RegexOptions.IgnoreCase),
                "La terminal muestra el evento SIEM protegido del intento bloqueado. Órdenes enviadas en defensa: " + report.defenseOrders + ".");
            yield return Screenshot("05_evento_protegido_SIEM.png");
            yield return Press(Key.Escape);
            yield return OpenComparison();
            bool defenseComparison = ComparisonMatches(@"previ|anterior", @"actual") && game.AttackResultOrders == 36 && game.DefenseResultOrders == 0;
            yield return Screenshot("06_comparacion_defensa_actual.png");

            yield return Press(Key.Digit1);
            bool defenseReset = IsCleanPreparation() && game.AttackResultOrders == 36 && game.DefenseResultOrders == 0;
            Require(defenseReset, "El reinicio de defensa debe apagar la alarma, limpiar la ejecución y conservar ambas referencias históricas.");
            yield return Screenshot("07_reinicio_sin_alarma.png");
            yield return OpenTerminal();
            if (game.mode == SimMode.Defensa) yield return Press(Key.Digit2);
            Require(game.mode == SimMode.Ataque, "No se pudo volver al modo ataque.");
            yield return Press(Key.Digit1);
            Require(game.State == ScenarioState.Ejecucion, "La terminal no inició el segundo ataque.");
            yield return AwaitAttackResult();
            report.secondAttackOrders = game.OrdersSent;
            yield return OpenComparison();
            bool secondComparison = ComparisonMatches(@"actual", @"previ|anterior") && game.AttackResultOrders == 36 && game.DefenseResultOrders == 0;
            Add("E3-P05", firstComparison && defenseComparison && secondComparison,
                "Comparación 36 frente a 0 en ambos modos. La defensa se mostró como referencia antes de ejecutarse y luego como ejecución previa.");
            Add("E3-P06", firstReset && defenseReset && report.firstAttackOrders == 36 && report.defenseOrders == 0 &&
                report.secondAttackOrders == 36 && report.largestDefenseCountOfOrders == 0 && game.EvidenceCount == 0 && game.DefenseCount == 0,
                "Ciclo 36 → 0 → 36 en una sola ejecución de Play Mode. Reinicios con reloj, contadores, evidencias, controles y alarma limpios.");
            yield return Screenshot("08_segundo_ataque_36_ordenes.png");
            yield return Press(Key.Escape);
            yield return OpenGlobalPanel(Key.I,
                () => explorer.PanelVisible && Regex.IsMatch(TextOf("InspectionPanel"), "instrucciones", RegexOptions.IgnoreCase),
                "instrucciones");
            string instructions = TextOf("InspectionPanel");
            bool instructionsOpen = explorer.PanelVisible && Regex.IsMatch(instructions, "instrucciones", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(instructions, "cr[eé]ditos", RegexOptions.IgnoreCase);
            yield return Screenshot("09_instrucciones.png");
            yield return Press(Key.Digit1);
            string credits = TextOf("InspectionPanel");
            bool creditsOpen = explorer.PanelVisible &&
                Regex.IsMatch(credits, "cr[eé]ditos", RegexOptions.IgnoreCase) && Regex.IsMatch(credits, "Unity|Liberation", RegexOptions.IgnoreCase);
            yield return Screenshot("10_creditos.png");
            yield return Press(Key.Escape);
            Add("E3-P08", instructionsOpen && creditsOpen && !explorer.PanelVisible && report.errors.Count == 0,
                "Ambos modos completados. I abrió instrucciones, 1 abrió créditos y Esc cerró la ficha. Errores de ejecución: " + report.errors.Count + ".");
        }

        static bool IsCleanPreparation() => game.State == ScenarioState.Preparacion && game.OrdersSent == 0 &&
            game.EvidenceCount == 0 && game.DefenseCount == 0 && Minute(game.CurrentTime) == StartTime &&
            !game.SessionBlocked && !game.ProtectedEventRecorded && !alarm.IsActive && !alarm.IsPlaying &&
            (alarm.redLight == null || !alarm.redLight.enabled) && (alarm.redBeacon == null || !alarm.redBeacon.enabled) &&
            !ui.SummaryVisible;

        static bool ComparisonMatches(string attackLabel, string defenseLabel)
        {
            string attack = TextOf("ComparisonAttack");
            string defense = TextOf("ComparisonDefense");
            return ui.SummaryVisible && Regex.IsMatch(attack, @"\b36\b") && Regex.IsMatch(defense, @"\b0\b") &&
                Regex.IsMatch(attack, attackLabel, RegexOptions.IgnoreCase) && Regex.IsMatch(defense, defenseLabel, RegexOptions.IgnoreCase);
        }

        static IEnumerator AwaitAttackResult()
        {
            double deadline = EditorApplication.timeSinceStartup + TestDuration + 8;
            while (game.State == ScenarioState.Ejecucion && EditorApplication.timeSinceStartup < deadline) yield return .15d;
            Require(game.State == ScenarioState.Resultado && Minute(game.CurrentTime) == EndTime,
                "El ataque no cerró su sesión a las 03:59.");
        }

        static IEnumerator OpenComparison() => OpenGlobalPanel(Key.F, () => ui.SummaryVisible, "comparación");

        static IEnumerator OpenGlobalPanel(Key key, Func<bool> isOpen, string panelName)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (isOpen()) yield break;
                if (explorer.PanelVisible) break;
                FocusGameView();
                yield return .3d;
                yield return Press(key);
            }
            string focus = EditorWindow.focusedWindow?.titleContent.text ?? "ninguna";
            Require(isOpen(), $"La tecla {key} no abrió {panelName}. Ventana enfocada {focus}. " +
                $"Teclado habilitado {Keyboard.current?.enabled}. Ficha abierta {explorer.PanelVisible}. " + Flatten(ui.StatusText));
        }

        static IEnumerator OpenTerminal()
        {
            yield return GoTo("TP_Terminal");
            yield return Select("Terminal_SWIFT");
            Require(explorer.PanelVisible, "La terminal no abrió su ficha.");
        }

        static IEnumerator InspectAndClose(string name)
        {
            yield return Select(name);
            Require(explorer.PanelVisible, "No se abrió la ficha de " + name + ".");
            yield return Press(Key.Escape);
        }

        static IEnumerator GoTo(string name, bool force = false)
        {
            Vector3 destination = FindTarget(name).destination.position;
            if (!force && HorizontalDistance(explorer.viewCamera.transform.position, destination) < .12f) yield break;
            yield return Select(name);
            Require(HorizontalDistance(explorer.viewCamera.transform.position, destination) < .15f, "La cámara no llegó a " + name + ".");
        }

        static IEnumerator Select(string name)
        {
            var target = FindTarget(name);
            Require(!explorer.PanelVisible, "Cierra la ficha antes de seleccionar " + name + ".");
            yield return AimAt(target.AimPosition);
            Require(explorer.CurrentTarget == target && target.IsHighlighted,
                "La mirada no alcanzó " + name + ". Objetivo actual " + (explorer.CurrentTarget == null ? "ninguno" : explorer.CurrentTarget.name) + ".");
            yield return Press(Key.Space);
            Require(explorer.LastSelectedId == target.uniqueId, "Espacio no seleccionó " + name + ".");
            yield return .2d;
        }

        static IEnumerator AimAt(Vector3 point)
        {
            Transform camera = explorer.viewCamera.transform;
            Vector3 direction = point - camera.position;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
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

        static IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
            yield return .12d;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            yield return .16d;
        }

        static IEnumerator Screenshot(string filename)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(output, filename));
            report.screenshots.Add(filename);
            yield return .45d;
        }

        static ExplorationTarget FindTarget(string name)
        {
            var target = GameObject.Find(name)?.GetComponent<ExplorationTarget>();
            Require(target != null, "No se encontró ExplorationTarget en " + name + ".");
            return target;
        }

        static string TextOf(string objectName)
        {
            var panel = GameObject.Find(objectName);
            return panel == null ? "" : string.Join("\n", panel.GetComponentsInChildren<TMP_Text>()
                .Where(text => text.enabled).Select(text => text.text));
        }

        static float HorizontalDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        static DateTime Minute(DateTime time) => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
        static string Flatten(string value) => value.Replace("\r", " ").Replace("\n", " ").Replace("|", "/");
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
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
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
            var md = new StringBuilder("# Validación de etapa 3 con XR Interaction Simulator\n\n");
            md.AppendLine("Codex ejecutó la validación mediante Unity MCP el " + report.timestampUtc + ".");
            md.AppendLine("La escena `" + report.scene + "` se ejecutó en Unity " + report.unityVersion + " con entradas de teclado y ratón en el simulador.");
            md.AppendLine("El recurso configura " + report.configuredDurationSeconds + " s de ataque. La validación usó una copia temporal de " + report.testDurationSeconds + " s.\n");
            md.AppendLine("Estado `" + report.state + "`.\n");
            md.AppendLine("| Caso | Resultado | Observación |\n| --- | --- | --- |");
            foreach (var test in report.tests)
                md.AppendLine("| " + test.id + " | " + (test.passed ? "Cumple" : "Falla") + " | " + Flatten(test.observed) + " |");
            md.AppendLine("\n## Capturas\n");
            foreach (string filename in report.screenshots)
                md.AppendLine("- [" + filename + "](" + Path.Combine(output, filename).Replace('\\', '/') + ")");
            if (report.errors.Count > 0)
            {
                md.AppendLine("\n## Incidencias\n");
                foreach (string error in report.errors) md.AppendLine("```text\n" + error + "\n```\n");
            }
            File.WriteAllText(Path.Combine(output, "Resultados_Etapa_03.md"), md.ToString(), Encoding.UTF8);
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
