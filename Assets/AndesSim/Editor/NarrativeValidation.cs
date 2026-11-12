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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Object = UnityEngine.Object;

namespace AndesSim.Editor
{
    /// <summary>End-to-end player-input walkthrough. Never calls narrative progression callbacks.</summary>
    [InitializeOnLoad]
    public static class NarrativeValidation
    {
        const string ActiveKey = "AndesSim.NarrativeValidation.Active";
        const string FolderKey = "AndesSim.NarrativeValidation.Folder";
        const string MaximizeKey = "AndesSim.NarrativeValidation.Maximized";
        static readonly Stack<IEnumerator> Steps = new Stack<IEnumerator>();
        static ExplorationController explorer;
        static XRInteractionSimulator simulator;
        static GameManager game;
        static MonoBehaviour director;
        static Report report;
        static string output;
        static double beganAt, resumeAt;
        static bool ending, settingsOverridden, previousRunInBackground;
        static InputSettings.BackgroundBehavior previousBackgroundBehavior;
        static InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
        static MouseState? pendingMouse;
        static KeyboardState? pendingKeyboard;
        static bool monitorPhone;
        static readonly HashSet<string> PhoneStages = new HashSet<string>();

        [Serializable]
        sealed class Check
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
            public string simulator;
            public string status;
            public float configuredDurationSeconds;
            public float elapsedSeconds;
            public bool durationAssetUnchanged;
            public bool visualReviewRequired;
            public bool phoneVisibleThroughout;
            public bool typingObserved;
            public int mentorMessages;
            public int playerMessages;
            public int notificationSounds;
            public int chatBubbleBodiesChecked;
            public float smallestBodyHeightMargin;
            public List<Check> checks = new List<Check>();
            public List<string> stages = new List<string>();
            public List<string> screenshots = new List<string>();
            public List<string> errors = new List<string>();
        }

        static NarrativeValidation() => EditorApplication.playModeStateChanged += PlayStateChanged;

        [MenuItem("Andes-Sim/Narrativa/Validar recorrido en simulador")]
        public static void StartValidation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "DataCenter_VR")
            {
                Debug.LogWarning("Abre DataCenter_VR en Edit Mode para validar la narrativa.");
                return;
            }
            output = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Docs",
                "Evidencias_Narrativa", "Run_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
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
                    timestampUtc = DateTime.UtcNow.ToString("o"), scene = SceneManager.GetActiveScene().path,
                    unityVersion = Application.unityVersion, simulator = typeof(XRInteractionSimulator).FullName,
                    status = "running"
                };
                beganAt = EditorApplication.timeSinceStartup;
                resumeAt = beganAt + 2;
                ending = false;
                monitorPhone = false;
                PhoneStages.Clear();
                Steps.Clear();
                Steps.Push(Validate());
                Application.logMessageReceived += CaptureError;
                EditorApplication.update += Tick;
                FocusGameView();
            }
            else if (change == PlayModeStateChange.ExitingPlayMode && !ending && report != null)
            {
                report.errors.Add("Se interrumpió la validación antes de completar el recorrido.");
                Finish(false, false);
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(ActiveKey, false);
                var view = GetGameView();
                if (view != null) view.maximized = SessionState.GetBool(MaximizeKey, false);
                Debug.Log("ANDES-SIM · Evidencias narrativas: " + SessionState.GetString(FolderKey, ""));
            }
        }

        static void Tick()
        {
            if (ending || !EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            try
            {
                Require(now - beganAt < 420, "El recorrido excedió el límite de 420 segundos; etapa " + Stage);
                if (monitorPhone) AssertPhoneVisible();
                if (now < resumeAt) return;
                if (Steps.Count == 0) { Finish(true); return; }
                IEnumerator step = Steps.Peek();
                if (!step.MoveNext()) Steps.Pop();
                else if (step.Current is IEnumerator nested) Steps.Push(nested);
                else if (step.Current is double delay) resumeAt = now + delay;
            }
            catch (Exception error)
            {
                report.errors.Add(error.ToString());
                if (!string.IsNullOrEmpty(output)) ScreenCapture.CaptureScreenshot(Path.Combine(output, "fallo.png"));
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
            // EditorApplication.update reads the Editor input buffer. Queue player actions
            // immediately before the Dynamic input update so UI action callbacks receive them.
            InputSystem.onBeforeUpdate += InjectQueuedInput;
            explorer = Object.FindAnyObjectByType<ExplorationController>();
            simulator = Object.FindAnyObjectByType<XRInteractionSimulator>();
            game = GameManager.I;
            director = Object.FindObjectsByType<MonoBehaviour>()
                .FirstOrDefault(item => item != null && item.GetType().Name == "TrainingDirector");
            Require(explorer != null && simulator != null && game != null && director != null,
                "Falta el director narrativo, el simulador, el explorador o GameManager.");
            Require(Keyboard.current != null && Mouse.current != null && InputSystem.GetDevice<XRSimulatedHMD>() != null,
                "Se necesita teclado, ratón y cabeza XR simulada.");
            report.configuredDurationSeconds = game.data.durationSeconds;
            monitorPhone = true;
            AssertPhoneVisible();
            Require(ReadMentorString("MentorDisplayName") == "Elliot Alderson", "El chat debe presentar a Elliot Alderson sin prefijo.");
            yield return WaitMentorReady();
            Require(Stage == "Welcome", "El entrenamiento debe comenzar con la bienvenida; etapa " + Stage);
            Add("NAR-01", true, "Escena abre con la bienvenida del senior y simulador XR activo.");
            yield return Screenshot("01_bienvenida.png");

            string welcome = Stage;
            int receivedBefore = ReadMentorInt("MentorMessageCount");
            int soundBefore = ReadMentorInt("NotificationSoundCount");
            yield return ClickNamed("SeniorHelp", "Guíame");
            Require(ReadMentorBool("IsMentorTyping") && ReadMentorInt("MentorMessageCount") == receivedBefore,
                "El chat debe mostrar Escribiendo antes de entregar el mensaje de ayuda.");
            report.typingObserved = true;
            yield return Screenshot("13_elliot_escribiendo.png", false);
            yield return WaitMentorReady();
            var notificationAudio = ReadMentor("ReceivedMessageAudioSource") as AudioSource;
            Require(ReadMentorInt("NotificationSoundCount") > soundBefore && notificationAudio != null &&
                    ReadMentor("ReceivedMessageAudioClip") is AudioClip && notificationAudio.volume > 0,
                "Entregar un mensaje debe reproducir la notificación con su clip de audio.");
            Add("CHAT-02", true, "Escribiendo aparece antes de entregar el mensaje; la recepción incrementa el contador de notificación con clip y volumen activos.");
            Require(ReadMentorString("LastDeliveredMessage").Contains("GUÍA DE LA ACCIÓN"), "La ayuda visible no respondió al clic.");
            yield return ClickNamed("SeniorRepeat", "Repetir");
            yield return WaitMentorReady();
            Require(Stage == welcome && !ReadMentorString("LastDeliveredMessage").Contains("GUÍA DE LA ACCIÓN"), "Repetir no restauró la explicación del objetivo.");
            Add("NAR-02", true, "Guíame muestra la explicación de la acción; Repetir restaura el mensaje y mantiene el objetivo.");
            int playerBefore = ReadMentorInt("PlayerMessageCount");
            yield return ClickPrimary();
            yield return AwaitStage("Orientation");
            Require(ReadMentorInt("PlayerMessageCount") > playerBefore && !string.IsNullOrWhiteSpace(ReadMentorString("LastPlayerMessage")),
                "Comenzar el entrenamiento debe registrar la acción del jugador como mensaje del chat.");
            Add("CHAT-03", true, "Las acciones del jugador producen mensajes propios y respuestas del mentor en el historial.");
            yield return Press(Key.H);
            yield return GoTo("TP_Terminal");
            yield return Select("Terminal_SWIFT");
            yield return ValidateMessageSequence();
            yield return AwaitStage("AccessQuery");

            yield return Query("andes access", false);
            Require(IsCoral(ReadMentorColor("OutputTextColor")), "La salida de error debe usar coral para distinguirse del comando.");
            Require(Stage == "AccessQuery" && !ReadBool("LastQuerySucceeded"), "Una consulta incompleta avanzó el recorrido.");
            Add("NAR-03", true, "Una consulta incompleta muestra corrección y permite reintentar el objetivo.");
            yield return Query("andes access --from 20:00 --to 21:00", true);
            Require(IsCyan(ReadMentorColor("CommandTextColor")) && IsGreen(ReadMentorColor("OutputTextColor")),
                "El comando debe verse en cian y la salida correcta en verde.");
            AssertTerminalPlacement();
            Add("CHAT-06", true, "Terminal a la izquierda con teléfono visible a la derecha: comando cian, resultado verde y error coral.");
            AssertInstructionsInPhone();
            Add("CHAT-07", true, "Las instrucciones y explicaciones están en el chat; las tarjetas antiguas de objetivo o explicación no aparecen fuera del teléfono.");
            Require(ReadString("LastQueryOutput").Contains("20:36"), "La consulta de acceso no contiene la sesión de las 20:36.");
            yield return AwaitStage("AccessReview");
            yield return Screenshot("02_consulta_accesos.png");
            Require(game.SimulationPaused && game.OrdersSent == 0 && At(2016, 2, 4, 20, 36),
                "La explicación inicial debe conservar 20:36, cero órdenes y reloj detenido.");
            Add("NAR-04", true, "Consulta e interpretación de cuenta, origen y hora de acceso antes del envío.");
            yield return ClickPrimary();
            yield return AwaitStage("PaymentQuery", 18);
            Require(game.SimulationPaused && game.OrdersSent == 36 && At(2016, 2, 4, 22, 6),
                "La transmisión debe detenerse en las 22:06 con 36 órdenes.");
            yield return Query("andes payments --status sent", true);
            Require(ReadString("LastQueryOutput").Contains("36"), "La consulta de pagos no muestra 36 órdenes.");
            yield return AwaitStage("PaymentReview");
            yield return Screenshot("03_ordenes_enviadas.png");
            yield return ClickPrimary();
            yield return AwaitStage("SessionEnd", 15);
            Require(game.State == ScenarioState.Resultado && game.OrdersSent == 36 && At(2016, 2, 5, 3, 59),
                "La reconstrucción debe terminar a las 03:59 del día siguiente con 36 órdenes.");
            Add("NAR-05", true, "Hitos exactos 20:36 → 22:06/36 órdenes → 05/02 03:59, con pausas para leer.");
            yield return ClickPrimary();
            yield return AwaitStage("Investigate");

            foreach (var evidence in new[] { ("TP_Impresora", "Impresora", "impresora_apagada"),
                         ("TP_Switch", "Switch_Barato", "switch_barato"), ("TP_Terminal", "Terminal_SWIFT", "log_huecos") })
            {
                yield return CloseReading();
                yield return GoTo(evidence.Item1);
                yield return Select(evidence.Item2);
                yield return ClickPrimary();
                Require(Stage == "PrepareDefense" || game.HasEvidence(evidence.Item3), "No se conservó evidencia " + evidence.Item3);
            }
            Require(ReadInt("TrainingEvidenceCount") == 3 && game.mode == SimMode.Defensa && game.EvidenceCount == 0,
                "El recorrido debe conservar tres evidencias únicas antes de preparar las defensas.");
            Add("NAR-06", true, "Tres evidencias registradas en un orden distinto al guion: impresora, switch y registro.");
            yield return Screenshot("04_evidencias_completas.png");
            if (Stage == "Investigate") yield return ClickPrimary();
            yield return AwaitStage("PrepareDefense");
            yield return CloseReading();
            yield return GoTo("TP_Controles");
            yield return Select("Panel_Controles");
            yield return AwaitStage("Mfa");
            foreach (var control in new[] { ("Mfa", "mfa"), ("Schedule", "fuera_horario"), ("Siem", "siem") })
            {
                yield return AwaitStage(control.Item1);
                yield return ClickPrimary();
                Require(game.HasDefense(control.Item2), "No se aplicó control " + control.Item2);
            }
            Require(game.DefenseCount == 3 && game.DefensesReady && game.OrdersSent == 0,
                "Los tres controles deben estar activos antes de probar el acceso.");
            Add("NAR-07", true, "El jugador aplica MFA, política de horario y SIEM desde la interfaz del panel.");
            yield return Screenshot("05_defensas_preparadas.png");
            yield return CloseReading();
            yield return GoTo("TP_Terminal");
            yield return Select("Terminal_SWIFT");
            yield return AwaitStage("DefenseReady");
            yield return ClickPrimary();
            yield return AwaitStage("DefenseResult");
            var alarm = Object.FindAnyObjectByType<AlarmSystem>();
            Require(game.mode == SimMode.Defensa && game.State == ScenarioState.Resultado && game.OrdersSent == 0 &&
                    game.SessionBlocked && game.ProtectedEventRecorded && At(2016, 2, 4, 20, 36) && alarm != null && alarm.IsActive,
                "El acceso protegido debe bloquearse a las 20:36 con alarma, evento protegido y cero órdenes.");
            Add("NAR-08", true, "Bloqueo real antes de envíos: cero órdenes, alarma activa y evento protegido.");
            yield return Screenshot("06_intento_bloqueado.png");
            yield return ClickPrimary();
            yield return AwaitStage("ProtectedQuery");
            yield return Query("andes events --result blocked", true);
            Require(ReadString("LastQueryOutput").IndexOf("block", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ReadString("LastQueryOutput").IndexOf("bloque", StringComparison.OrdinalIgnoreCase) >= 0,
                "La consulta del evento protegido no explica el bloqueo.");
            yield return AwaitStage("ProtectedReview");
            Add("NAR-09", ReadInt("QueryCount") == 3, "Tres consultas completadas: acceso, pagos y evento centralizado protegido.");
            yield return ClickPrimary();
            yield return AwaitStage("Comparison");
            Require(game.AttackResultOrders == 36 && game.DefenseResultOrders == 0,
                "La comparación debe utilizar las ejecuciones realizadas: 36 frente a 0.");
            Add("NAR-10", true, "Comparación final basada en ataque de 36 órdenes y defensa de cero órdenes realizados en esta sesión.");
            yield return Screenshot("07_comparacion_36_0.png");
            yield return ClickPrimary();
            yield return AwaitStage("Debrief");
            yield return Screenshot("08_conclusiones.png");
            yield return ClickPrimary();
            yield return AwaitStage("Completed");
            Add("NAR-11", true, "Recorrido terminado después de explicaciones, investigación y práctica técnica, con cierre accesible.");
            yield return Screenshot("09_entrenamiento_completado.png");
            foreach (string required in new[] { "Welcome", "Orientation", "AccessQuery", "Comparison", "Completed" })
                Require(PhoneStages.Contains(required), "No se observó el teléfono visible durante " + required);
            report.phoneVisibleThroughout = true;
            Add("CHAT-01", true, "Elliot Alderson permanece visible en su teléfono a la derecha durante bienvenida, exploración, consola, comparación y cierre.");
            yield return ValidateChatHistory();

            // Repeat only the protected run through its visible action, preserving the investigation.
            yield return ClickNamed("SecondaryAction", "Repetir prueba protegida");
            yield return AwaitStage("PrepareDefense");
            Require(ReadInt("TrainingEvidenceCount") == 3 && ReadInt("QueryCount") == 3 &&
                    game.AttackResultOrders == 36 && game.DefenseResultOrders == 0 && game.DefenseCount == 0 && !alarm.IsActive,
                "Repetir la defensa debe conservar expediente, consultas e historial y limpiar sus controles y alarma.");
            yield return GoTo("TP_Controles");
            yield return Select("Panel_Controles");
            foreach (var control in new[] { ("Mfa", "mfa"), ("Schedule", "fuera_horario"), ("Siem", "siem") })
            {
                yield return AwaitStage(control.Item1);
                yield return ClickPrimary();
                Require(game.HasDefense(control.Item2), "No se volvió a aplicar " + control.Item2);
            }
            yield return GoTo("TP_Terminal");
            yield return Select("Terminal_SWIFT");
            yield return ClickPrimary();
            yield return AwaitStage("DefenseResult");
            Require(game.OrdersSent == 0 && game.SessionBlocked && alarm.IsActive, "La defensa repetida debe bloquear otra vez con cero órdenes.");
            yield return ClickPrimary();
            yield return AwaitStage("ProtectedQuery");
            yield return Query("andes events --result blocked", true);
            Require(ReadInt("QueryCount") == 3, "Repetir una consulta no debe inflar el contador de las tres prácticas únicas.");
            yield return ClickPrimary();
            yield return AwaitStage("Comparison");
            Require(game.AttackResultOrders == 36 && game.DefenseResultOrders == 0, "Se perdió la comparación al repetir la defensa.");
            yield return Screenshot("10_defensa_repetida.png");
            yield return ClickPrimary();
            yield return ClickPrimary();
            yield return AwaitStage("Completed");
            Add("REC-01", true, "Repetir la prueba protegida conserva expediente 3/3, consultas únicas 3/3 y comparación 36 frente a 0.");

            yield return ClickNamed("RestartTraining", "Reiniciar");
            yield return ClickNamed("ConfirmRestart", "Sí, reiniciar", "Reiniciar entrenamiento", "Confirmar");
            yield return AwaitStage("Welcome");
            Require(game.State == ScenarioState.Preparacion && game.OrdersSent == 0 && game.EvidenceCount == 0 &&
                    game.DefenseCount == 0 && !game.AttackResultOrders.HasValue && !game.DefenseResultOrders.HasValue &&
                    !game.SessionBlocked && !game.ProtectedEventRecorded && !alarm.IsActive && ReadInt("QueryCount") == 0,
                "Reiniciar debe limpiar la ejecución, el historial, las consultas, la narrativa y la alarma.");
            Add("NAR-12", true, "Reinicio completo devuelve a bienvenida sin evidencias, controles, alarma, consultas ni historial previos.");

            yield return ClickPrimary();
            yield return AwaitStage("Orientation");
            yield return Press(Key.H);
            yield return GoTo("TP_Terminal");
            yield return Select("Terminal_SWIFT");
            yield return AwaitStage("AccessQuery");
            yield return ClickNamed("SeniorHelp", "Guíame");
            yield return WaitMentorReady();
            Require(ReadMentorString("LastDeliveredMessage").Contains("GUÍA DE LA ACCIÓN"),
                "La guía del senior debe aparecer en el chat mientras se usa la consola.");
            Add("REC-02", true, "La explicación completa del senior aparece en el chat del teléfono mientras la consola permanece abierta.");
            yield return Screenshot("11_guia_dentro_de_consola.png");
            InputField focusedInput = Object.FindObjectsByType<InputField>().First(x => x.isActiveAndEnabled);
            yield return FocusInput(focusedInput);
            Vector3 beforeTyping = explorer.viewCamera.transform.position;
            yield return HoldKey(Key.W, .35d);
            Require(Vector3.Distance(beforeTyping, explorer.viewCamera.transform.position) < .015f,
                "La tecla W de una consulta no debe desplazar al jugador mientras escribe.");
            Add("REC-03", true, "El campo recibe foco y teclear W no mueve la cámara ni activa locomoción del simulador.");
            yield return ClickNamed("CloseTerminal", "Volver a la sala");
            Require(!TerminalOpen && Stage == "AccessQuery", "Cerrar la consulta pendiente debe conservar el objetivo.");
            yield return ClickPrimary();
            Require(TerminalOpen && Stage == "AccessQuery", "La acción Reabrir consulta debe recuperar la consola pendiente.");
            yield return Query("andes access --from 20:00 --to 21:00", true);
            yield return AwaitStage("AccessReview");
            yield return ClickNamed("CloseTerminal", "Volver a la sala");
            Require(!TerminalOpen && Stage == "AccessReview" && ReadInt("QueryCount") == 1,
                "Cerrar después de una consulta válida debe conservar su interpretación pendiente.");
            yield return ClickPrimary();
            yield return AwaitStage("Transmission");
            Add("REC-04", true, "Cerrar antes de ejecutar conserva la tarea y ofrece Reabrir consulta; cerrar tras ejecutar conserva su interpretación y permite seguir.");
            yield return ClickNamed("RestartTraining", "Reiniciar");
            yield return ClickNamed("ConfirmRestart", "Sí, comenzar de nuevo");
            yield return AwaitStage("Welcome");
            yield return 9d;
            Require(Stage == "Welcome" && game.OrdersSent == 0 && game.State == ScenarioState.Preparacion &&
                    ReadInt("QueryCount") == 0 && !game.AttackResultOrders.HasValue && !alarm.IsActive,
                "Una transmisión anterior siguió ejecutándose después de reiniciar.");
            Add("REC-05", true, "Reiniciar durante la transmisión cancela su corrutina; tras nueve segundos sigue la bienvenida limpia.");
            yield return ClickPrimary();
            yield return AwaitStage("Orientation");
            yield return ClickPrimary();
            Require(Stage == "Orientation" && !TerminalOpen,
                "Llévame debe acercar al equipo y conservar la selección como acción del jugador.");
            yield return Select("Terminal_SWIFT");
            yield return AwaitStage("AccessQuery");
            Add("REC-06", true, "Llévame lleva a la estación; el jugador selecciona la terminal con la mirada para abrir la práctica.");
            focusedInput = Object.FindObjectsByType<InputField>().First(x => x.isActiveAndEnabled);
            yield return FocusInput(focusedInput);
            yield return ClickNamed("RestartTraining", "Reiniciar");
            yield return ClickNamed("ConfirmRestart", "Sí, comenzar de nuevo");
            yield return AwaitStage("Welcome");
            Require(!TerminalOpen && !ReadMentorBool("IsTyping") && game.OrdersSent == 0 && !alarm.IsActive,
                "El reinicio desde consola debe cerrar el campo, soltar el foco y apagar la alarma.");
            yield return ClickPrimary();
            yield return AwaitStage("Orientation");
            yield return Press(Key.H);
            Vector3 beforeMovement = explorer.viewCamera.transform.position;
            yield return HoldKey(Key.W, .35d);
            Require(Vector3.Distance(beforeMovement, explorer.viewCamera.transform.position) > .025f,
                "WASD no recuperó la locomoción después de reiniciar desde el campo de consulta.");
            Add("REC-07", true, "Reiniciar con la consola enfocada libera el campo y restaura movimiento WASD real al volver a explorar.");
            yield return Screenshot("12_movimiento_restaurado.png");
            yield return GoTo("TP_Terminal");
            yield return Select("Terminal_SWIFT");
            yield return WaitWritingOnly();
            Require(ReadMentorBool("AwaitingNextMessage"), "La prueba de reinicio requiere una indicación pendiente de Siguiente.");
            yield return ClickButtonWithoutWaiting("RestartTraining");
            yield return ClickButtonWithoutWaiting("ConfirmRestart");
            yield return AwaitStage("Welcome");
            yield return 1d;
            Require(Stage == "Welcome" && ReadMentorBool("IsReadyForAction") && !ReadMentorBool("AwaitingNextMessage") &&
                    ReadMentorString("LastDeliveredMessage").Contains("Soy Elliot Alderson") && game.OrdersSent == 0,
                "Reiniciar debe cancelar los mensajes de la secuencia anterior y sus botones pendientes.");
            Add("SEQ-04", true, "Reiniciar mientras Siguiente espera confirmación cancela la secuencia anterior y entrega solo la nueva bienvenida.");
            AssertChatBubbleGeometry();
            Add("CHAT-08", true, "Todos los cuerpos de burbujas del mentor, jugador y sistema tienen altura suficiente para Text.preferredHeight y caben dentro de sus fondos y filas.");
            report.durationAssetUnchanged = game.data.durationSeconds == report.configuredDurationSeconds;
            Require(report.durationAssetUnchanged, "La validación no debe modificar la duración configurada del recurso.");
        }

        static IEnumerator Query(string value, bool expectedSuccess)
        {
            yield return WaitMentorReady();
            if (!TerminalOpen)
            {
                yield return CloseReading();
                yield return Select("Terminal_SWIFT");
            }
            var input = Object.FindObjectsByType<InputField>().FirstOrDefault(x => x.isActiveAndEnabled);
            Require(input != null, "La consola no ofrece un campo de consulta editable; etapa " + Stage);
            yield return FocusInput(input);
            yield return PressChord(Key.LeftCtrl, Key.A);
            foreach (char character in value) InputSystem.QueueTextEvent(Keyboard.current, character);
            yield return .25d;
            // UGUI's legacy InputField consumes Event.PopEvent rather than Keyboard.onTextInput.
            // Deliver text through the focused Game View's native GUI event path when needed.
            // The field is never assigned directly and commands still run only through the visible button.
            if (input.text != value)
            {
                var view = GetGameView();
                Require(view != null && input.isFocused, "El campo de consola no recibió foco real para escribir.");
                view.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.A, modifiers = EventModifiers.Control });
                view.SendEvent(new Event { type = EventType.KeyUp, keyCode = KeyCode.A, modifiers = EventModifiers.Control });
                yield return .1d;
                foreach (char character in value)
                {
                    view.SendEvent(new Event { type = EventType.KeyDown, character = character });
                    view.SendEvent(new Event { type = EventType.KeyUp, character = character });
                }
                yield return .2d;
            }
            Require(input.text == value, "El campo debe contener exactamente la consulta escrita antes de ejecutar: esperado «" + value + "», observado «" + input.text + "».");
            yield return ClickNamed("ExecuteCommand", "Ejecutar consulta", "Ejecutar");
            yield return WaitMentorReady();
            Require(ReadBool("LastQuerySucceeded") == expectedSuccess, "Resultado inesperado para consulta: " + value + "\n" + ReadString("LastQueryOutput"));
        }

        static IEnumerator CloseReading()
        {
            if (TerminalOpen) yield return ClickNamed("CloseTerminal", "Cerrar consola", "Cerrar");
            if (explorer.PanelVisible) yield return Press(Key.Escape);
            yield return .2d;
        }

        static IEnumerator AwaitStage(string expected, double timeout = 4)
        {
            double deadline = EditorApplication.timeSinceStartup + timeout;
            while (Stage != expected && EditorApplication.timeSinceStartup < deadline) yield return .1d;
            Require(Stage == expected, "Etapa esperada " + expected + ", actual " + Stage);
            if (!report.stages.Contains(expected)) report.stages.Add(expected);
            yield return WaitMentorReady();
        }

        static IEnumerator ClickPrimary() => ClickNamed(TerminalOpen ? "TerminalContinue" : "PrimaryAction", "Continuar", "Comenzar", "Guardar", "Aplicar", "Interpretar");

        static IEnumerator ClickNamed(string objectName, params string[] labels)
        {
            yield return WaitMentorReady();
            var buttons = Object.FindObjectsByType<Button>()
                .Where(x => x.isActiveAndEnabled && x.interactable).ToArray();
            var button = buttons.FirstOrDefault(x => x.name == objectName) ?? buttons.FirstOrDefault(x => labels.Any(label =>
                ButtonText(x).IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0));
            Require(button != null, "No se encontró botón " + objectName + " en " + Stage + ". Activos: " + string.Join(" / ", buttons.Select(x => x.name + ": " + ButtonText(x))));
            yield return ClickRect(button.GetComponent<RectTransform>());
        }

        static string ButtonText(Button button) => string.Join(" ", button.GetComponentsInChildren<Text>().Select(x => x.text)) +
                                                   string.Join(" ", button.GetComponentsInChildren<TMP_Text>().Select(x => x.text));

        static IEnumerator ClickRect(RectTransform rect)
        {
            FocusGameView();
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            QueueMouse(new MouseState { position = point });
            yield return .1d;
            QueueMouse(new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return .1d;
            QueueMouse(new MouseState { position = point });
            yield return .25d;
        }

        static IEnumerator FocusInput(InputField input)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                yield return ClickRect(input.GetComponent<RectTransform>());
                yield return .18d;
                if (input.isFocused) yield break;
            }
            Require(input.isFocused, "El campo no obtuvo foco después de tres clics reales en Game View.");
        }

        static IEnumerator GoTo(string name)
        {
            var target = FindTarget(name);
            if (Vector2.Distance(new Vector2(explorer.viewCamera.transform.position.x, explorer.viewCamera.transform.position.z),
                new Vector2(target.destination.position.x, target.destination.position.z)) < .15f) yield break;
            yield return Select(name);
        }

        static IEnumerator Select(string name)
        {
            yield return WaitMentorReady();
            var target = FindTarget(name);
            Require(!explorer.PanelVisible && !TerminalOpen, "Cierra el contenido antes de seleccionar " + name);
            yield return AimAt(target.AimPosition);
            Require(explorer.CurrentTarget == target, "La mirada no alcanzó " + name + ", actual " + explorer.CurrentTarget?.name);
            yield return Press(Key.Space);
            Require(explorer.LastSelectedId == target.uniqueId, "Espacio no seleccionó " + name);
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
                QueueMouse(mouse.WithButton(MouseButton.Right));
                yield return .08d;
                QueueMouse(new MouseState { position = mouse.position });
                yield return .08d;
            }
            QueueMouse(new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f) });
            Physics.SyncTransforms();
            yield return .2d;
        }

        static IEnumerator Press(Key key)
        {
            QueueKeyboard(new KeyboardState(key));
            yield return .1d;
            QueueKeyboard(new KeyboardState());
            yield return .16d;
        }

        static IEnumerator PressChord(Key first, Key second)
        {
            QueueKeyboard(new KeyboardState(first, second));
            yield return .1d;
            QueueKeyboard(new KeyboardState());
            yield return .1d;
        }

        static IEnumerator HoldKey(Key key, double seconds)
        {
            QueueKeyboard(new KeyboardState(key));
            yield return seconds;
            QueueKeyboard(new KeyboardState());
            yield return .2d;
        }

        static IEnumerator WaitMentorReady(double timeout = 18)
        {
            double deadline = EditorApplication.timeSinceStartup + timeout;
            while ((!ReadMentorBool("IsReadyForAction") || ReadMentorBool("IsMentorTyping")) &&
                   EditorApplication.timeSinceStartup < deadline)
            {
                if (ReadMentorBool("AwaitingNextMessage") && !ReadMentorBool("IsMentorTyping"))
                    yield return ClickNextMessage();
                else yield return .05d;
            }
            Require(ReadMentorBool("IsReadyForAction") && !ReadMentorBool("IsMentorTyping"),
                "El chat no terminó de entregar sus mensajes para continuar en " + Stage + ". Pendientes: " + ReadMentorInt("PendingMessageCount"));
        }

        static IEnumerator WaitWritingOnly()
        {
            double deadline = EditorApplication.timeSinceStartup + 6;
            while (ReadMentorBool("IsMentorTyping") && EditorApplication.timeSinceStartup < deadline) yield return .05d;
            Require(!ReadMentorBool("IsMentorTyping"), "La escritura del mensaje no terminó.");
            yield return .1d;
        }

        static IEnumerator ClickButtonWithoutWaiting(string name)
        {
            var button = Object.FindObjectsByType<Button>().FirstOrDefault(x => x.name == name && x.isActiveAndEnabled && x.interactable);
            Require(button != null, "No está disponible el botón " + name + " en " + Stage);
            yield return ClickRect(button.GetComponent<RectTransform>());
        }

        static IEnumerator ClickNextMessage()
        {
            string name = TerminalOpen ? "TerminalContinue" : "PrimaryAction";
            var button = Object.FindObjectsByType<Button>().FirstOrDefault(x => x.name == name && x.isActiveAndEnabled && x.interactable);
            Require(button != null && ButtonText(button).Trim() == "Siguiente", "La secuencia pendiente debe ofrecer el botón verde Siguiente.");
            yield return ClickRect(button.GetComponent<RectTransform>());
        }

        static IEnumerator ValidateMessageSequence()
        {
            yield return WaitWritingOnly();
            Require(Stage == "AccessQuery" && ReadMentorBool("AwaitingNextMessage") && !ReadMentorBool("IsReadyForAction"),
                "Las dos indicaciones de acceso deben detenerse después de mostrar solo la primera.");
            int received = ReadMentorInt("ReceivedMessageCount");
            int acknowledgements = ReadMentorInt("NextAcknowledgementCount");
            string firstMessage = ReadMentorString("LastDeliveredMessage");
            yield return Screenshot("15_primera_indicacion_siguiente.png", false);
            yield return 1d;
            Require(received == ReadMentorInt("ReceivedMessageCount") && firstMessage == ReadMentorString("LastDeliveredMessage"),
                "La segunda indicación no debe llegar automáticamente mientras se lee la primera.");
            Add("SEQ-01", true, "Se entrega una sola indicación y el resto espera Siguiente sin cambiar de objetivo ni ejecutar acciones.");
            yield return ClickNextMessage();
            Require(ReadMentorString("LastPlayerMessage") == "Ok" && ReadMentorInt("NextAcknowledgementCount") == acknowledgements + 1,
                "Siguiente debe enviar una única respuesta Ok del jugador.");
            yield return WaitWritingOnly();
            Require(ReadMentorInt("ReceivedMessageCount") == received + 1 && ReadMentorBool("IsReadyForAction") &&
                    Stage == "AccessQuery" && ReadInt("QueryCount") == 0 && ReadMentorString("PrimaryActionLabel") != "Siguiente",
                "Ok debe entregar solo la siguiente indicación y restaurar la acción original sin ejecutar la consulta.");
            Add("SEQ-02", true, "Siguiente envía Ok, entrega una sola respuesta con escritura y devuelve las acciones del objetivo.");
            yield return Screenshot("16_segunda_indicacion_y_ok.png");
            var phone = ReadMentor("PhoneRoot") as RectTransform;
            Require(phone != null && !phone.GetComponentsInChildren<Text>(true).Any(x =>
                x.text.IndexOf("Ing.", StringComparison.OrdinalIgnoreCase) >= 0), "El nombre del senior no debe llevar Ing. en la interfaz.");
            Add("SEQ-03", true, "La cabecera, las burbujas y la presentación muestran Elliot Alderson sin prefijo Ing.");
        }

        static IEnumerator ValidateChatHistory()
        {
            var chat = ReadMentor("ChatScroll") as ScrollRect;
            Require(chat != null && chat.content != null && chat.viewport != null &&
                    chat.content.rect.height > chat.viewport.rect.height + 50,
                "La conversación completa debe producir un historial desplazable.");
            int countBefore = ReadMentorInt("ChatMessageCount");
            Require(countBefore > 12 && ReadMentorString("ChatHistoryText").Length > 600,
                "El chat debe conservar los mensajes previos del recorrido.");
            yield return ClickNamed("LatestMessages", "Ver últimos");
            yield return .15d;
            Require(ReadMentorBool("IsFollowingLatest") && LatestBubbleVisible(chat),
                "La prueba de historial debe comenzar siguiendo la última burbuja visible.");
            float beforeScroll = chat.verticalNormalizedPosition;
            Canvas canvas = chat.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, chat.viewport.TransformPoint(chat.viewport.rect.center));
            QueueMouse(new MouseState { position = point });
            yield return .1d;
            QueueMouse(new MouseState { position = point, scroll = new Vector2(0, 1600) });
            yield return .15d;
            QueueMouse(new MouseState { position = point });
            yield return .35d;
            Require(!ReadMentorBool("IsFollowingLatest") && chat.verticalNormalizedPosition > beforeScroll + .05f,
                "La rueda no permitió revisar los mensajes anteriores.");
            yield return ClickNamed("SeniorHelp", "Guíame");
            yield return WaitMentorReady();
            Require(ReadMentorInt("ChatMessageCount") > countBefore && ReadMentorBool("IsFollowingLatest") && LatestBubbleVisible(chat),
                "Cada mensaje nuevo debe enfocar la última burbuja, incluso después de revisar mensajes anteriores.");
            Add("CHAT-04", true, "El historial sigue disponible y cada respuesta nueva desplaza automáticamente el chat hasta el último mensaje.");
            yield return Screenshot("14_historial_y_mensaje_nuevo.png");
            QueueMouse(new MouseState { position = point, scroll = new Vector2(0, 1600) });
            yield return .15d;
            QueueMouse(new MouseState { position = point });
            yield return .2d;
            yield return ClickNamed("LatestMessages", "Ver últimos");
            yield return .25d;
            Require(ReadMentorBool("IsFollowingLatest") && LatestBubbleVisible(chat),
                "Ver últimos debe reactivar el seguimiento y hacer visible la última burbuja.");
            Add("CHAT-05", true, "Ver últimos reactiva el seguimiento y muestra la última burbuja; los mensajes largos comienzan por su primera línea.");
        }

        static bool LatestBubbleVisible(ScrollRect chat)
        {
            if (chat.content.childCount == 0) return false;
            var last = chat.content.GetChild(chat.content.childCount - 1) as RectTransform;
            if (last == null) return false;
            Rect bubble = ScreenRect(last), viewport = ScreenRect(chat.viewport);
            return bubble.height <= viewport.height
                ? bubble.yMin >= viewport.yMin - 5 && bubble.yMax <= viewport.yMax + 5
                : Mathf.Abs(bubble.yMax - viewport.yMax) <= 5;
        }

        static void AssertChatBubbleGeometry()
        {
            Canvas.ForceUpdateCanvases();
            var phone = ReadMentor("PhoneRoot") as RectTransform;
            Require(phone != null, "Falta el teléfono para revisar la geometría del historial.");
            Text[] bodies = phone.GetComponentsInChildren<Text>(true).Where(text => text.name == "ChatMessageBody").ToArray();
            Require(bodies.Length == ReadMentorInt("ChatMessageCount") && bodies.Length > 20,
                "Cada mensaje del historial debe tener un cuerpo de texto medible.");
            float smallestMargin = float.PositiveInfinity;
            foreach (Text body in bodies)
            {
                RectTransform rect = body.rectTransform;
                float margin = rect.rect.height - body.preferredHeight;
                smallestMargin = Mathf.Min(smallestMargin, margin);
                Require(margin >= -1,
                    "Texto recortado en " + body.transform.parent.parent.name + ": alto " + rect.rect.height +
                    ", preferido " + body.preferredHeight + ". " + body.text);
                var bubble = body.transform.parent as RectTransform;
                var row = bubble != null ? bubble.parent as RectTransform : null;
                Require(bubble != null && row != null && row.rect.height >= bubble.rect.height - 1,
                    "La fila del mensaje debe contener toda su burbuja.");
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                float localBottom = bubble.InverseTransformPoint(corners[0]).y;
                float localTop = bubble.InverseTransformPoint(corners[1]).y;
                Require(localBottom >= bubble.rect.yMin - 1 && localTop <= bubble.rect.yMax + 1,
                    "El fondo debe contener todo el cuerpo del mensaje " + row.name + ".");
            }
            report.chatBubbleBodiesChecked = bodies.Length;
            report.smallestBodyHeightMargin = smallestMargin;
        }

        static void AssertPhoneVisible()
        {
            var phone = ReadMentor("PhoneRoot") as RectTransform;
            Require(phone != null && phone.gameObject.activeInHierarchy, "El teléfono debe permanecer activo durante " + Stage);
            Rect screen = ScreenRect(phone);
            Require(screen.center.x > Screen.width * .6f && screen.xMax <= Screen.width + 2 && screen.yMin >= -2 && screen.yMax <= Screen.height + 2,
                "El teléfono debe permanecer completo a la derecha de la pantalla durante " + Stage + ": " + screen);
            PhoneStages.Add(Stage);
        }

        static void AssertTerminalPlacement()
        {
            Require(TerminalOpen, "La comprobación de terminal requiere la consola abierta.");
            var terminal = GameObject.Find("GuidedTerminal")?.GetComponent<RectTransform>();
            var phone = ReadMentor("PhoneRoot") as RectTransform;
            Require(terminal != null && phone != null, "Faltan las superficies de terminal y teléfono.");
            Rect terminalScreen = ScreenRect(terminal);
            Rect phoneScreen = ScreenRect(phone);
            Require(terminalScreen.center.x < Screen.width * .55f && terminalScreen.xMax <= phoneScreen.xMin + 2,
                "La terminal debe ocupar la izquierda sin cubrir el teléfono: " + terminalScreen + " / " + phoneScreen);
        }

        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static void AssertInstructionsInPhone()
        {
            var phone = ReadMentor("PhoneRoot") as RectTransform;
            Require(phone != null, "Falta el teléfono para comprobar la ubicación de las instrucciones.");
            foreach (string name in new[] { "CurrentObjective", "SeniorExplanation", "TerminalSeniorExplanation" })
            {
                var oldCard = GameObject.Find(name);
                Require(oldCard == null || oldCard.transform == phone || oldCard.transform.IsChildOf(phone),
                    "La tarjeta " + name + " debe integrarse en el teléfono.");
            }
            string message = ReadMentorString("CurrentMessage");
            string delivered = ReadMentorString("LastDeliveredMessage");
            Require(delivered.Length > 0 && ReadMentorString("ChatHistoryText").Contains(delivered),
                "La explicación entregada debe conservarse en el historial del teléfono.");
            foreach (var text in Object.FindObjectsByType<Text>())
                if (text.isActiveAndEnabled && text.text == message && message.Length > 50)
                    Require(text.transform.IsChildOf(phone), "La explicación completa debe presentarse dentro del chat.");
        }

        static Color ReadMentorColor(string property) => ReadMentor(property) is Color color ? color : Color.clear;
        static bool IsCyan(Color color) => color.g > .6f && color.b > .6f && color.r < .6f;
        static bool IsGreen(Color color) => color.g > color.r + .08f && color.g > color.b + .03f;
        static bool IsCoral(Color color) => color.r > color.g + .08f && color.r > color.b + .08f;

        static string ActiveText(string name)
        {
            var obj = GameObject.Find(name);
            return obj == null ? "" : string.Join("\n", obj.GetComponentsInChildren<Text>().Where(x => x.isActiveAndEnabled).Select(x => x.text));
        }

        static IEnumerator Screenshot(string name, bool waitForReady = true)
        {
            if (waitForReady) yield return WaitMentorReady();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
            report.screenshots.Add(name);
            yield return .35d;
        }

        static ExplorationTarget FindTarget(string name)
        {
            var target = GameObject.Find(name)?.GetComponent<ExplorationTarget>();
            Require(target != null, "No existe el objeto " + name);
            return target;
        }

        static object Read(string property)
        {
            if (director == null) return null;
            var type = director.GetType();
            return type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance)?.GetValue(director) ??
                   type.GetField(property, BindingFlags.Public | BindingFlags.Instance)?.GetValue(director);
        }
        static string ReadString(string property) => Read(property)?.ToString() ?? "";
        static bool ReadBool(string property) => Read(property) is bool value && value;
        static int ReadInt(string property) => Read(property) is int value ? value : -1;
        static string Stage => ReadString("StageId");
        static bool TerminalOpen
        {
            get
            {
                object mentor = Read("mentor");
                return mentor != null && mentor.GetType().GetProperty("IsTerminalOpen")?.GetValue(mentor) is bool value && value;
            }
        }
        static string ReadMentorString(string property)
        {
            return ReadMentor(property)?.ToString() ?? "";
        }
        static object ReadMentor(string property)
        {
            object mentor = Read("mentor");
            return mentor?.GetType().GetProperty(property)?.GetValue(mentor);
        }
        static int ReadMentorInt(string property) => ReadMentor(property) is int value ? value : -1;
        static bool ReadMentorBool(string property)
        {
            return ReadMentor(property) is bool value && value;
        }
        static void QueueMouse(MouseState state) => pendingMouse = state;
        static void QueueKeyboard(KeyboardState state) => pendingKeyboard = state;
        static void InjectQueuedInput()
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            if (pendingMouse.HasValue && Mouse.current != null)
            {
                InputSystem.QueueStateEvent(Mouse.current, pendingMouse.Value);
                pendingMouse = null;
            }
            if (pendingKeyboard.HasValue && Keyboard.current != null)
            {
                InputSystem.QueueStateEvent(Keyboard.current, pendingKeyboard.Value);
                pendingKeyboard = null;
            }
        }
        static bool At(int year, int month, int day, int hour, int minute) =>
            game.CurrentTime.Year == year && game.CurrentTime.Month == month && game.CurrentTime.Day == day &&
            game.CurrentTime.Hour == hour && game.CurrentTime.Minute == minute;
        static void Require(bool value, string explanation) { if (!value) throw new InvalidOperationException(explanation); }
        static void Add(string id, bool passed, string observed) => report.checks.Add(new Check { id = id, passed = passed, observed = observed });
        static void CaptureError(string message, string trace, LogType kind)
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) report.errors.Add(kind + ": " + message + "\n" + trace);
        }

        static void Finish(bool complete, bool leavePlayMode = true)
        {
            if (ending) return;
            ending = true;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= CaptureError;
            InputSystem.onBeforeUpdate -= InjectQueuedInput;
            pendingMouse = null;
            pendingKeyboard = null;
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            if (Mouse.current != null) InputSystem.QueueStateEvent(Mouse.current, new MouseState());
            if (settingsOverridden)
            {
                Application.runInBackground = previousRunInBackground;
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
                settingsOverridden = false;
            }
            report.elapsedSeconds = (float)(EditorApplication.timeSinceStartup - beganAt);
            report.mentorMessages = ReadMentorInt("MentorMessageCount");
            report.playerMessages = ReadMentorInt("PlayerMessageCount");
            report.notificationSounds = ReadMentorInt("NotificationSoundCount");
            bool passed = complete && report.checks.Count == 31 && report.checks.All(x => x.passed) && report.errors.Count == 0;
            report.status = passed ? "functional_checks_passed_visual_review_pending" : "failed";
            report.visualReviewRequired = passed;
            File.WriteAllText(Path.Combine(output, "resultados.json"), JsonUtility.ToJson(report, true), Encoding.UTF8);
            var md = new StringBuilder("# Recorrido narrativo con XR Interaction Simulator\n\n");
            md.AppendLine("Ejecución UTC: " + report.timestampUtc + ". Escena: `" + report.scene + "`. Unity: " + report.unityVersion + ".");
            md.AppendLine("Estado: `" + report.status + "`. Entradas: teclado y ratón del Input System para simulador XR y botones visibles; texto mediante eventos de teclado de Game View cuando el InputField UGUI requiere Event.PopEvent. No se asigna el texto del campo ni se invocan callbacks de progreso desde la prueba.");
            md.AppendLine("Duración configurada del recurso: " + report.configuredDurationSeconds + " s. El reloj narrativo avanza entre hitos y se detiene para la lectura. Recurso sin modificación: " + report.durationAssetUnchanged + ".");
            md.AppendLine("Tiempo de validación: " + report.elapsedSeconds.ToString("F1") + " s.\n\n| Caso | Resultado | Observación |\n| --- | --- | --- |");
            foreach (var check in report.checks) md.AppendLine("| " + check.id + " | " + (check.passed ? "Cumple" : "Falla") + " | " + check.observed.Replace("|", "/") + " |");
            md.AppendLine("\n## Capturas\n");
            foreach (string shot in report.screenshots) md.AppendLine("- [" + shot + "](" + Path.Combine(output, shot).Replace('\\', '/') + ")");
            if (report.errors.Count > 0) md.AppendLine("\n## Incidencias\n\n```text\n" + string.Join("\n", report.errors) + "\n```");
            File.WriteAllText(Path.Combine(output, "Resultados_Narrativa.md"), md.ToString(), Encoding.UTF8);
            if (leavePlayMode) EditorApplication.isPlaying = false;
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
            if (!view.maximized) view.maximized = true;
            if (EditorWindow.focusedWindow != view) view.Focus();
        }
    }
}
#endif
