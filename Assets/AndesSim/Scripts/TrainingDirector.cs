using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace AndesSim
{
    public enum TrainingStage
    {
        Welcome, Orientation, AccessQuery, AccessReview, Transmission, PaymentQuery,
        PaymentReview, SessionEnd, Investigate, PrepareDefense, Mfa, Schedule, Siem,
        DefenseReady, DefenseResult, ProtectedQuery, ProtectedReview, Comparison, Debrief, Completed
    }

    /// <summary>
    /// Linear, action-driven training. Presentation never substitutes for evidence,
    /// a successful query, activation of a control or the actual scenario result.
    /// </summary>
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameManager), typeof(MentorPresenter))]
    public sealed class TrainingDirector : MonoBehaviour
    {
        public GameManager game;
        public ExplorationController exploration;
        public MentorPresenter mentor;
        [Range(2, 30)] public float transmissionSeconds = 8f;

        public TrainingStage Stage { get; private set; }
        public string StageId => Stage.ToString();
        public int QueryCount { get; private set; }
        public int TrainingEvidenceCount => collectedEvidence.Count;
        public bool LastQuerySucceeded { get; private set; }
        public string LastQueryOutput { get; private set; } = "";
        public bool TrainingComplete => Stage == TrainingStage.Completed;

        const string AccessCommand = "andes access --from 20:00 --to 21:00";
        const string PaymentCommand = "andes payments --status sent";
        const string ProtectedCommand = "andes events --result blocked";
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        readonly HashSet<string> collectedEvidence = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> completedQueries = new HashSet<string>(StringComparer.Ordinal);
        ExplorationTarget[] targets;
        DefenseControl[] controls;
        AudioManager cues;
        AlarmSystem alarm;
        XRInteractionSimulator simulator;
        InputActionManager simulatorInputs;
        InputAction[] suspendedActions = Array.Empty<InputAction>();
        TextMeshPro marker;
        ExplorationTarget attentionTarget;
        Vector3 originalPosition, originalForward;
        float moveX, moveY, moveZ, rotateX, rotateY, scrollRotation;
        bool inputSuspended, initialized, changing;
        float idleTime, lastActionTime;
        int hintLevel;
        int trainingGeneration;
        string currentExplanation, objective, chapter;
        string inspectedEvidence;
        Coroutine transmission;

        void Start()
        {
            game = game != null ? game : GetComponent<GameManager>();
            mentor = mentor != null ? mentor : GetComponent<MentorPresenter>();
            exploration = exploration != null ? exploration : FindAnyObjectByType<ExplorationController>();
            if (exploration == null || game.data == null || exploration.viewCamera == null)
            {
                Debug.LogError("[ANDES-SIM] El entrenamiento requiere datos y el explorador XR configurados.", this);
                enabled = false;
                return;
            }
            targets = FindObjectsByType<ExplorationTarget>();
            controls = FindObjectsByType<DefenseControl>();
            cues = GetComponent<AudioManager>();
            alarm = FindAnyObjectByType<AlarmSystem>();
            simulator = FindAnyObjectByType<XRInteractionSimulator>();
            if (simulator != null)
            {
                moveX = simulator.translateXSpeed; moveY = simulator.translateYSpeed;
                moveZ = simulator.translateZSpeed; rotateX = simulator.rotateXSensitivity;
                rotateY = simulator.rotateYSensitivity;
                scrollRotation = simulator.mouseScrollRotateSensitivity;
                simulatorInputs = simulator.GetComponent<InputActionManager>();
            }
            originalPosition = exploration.viewCamera.transform.position;
            originalForward = exploration.viewCamera.transform.forward;
            exploration.NarrativePresenter = mentor;
            exploration.SetGuidedMode(true);
            exploration.TargetActivated += OnTargetActivated;
            mentor.HelpRequested += ExplainAction;
            mentor.RepeatRequested += RepeatExplanation;
            mentor.RestartRequested += BeginNewTraining;
            mentor.TerminalClosed += OnTerminalClosed;
            game.Changed += RefreshStatus;
            BuildMarker();
            initialized = true;
            BeginNewTraining();
        }

        void Update()
        {
            if (MobileExperience.IsPaused) return;
            if (!initialized) return;
            SetSimulatorInputSuspended(mentor.BlocksWorldInput);
            if (simulator != null)
                simulator.mouseScrollRotateSensitivity = mentor.BlocksWorldInput || mentor.PointerOverUI ? 0 : scrollRotation;
            UpdateAttention();
            bool acting = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame ||
                          Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.isPressed);
            if (acting) idleTime = 0;
            // Reading and exercises are fully explained; reminders only apply while exploring.
            if (mentor.IsReadyForAction && !mentor.IsReading && !mentor.IsTerminalOpen && inspectedEvidence == null && IsSpatialStage()) idleTime += Time.unscaledDeltaTime;
            if (idleTime >= 25 && hintLevel == 0)
            {
                hintLevel = 1;
                mentor.SetMessage(currentExplanation + "\n\nPuedes pulsar «Guíame» para ver la acción exacta o usar «Llévame» para llegar al equipo.");
            }
        }

        void OnDestroy()
        {
            if (transmission != null) StopCoroutine(transmission);
            SetSimulatorInputSuspended(false);
            if (simulator != null) simulator.mouseScrollRotateSensitivity = scrollRotation;
            if (exploration != null)
            {
                exploration.TargetActivated -= OnTargetActivated;
                exploration.NarrativePresenter = null;
            }
            if (mentor != null)
            {
                mentor.HelpRequested -= ExplainAction;
                mentor.RepeatRequested -= RepeatExplanation;
                mentor.RestartRequested -= BeginNewTraining;
                mentor.TerminalClosed -= OnTerminalClosed;
            }
            if (game != null) game.Changed -= RefreshStatus;
            if (marker != null) Destroy(marker.gameObject);
        }

        void OnDisable()
        {
            SetSimulatorInputSuspended(false);
            if (simulator != null) simulator.mouseScrollRotateSensitivity = scrollRotation;
        }

        public void BeginNewTraining()
        {
            if (!initialized) return;
            mentor.CancelPendingMessages();
            if (trainingGeneration > 0) mentor.AddPlayerMessage("Quiero comenzar un entrenamiento nuevo.");
            trainingGeneration++;
            mentor.AddSystemMessage("ENTRENAMIENTO " + trainingGeneration.ToString("00") + " · Nuevo expediente");
            if (transmission != null) { StopCoroutine(transmission); transmission = null; }
            game.ResetScenario();
            game.SelectMode(SimMode.Ataque);
            game.ClearResultHistory();
            collectedEvidence.Clear();
            completedQueries.Clear();
            QueryCount = 0;
            LastQuerySucceeded = false;
            LastQueryOutput = "";
            inspectedEvidence = null;
            exploration.CloseInspection();
            mentor.CloseTerminal();
            exploration.xrOrigin.MatchOriginUpCameraForward(Vector3.up, originalForward);
            exploration.xrOrigin.MoveCameraToWorldLocation(originalPosition);
            Physics.SyncTransforms();
            Enter(TrainingStage.Welcome);
        }

        void Enter(TrainingStage next)
        {
            Stage = next;
            idleTime = 0; hintLevel = 0; inspectedEvidence = null;
            mentor.CloseTerminal();
            attentionTarget = null;
            string action = null;
            Action callback = null;
            bool modal = false;
            switch (next)
            {
                case TrainingStage.Welcome:
                    chapter = "01 / BIENVENIDA AL EQUIPO";
                    objective = "Reconstruye el incidente y aprende a impedir que se repita";
                    currentExplanation = "Soy Elliot Alderson, ingeniero senior de ciberseguridad del Banco Andino. Hoy trabajarás conmigo: investigaremos una sesión nocturna, sus 36 órdenes de pago y las señales que quedaron en esta sala. Después aplicarás las defensas y comprobarás el resultado.\n\nEste teléfono será nuestro canal de trabajo. Me informarás de las acciones que completes y te responderé con el siguiente paso. Puedes revisar toda la conversación con la barra lateral. Los datos y comandos del laboratorio son ficticios.";
                    action = "Comenzar entrenamiento";
                    callback = () => Enter(TrainingStage.Orientation);
                    modal = true;
                    break;
                case TrainingStage.Orientation:
                    chapter = "02 / RECONSTRUIR EL ACCESO";
                    objective = "Acércate a la TERMINAL SWIFT y selecciónala";
                    currentExplanation = "Empecemos por la terminal que envía las órdenes. Sigue el marcador verde. Orienta la cámara con el joystick o el giroscopio. Toca la terminal o céntrala y pulsa Inspeccionar. También puedes pulsar «Llévame» para llegar y después seleccionarla.\n\nSWIFT es una red de mensajería financiera: una orden enviada aún no equivale a dinero robado.";
                    attentionTarget = Target("terminal_swift");
                    break;
                case TrainingStage.AccessQuery:
                    chapter = "02 / RECONSTRUIR EL ACCESO";
                    objective = "Ejecuta la consulta de accesos y revisa la sesión de las 20:36";
                    currentExplanation = "La reconstrucción está detenida a las 20:36 del 4 de febrero de 2016. Consultaremos quién abrió la sesión y desde dónde. Te explicaré aquí el comando completo: puedes escribirlo o cargar el ejemplo en la terminal de la izquierda. Los comandos aparecen en cian y las salidas válidas en verde. Ninguna contraseña aparece en el registro.";
                    break;
                case TrainingStage.AccessReview:
                    chapter = "02 / RECONSTRUIR EL ACCESO";
                    objective = "Identifica el acceso fuera de horario en la salida";
                    currentExplanation = "La fila de las 20:36 muestra a operador.swift desde 10.24.3.17. La cuenta es reconocida, pero no hay segundo factor y el acceso ocurre fuera del horario autorizado. Un inicio de sesión correcto no demuestra que la persona sea legítima. Señala esa fila con el botón para anotarla y observar los envíos.";
                    break;
                case TrainingStage.Transmission:
                    chapter = "03 / OBSERVAR LOS ENVÍOS";
                    objective = "Observa el reloj y el contador hasta las 22:06";
                    currentExplanation = "Continuamos la reproducción acelerada. La sesión consiguió llegar a la función de envío. Observa cómo suben las órdenes aunque la impresora permanezca apagada. Al llegar a las 22:06 detendré la reproducción para que consultes la cola de pagos.";
                    break;
                case TrainingStage.PaymentQuery:
                    chapter = "03 / OBSERVAR LOS ENVÍOS";
                    objective = "Selecciona la terminal y consulta las órdenes enviadas";
                    currentExplanation = "Ya son las 22:06 y el contador indica 36 órdenes. Vuelve a seleccionar la terminal para consultar la cola. El filtro «sent» significa enviadas. Contrastar la cola con las confirmaciones impresas nos permite detectar actividad que habría pasado inadvertida mirando solo la impresora.";
                    attentionTarget = Target("terminal_swift");
                    break;
                case TrainingStage.PaymentReview:
                    chapter = "03 / OBSERVAR LOS ENVÍOS";
                    objective = "Relaciona las 36 órdenes con la impresora apagada";
                    currentExplanation = "La cola confirma 36 órdenes enviadas a las 22:06. Su monto total intentado es US$951 millones; este laboratorio no afirma que ese dinero haya sido robado. La impresora está apagada, pero eso no detuvo los envíos. Confirma la lectura y veremos cuándo terminó la sesión.";
                    break;
                case TrainingStage.SessionEnd:
                    chapter = "03 / FIN DE LA RECONSTRUCCIÓN";
                    objective = "Revisa el cierre a las 03:59 del día siguiente";
                    currentExplanation = "La sesión terminó a las 03:59 del 5 de febrero. La primera ejecución quedó registrada con 36 órdenes. Ahora investigaremos qué evidencia local quedó. Guardaremos tres hallazgos antes de preparar una nueva ejecución protegida. Puedes visitarlos en cualquier orden.";
                    action = "Investigar la sala";
                    callback = () => Enter(TrainingStage.Investigate);
                    break;
                case TrainingStage.Investigate:
                    chapter = "04 / CONSERVAR LAS EVIDENCIAS";
                    objective = "Inspecciona y guarda las tres evidencias de la sala";
                    currentExplanation = InvestigationMessage();
                    attentionTarget = NextEvidenceTarget();
                    break;
                case TrainingStage.PrepareDefense:
                    chapter = "05 / PREPARAR LA DEFENSA";
                    objective = "Ve al PANEL DE SEGURIDAD y selecciónalo";
                    currentExplanation = "Conservamos los tres hallazgos en tu expediente. He preparado otra ejecución del mismo intento; los resultados del ataque siguen disponibles. En el panel activaremos tres controles y te explicaré sus funciones. Se aplican antes de abrir la sesión: no estamos cambiando el pasado ni borrando el incidente.";
                    attentionTarget = Target("panel_controles");
                    break;
                case TrainingStage.Mfa:
                    chapter = "05 / AUTENTICACIÓN";
                    objective = "Activa MFA en el panel de seguridad";
                    currentExplanation = "MFA añade un factor independiente a la contraseña, por ejemplo una llave de seguridad. Aunque alguien conozca la clave del operador, debe superar esa verificación. Aquí el control protege el acceso privilegiado de la terminal. Actívalo y observa cómo su indicador pasa a verde.";
                    action = "Activar MFA";
                    callback = () => ActivateControl(DefenseControl.MfaId, TrainingStage.Schedule);
                    attentionTarget = Target("panel_controles");
                    break;
                case TrainingStage.Schedule:
                    chapter = "05 / AUTORIZACIÓN";
                    objective = "Activa la política de horario con bloqueo y alerta";
                    currentExplanation = "MFA ya está activo. Ahora configuramos la autorización: en esta práctica solo se permite iniciar sesiones de 08:00 a 18:00. Fuera de ese intervalo, la política rechaza el acceso y genera una alerta. La alerta informa; la regla de bloqueo es la que impide continuar. Activa la política.";
                    action = "Activar horario y bloqueo";
                    callback = () => ActivateControl(DefenseControl.ScheduleId, TrainingStage.Siem);
                    attentionTarget = Target("panel_controles");
                    break;
                case TrainingStage.Siem:
                    chapter = "05 / OBSERVABILIDAD Y EVIDENCIA";
                    objective = "Activa el SIEM con protección del registro";
                    currentExplanation = "La autenticación y la política de horario previenen el acceso. El SIEM tiene otro papel: reúne eventos para investigarlos. En este laboratorio, la copia central usa acceso restringido y protección de integridad para conservar evidencia si se altera un log local. Actívalo; centralizar por sí solo no vuelve un registro inalterable.";
                    action = "Activar SIEM protegido";
                    callback = () => ActivateControl(DefenseControl.SiemId, TrainingStage.DefenseReady);
                    attentionTarget = Target("panel_controles");
                    break;
                case TrainingStage.DefenseReady:
                    chapter = "06 / PROBAR LOS CONTROLES";
                    objective = "Regresa a la TERMINAL SWIFT y selecciona el intento protegido";
                    currentExplanation = "Los tres indicadores están activos. Regresa a la terminal para repetir el acceso de las 20:36 con la misma situación inicial. La simulación exige este conjunto de controles como política de preparación. Vamos a comprobar tanto la prevención del envío como la evidencia del intento.";
                    attentionTarget = Target("terminal_swift");
                    break;
                case TrainingStage.DefenseResult:
                    chapter = "06 / INTENTO BLOQUEADO";
                    objective = "Comprueba el bloqueo, la baliza roja y las 0 órdenes";
                    currentExplanation = "El intento quedó bloqueado a las 20:36, antes de enviar órdenes. Observa la baliza roja y escucha la alarma. MFA y la regla de horario protegen el acceso; el SIEM conserva el evento. Confirmemos ahora esa evidencia en la terminal. Puedes silenciar la alarma sin eliminar el bloqueo ni el registro.";
                    action = "Revisar el evento protegido";
                    callback = () => Enter(TrainingStage.ProtectedQuery);
                    break;
                case TrainingStage.ProtectedQuery:
                    chapter = "07 / VERIFICAR LA EVIDENCIA";
                    objective = "Selecciona la terminal y consulta el evento bloqueado";
                    currentExplanation = "Busca el evento cuyo resultado sea «blocked». Su hora debe coincidir con el intento de las 20:36. La salida mostrará el motivo del rechazo, los controles y la copia central protegida. Así verificamos el resultado con datos, además de observar la alarma.";
                    attentionTarget = Target("terminal_swift");
                    break;
                case TrainingStage.ProtectedReview:
                    chapter = "07 / VERIFICACIÓN COMPLETADA";
                    objective = "Identifica el evento centralizado de bloqueo";
                    currentExplanation = "El evento confirma «BLOCKED», horario no autorizado y 0 órdenes. También identifica el segundo factor requerido y la copia central con acceso restringido e integridad verificada. Esto permite investigar el intento. El SIEM no envió el bloqueo: la política de acceso lo hizo y el SIEM conservó su evidencia.";
                    break;
                case TrainingStage.Comparison:
                    chapter = "08 / COMPARAR LAS DOS EJECUCIONES";
                    objective = "Compara tus resultados reales: 36 órdenes frente a 0";
                    currentExplanation = "ATAQUE · EJECUCIÓN PREVIA: " + game.AttackResultOrders + " órdenes enviadas.\nDEFENSA · EJECUCIÓN ACTUAL: " + game.DefenseResultOrders + " órdenes enviadas.\n\nConservaste " + TrainingEvidenceCount + "/3 evidencias y realizaste " + QueryCount + "/3 consultas. Cambiaron los controles antes del intento. El monto de US$951 millones corresponde a órdenes intentadas; su procesamiento posterior pertenece a otro escenario.";
                    action = "Revisar lo aprendido";
                    callback = () => Enter(TrainingStage.Debrief);
                    modal = true;
                    break;
                case TrainingStage.Debrief:
                    chapter = "08 / CIERRE CON ELLIOT";
                    objective = "Relaciona cada acción con su función de seguridad";
                    currentExplanation = "Trabajaste como parte del equipo: consultaste el acceso y la cola, contrastaste tres evidencias y verificaste el bloqueo.\n\nAUTENTICACIÓN · MFA verifica un factor adicional.\nAUTORIZACIÓN · La política limita cuándo se permite acceder.\nPREVENCIÓN · El rechazo evita el envío en esta prueba.\nOBSERVABILIDAD · El SIEM conserva el evento protegido.\n\nUna impresora apagada no demuestra ausencia de operaciones; un hueco exige contrastar fuentes, no inventar lo que falta.";
                    action = "Finalizar entrenamiento";
                    callback = () => Enter(TrainingStage.Completed);
                    modal = true;
                    break;
                default:
                    chapter = "ENTRENAMIENTO COMPLETADO";
                    objective = "Caso cerrado · 36 → 0 órdenes";
                    currentExplanation = "Buen trabajo. Completaste el recorrido y comprobaste cómo cambió el resultado al preparar los controles. Puedes comenzar otra sesión completa o repetir solo la prueba protegida conservando la comparación.\n\nCRÉDITOS · Entorno y sonidos: ANDES-SIM. Unity XR Interaction Toolkit, Simulator y fuente LegacyRuntime: Unity Technologies. Liberation Sans: Google y Red Hat, SIL OFL 1.1. Asistencia de Codex, OpenAI, en código, narrativa y documentación. Datos, cuentas, IP y comandos ficticios.";
                    action = "Nuevo entrenamiento";
                    callback = BeginNewTraining;
                    modal = true;
                    cues?.PlayResult();
                    break;
            }
            string report = StageConfirmation(next);
            mentor.Show(chapter, objective, currentExplanation, action, Guard(callback, report), (int)next + 1,
                Enum.GetValues(typeof(TrainingStage)).Length, modal);
            if (action == null) mentor.SetActions(null, null);
            if (IsSpatialStage()) SetTravelAction();
            if (next == TrainingStage.DefenseResult)
                mentor.SetActions(action, Guard(callback, report), "Silenciar alarma", Guard(SilenceAlarm));
            if (next == TrainingStage.Completed)
                mentor.SetActions(action, Guard(callback), "Repetir prueba protegida", Guard(ReplayDefense));
            RefreshStatus();
        }

        string StageConfirmation(TrainingStage stage) => stage switch
        {
            TrainingStage.Welcome => "Estoy listo. Comencemos el entrenamiento.",
            TrainingStage.SessionEnd => "La sesión terminó a las 03:59. Voy a investigar las evidencias de la sala.",
            TrainingStage.DefenseResult => "Comprobé el bloqueo y las cero órdenes. Revisaré el evento protegido.",
            TrainingStage.Comparison => "La comparación confirma 36 órdenes frente a 0. Revisemos lo aprendido.",
            TrainingStage.Debrief => "Finalicé el caso: investigué el incidente, preparé las defensas y verifiqué el resultado.",
            _ => null
        };

        Action Guard(Action action, string report = null)
        {
            if (action == null) return null;
            TrainingStage expected = Stage;
            int generation = trainingGeneration;
            return () =>
            {
                if (changing || Stage != expected || trainingGeneration != generation || !mentor.IsReadyForAction ||
                    Time.unscaledTime - lastActionTime < .22f) return;
                lastActionTime = Time.unscaledTime;
                changing = true;
                try
                {
                    if (!string.IsNullOrWhiteSpace(report)) mentor.AddPlayerMessage(report);
                    action();
                }
                finally { changing = false; }
                idleTime = 0;
            };
        }

        void OnTargetActivated(ExplorationTarget target)
        {
            if (!mentor.IsReadyForAction) return;
            idleTime = 0;
            if (target.kind != ExplorationTarget.Kind.Inspection)
            {
                mentor.AddPlayerMessage("Me desplacé a " + target.title + ".");
                mentor.SetMessage("Estás en " + target.title + ". El objetivo sigue siendo: " + objective + ". Usa la retícula para seleccionar el equipo o las acciones de este chat para continuar.");
                return;
            }
            mentor.AddPlayerMessage("Inspeccioné " + target.title + ".");
            exploration.CloseInspection();
            if (target.uniqueId == "terminal_swift")
            {
                switch (Stage)
                {
                    case TrainingStage.Orientation:
                        if (!game.StartAttack()) { mentor.SetMessage("No se pudo preparar la reconstrucción. Usa Nuevo entrenamiento para restaurarla."); return; }
                        game.SetSimulationPaused(true);
                        Enter(TrainingStage.AccessQuery);
                        OpenQuery(); return;
                    case TrainingStage.AccessQuery:
                    case TrainingStage.PaymentQuery:
                    case TrainingStage.ProtectedQuery:
                        OpenQuery(); return;
                    case TrainingStage.DefenseReady:
                        mentor.SetMessage("Los tres controles están activos. Pulsa «Simular intento protegido» para repetir el acceso de las 20:36. Tú inicias esta prueba.");
                        mentor.SetActions("Simular intento protegido", Guard(StartProtectedAttempt), "Llévame a terminal", Guard(TravelToAttention));
                        return;
                }
            }
            if (target.uniqueId == "panel_controles" && Stage == TrainingStage.PrepareDefense)
            {
                Enter(TrainingStage.Mfa); return;
            }
            if (Stage == TrainingStage.Investigate && target.TryGetComponent<EvidenceItem>(out var evidence))
            {
                InspectEvidence(evidence); return;
            }
            if (target.uniqueId == "panel_controles" && Stage >= TrainingStage.Mfa && Stage <= TrainingStage.Siem)
            {
                mentor.SetMessage(currentExplanation); return;
            }
            string description = target.uniqueId == "rack_servidor" || target.uniqueId.StartsWith("rack", StringComparison.Ordinal)
                ? "Los racks alojan servidores. Nuestro incidente se centra en la terminal y su acceso; inspeccionar un servidor no demuestra que haya sido comprometido."
                : "Puedes explorar este equipo. Para continuar el caso, completa el objetivo que aparece arriba; el marcador verde indica el siguiente lugar.";
            mentor.SetMessage(description + "\n\n" + currentExplanation);
        }

        void OpenQuery()
        {
            if (Stage == TrainingStage.PaymentQuery && game.CurrentTime < game.data.OrdersDeadline ||
                Stage == TrainingStage.ProtectedQuery && !game.ProtectedEventRecorded) return;
            string example = Stage == TrainingStage.AccessQuery ? AccessCommand :
                Stage == TrainingStage.PaymentQuery ? PaymentCommand : ProtectedCommand;
            string purpose = Stage == TrainingStage.AccessQuery
                ? "andes es la consola ficticia del laboratorio. access consulta accesos; --from y --to delimitan el intervalo horario. Ejecuta el ejemplo y busca la fila de las 20:36: cuenta, origen y segundo factor."
                : Stage == TrainingStage.PaymentQuery
                    ? "payments consulta la cola de pagos; --status sent filtra las órdenes enviadas. Comprueba el contador y la hora, y contrástalos con la impresora apagada. Esta consulta no envía ningún pago."
                    : "events consulta la copia central; --result blocked filtra los accesos rechazados. Comprueba la hora, el motivo de bloqueo y la protección del evento. La consulta solo lee datos de esta simulación.";
            mentor.SetTerminal("TERMINAL ANDES / LABORATORIO", purpose, example, RunQuery);
            // The terminal already has a Back button. Its main continuation appears
            // only when a successful query has produced something to interpret.
            mentor.SetActions(null, null);
        }

        void OnTerminalClosed()
        {
            if (Stage == TrainingStage.AccessQuery || Stage == TrainingStage.PaymentQuery || Stage == TrainingStage.ProtectedQuery)
                mentor.SetActions("Reabrir consulta de terminal", Guard(OpenQuery, "Retomé la consulta de terminal que había dejado pendiente."));
        }

        void RunQuery(string input)
        {
            if (!mentor.IsReadyForAction || Stage != TrainingStage.AccessQuery && Stage != TrainingStage.PaymentQuery && Stage != TrainingStage.ProtectedQuery) return;
            mentor.AddPlayerMessage("Ejecuté en la terminal:\n" + (string.IsNullOrWhiteSpace(input) ? "[consulta vacía]" : input.Trim()));
            string normalized = string.Join(" ", (input ?? "").Trim().ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            string expected = Stage == TrainingStage.AccessQuery ? AccessCommand : Stage == TrainingStage.PaymentQuery ? PaymentCommand : ProtectedCommand;
            LastQuerySucceeded = normalized == expected;
            if (!LastQuerySucceeded)
            {
                LastQueryOutput = "ERROR · CONSULTA NO RECONOCIDA\ncomando esperado: " + expected + "\nresultado: sin ejecutar";
                mentor.SetTerminalOutput(LastQueryOutput, false);
                mentor.SetMessage("La consulta no se ejecutó. Revisa el nombre y los filtros. Para esta tarea usamos:\n" + expected +
                    "\n\nPulsa «Usar ejemplo» en la terminal para cargarlo, revisa sus parámetros y vuelve a ejecutar. Puedes corregirlo sin perder tu progreso. Las salidas de error se muestran en coral.");
                return;
            }
            TrainingStage review;
            string label;
            Action next;
            if (Stage == TrainingStage.AccessQuery)
            {
                LastQueryOutput = "FUENTE: registro local / datos ficticios\n04/02/2016 20:36  LOGIN ACCEPTED\ncuenta: operador.swift   origen: 10.24.3.17\nsegundo_factor: AUSENTE   horario: FUERA DE 08:00–18:00";
                review = TrainingStage.AccessReview;
                label = "Señalar acceso de las 20:36";
                next = StartTransmission;
            }
            else if (Stage == TrainingStage.PaymentQuery)
            {
                LastQueryOutput = "FUENTE: cola de pagos / ejecución actual\n" + game.CurrentTime.ToString("dd/MM/yyyy HH:mm", Invariant) +
                                  "  status: SENT\nórdenes enviadas: " + game.OrdersSent +
                                  "   monto intentado: US$951.000.000\nimpresora de confirmaciones: APAGADA";
                review = TrainingStage.PaymentReview;
                label = "Confirmar lectura y cerrar sesión";
                next = () => { game.AdvanceGuidedTo(game.data.EndTime); Enter(TrainingStage.SessionEnd); };
            }
            else
            {
                LastQueryOutput = "FUENTE: SIEM / ejecución protegida actual\n04/02/2016 20:36  ACCESS BLOCKED\ncuenta: operador.swift   segundo_factor: REQUERIDO\nregla: ACCESO FUERA DE 08:00–18:00 → RECHAZAR\nórdenes enviadas: " + game.OrdersSent +
                                  "\nintegridad: VERIFICADA   acceso al registro: RESTRINGIDO";
                review = TrainingStage.ProtectedReview;
                label = "Identificar evento y comparar";
                next = () => Enter(TrainingStage.Comparison);
            }
            completedQueries.Add(expected);
            QueryCount = completedQueries.Count;
            // Preserve the successful terminal output while changing its explanatory beat.
            Stage = review;
            idleTime = 0; hintLevel = 0;
            string output = LastQueryOutput;
            string explanation = review == TrainingStage.AccessReview
                ? "La fila de las 20:36 registra operador.swift desde 10.24.3.17: sin segundo factor y fuera de horario. Autenticar una cuenta no prueba que su usuario sea legítimo. Señala el acceso para continuar la reconstrucción."
                : review == TrainingStage.PaymentReview
                    ? "Las 36 órdenes salieron aunque la impresora estaba apagada. US$951 millones es el monto intentado, no dinero confirmado como robado. Confirma esta relación para avanzar al cierre de las 03:59."
                    : "El evento protegido coincide con las 20:36: acceso bloqueado y cero órdenes. La regla rechazó la sesión; el SIEM conservó la evidencia con integridad y acceso restringido. Identifica este evento para comparar tus dos ejecuciones.";
            currentExplanation = explanation;
            string interpretationReport = review == TrainingStage.AccessReview
                ? "Identifiqué la sesión de las 20:36: sin segundo factor y fuera de horario."
                : review == TrainingStage.PaymentReview
                    ? "Confirmé 36 órdenes enviadas aunque la impresora estaba apagada."
                    : "Identifiqué el evento central de bloqueo a las 20:36, con cero órdenes.";
            objective = review == TrainingStage.AccessReview ? "Señala la fila del acceso de las 20:36" :
                review == TrainingStage.PaymentReview ? "Confirma las 36 órdenes con la impresora apagada" :
                "Identifica el evento protegido de las 20:36";
            mentor.Show(chapter, objective, explanation, label, Guard(next, interpretationReport), (int)review + 1,
                Enum.GetValues(typeof(TrainingStage)).Length);
            mentor.SetTerminalOutput(output, true);
            mentor.SetActions(label, Guard(next, interpretationReport));
            cues?.PlayNotice();
            RefreshStatus();
        }

        void StartTransmission()
        {
            Enter(TrainingStage.Transmission);
            transmission = StartCoroutine(PlayTransmission());
        }

        IEnumerator PlayTransmission()
        {
            while (!mentor.IsReadyForAction) yield return null;
            DateTime start = game.CurrentTime;
            DateTime end = game.data.OrdersDeadline;
            float elapsed = 0;
            while (elapsed < transmissionSeconds)
            {
                if (MobileExperience.IsPaused) { yield return null; continue; }
                elapsed += Time.unscaledDeltaTime;
                double fraction = Math.Min(1, elapsed / Math.Max(.1f, transmissionSeconds));
                game.AdvanceGuidedTo(start.AddTicks((long)((end.Ticks - start.Ticks) * fraction)));
                yield return null;
            }
            game.AdvanceGuidedTo(end);
            transmission = null;
            Enter(TrainingStage.PaymentQuery);
        }

        void InspectEvidence(EvidenceItem evidence)
        {
            if (evidence.evidenceId != LogViewer.EvidenceId && evidence.evidenceId != ScenarioData.SwitchEvidenceId &&
                evidence.evidenceId != ScenarioData.PrinterEvidenceId) return;
            inspectedEvidence = evidence.evidenceId;
            attentionTarget = Target(evidence.evidenceId == LogViewer.EvidenceId ? "terminal_swift" : evidence.evidenceId);
            string detail;
            if (evidence.evidenceId == LogViewer.EvidenceId)
                detail = "REGISTRO LOCAL\n001  20:36 Sesión iniciada\n002–004  [HUECO: entradas borradas]\n005  22:06 Cola: 36 órdenes\n006–007  [HUECO: entradas borradas]\n008  03:59 Sesión cerrada\n\nFaltan entradas numeradas. Guarda la copia y contrástala con otras fuentes; no podemos reconstruir su contenido solo por el salto.";
            else if (evidence.evidenceId == ScenarioData.PrinterEvidenceId)
                detail = "IMPRESORA APAGADA\n\nEl indicador está apagado y no hay confirmaciones impresas. Sin embargo, la consulta de la cola confirmó 36 envíos. Guarda esta anomalía. El papel es una señal operativa, no un control que impida enviar pagos.";
            else
                detail = "SWITCH DE LA RED LOCAL\n\nEste equipo conecta la terminal con la red. Conserva su identificación como contexto de infraestructura. Su apariencia o precio no demuestran una intrusión. Para evaluar segmentación harían falta configuración, rutas y registros de red; ese análisis pertenece a otro escenario.";
            currentExplanation = detail;
            mentor.SetMessage(detail);
            if (collectedEvidence.Contains(evidence.evidenceId))
                mentor.SetActions("Ya guardada · seguir investigando", Guard(() => Enter(TrainingStage.Investigate)));
            else
                mentor.SetActions("Guardar evidencia", Guard(() => SaveEvidence(evidence)));
        }

        void SaveEvidence(EvidenceItem item)
        {
            if (Stage != TrainingStage.Investigate || inspectedEvidence != item.evidenceId) return;
            item.Collect(game);
            if (!game.HasEvidence(item.evidenceId)) return;
            if (collectedEvidence.Add(item.evidenceId))
            {
                mentor.AddPlayerMessage("Guardé la evidencia: " + game.data.GetEvidenceTitle(item.evidenceId) + ".");
                cues?.PlayEvidence();
            }
            if (collectedEvidence.Count == 3)
            {
                game.ResetScenario();
                game.SelectMode(SimMode.Defensa);
                Enter(TrainingStage.PrepareDefense);
            }
            else Enter(TrainingStage.Investigate);
        }

        void ActivateControl(string id, TrainingStage next)
        {
            var control = controls.FirstOrDefault(value => value.defenseId == id);
            if (control == null || !(control.Activate(game) || game.HasDefense(id)))
            {
                mentor.SetMessage("No se activó el control. La prueba debe estar en preparación de defensa. Pulsa Nuevo entrenamiento para restaurar el recorrido.");
                return;
            }
            mentor.AddPlayerMessage("Activé " + control.Title + " y comprobé su indicador.");
            cues?.PlayNotice();
            Enter(next);
        }

        void StartProtectedAttempt()
        {
            if (Stage != TrainingStage.DefenseReady || !game.DefensesReady || !game.StartDefense()) return;
            mentor.AddPlayerMessage("Inicié el intento protegido con MFA, política de horario y SIEM preparados.");
            Enter(TrainingStage.DefenseResult);
        }

        void ReplayDefense()
        {
            mentor.AddPlayerMessage("Quiero repetir la prueba protegida conservando el expediente y la comparación.");
            if (alarm != null) alarm.Silence();
            game.ResetScenario();
            game.SelectMode(SimMode.Defensa);
            // Repeating this run preserves the completed investigation and attack snapshot.
            Enter(TrainingStage.PrepareDefense);
        }

        void SilenceAlarm()
        {
            if (alarm != null)
            {
                alarm.Silence();
                mentor.AddPlayerMessage("Silencié la alarma después de comprobar el bloqueo.");
                mentor.SetMessage("Recibido. La baliza y el registro protegido siguen activos. Puedes continuar con la verificación del evento.");
            }
        }

        string InvestigationMessage()
        {
            string pending = string.Join(", ", new[] { LogViewer.EvidenceId, ScenarioData.PrinterEvidenceId, ScenarioData.SwitchEvidenceId }
                .Where(id => !collectedEvidence.Contains(id)).Select(id => game.data.GetEvidenceTitle(id)));
            return "Selecciona un equipo marcado y pulsa «Guardar evidencia» después de revisar mi explicación. Puedes elegir el orden. Cada hallazgo se guarda una sola vez.\n\nEXPEDIENTE: " + collectedEvidence.Count + "/3.\nPor revisar: " + pending + ".";
        }

        void SetTravelAction()
        {
            if (!IsSpatialStage()) return;
            mentor.SetActions("Llévame al objetivo", Guard(TravelToAttention));
        }

        void TravelToAttention()
        {
            if (attentionTarget == null) attentionTarget = NextEvidenceTarget();
            if (attentionTarget == null) return;
            string stationId = attentionTarget.uniqueId == "terminal_swift" ? "TP_Terminal" :
                attentionTarget.uniqueId == "panel_controles" ? "TP_Controles" :
                attentionTarget.uniqueId == ScenarioData.PrinterEvidenceId ? "TP_Impresora" : "TP_Switch";
            var station = Target(stationId);
            if (exploration.TravelToStation(station))
            {
                mentor.AddPlayerMessage("Llegué a la estación de " + attentionTarget.title + ".");
                mentor.SetMessage("Ya estás en la estación. Centra la retícula en " + attentionTarget.title +
                    " y pulsa Inspeccionar, o toca directamente el equipo. Usa el joystick o el giroscopio para mirar.\n\n" + currentExplanation);
            }
        }

        ExplorationTarget Target(string id) => targets.FirstOrDefault(target => target.uniqueId == id);

        ExplorationTarget NextEvidenceTarget()
        {
            if (!collectedEvidence.Contains(LogViewer.EvidenceId)) return Target("terminal_swift");
            if (!collectedEvidence.Contains(ScenarioData.PrinterEvidenceId)) return Target(ScenarioData.PrinterEvidenceId);
            return Target(ScenarioData.SwitchEvidenceId);
        }

        bool IsSpatialStage() => Stage == TrainingStage.Orientation || Stage == TrainingStage.PaymentQuery ||
            Stage == TrainingStage.Investigate || Stage == TrainingStage.PrepareDefense ||
            Stage == TrainingStage.DefenseReady || Stage == TrainingStage.ProtectedQuery;

        void RepeatExplanation()
        {
            if (!mentor.IsReadyForAction) return;
            idleTime = 0;
            mentor.AddPlayerMessage("¿Puedes repetir la explicación del objetivo actual?");
            mentor.SetMessage(currentExplanation);
        }

        void ExplainAction()
        {
            if (!mentor.IsReadyForAction) return;
            idleTime = 0;
            mentor.AddPlayerMessage("Necesito que me guíes paso a paso para completar esta acción.");
            bool reviewing = Stage == TrainingStage.AccessReview || Stage == TrainingStage.PaymentReview || Stage == TrainingStage.ProtectedReview;
            string action = mentor.IsTerminalOpen
                ? reviewing
                    ? "La consulta ya se ejecutó. Lee la salida y mi explicación en este chat, y pulsa el botón de confirmación del teléfono para continuar. Puedes volver a la sala y retomar esta misma confirmación desde el teléfono."
                    : "Pulsa «Usar ejemplo», lee el comando y pulsa «Ejecutar». Revisa la salida y usa el botón de confirmación que aparecerá al obtener el resultado. Si la consulta falla puedes corregirla aquí."
                : Stage == TrainingStage.Investigate && inspectedEvidence != null
                    ? "Revisa el hallazgo y pulsa «Guardar evidencia». Después veremos cuál falta."
                    : IsSpatialStage()
                        ? "Pulsa «Llévame al objetivo» si necesitas llegar. Orienta la cámara con joystick o giroscopio. Toca el equipo marcado o usa Inspeccionar. Para desplazarte usa teletransporte o Llévame."
                        : "Lee mi explicación y pulsa el botón de acción visible. No hay una elección secreta ni penalización: cada paso te muestra lo que vamos a comprobar.";
            mentor.SetMessage(mentor.IsTerminalOpen
                ? "GUÍA DE LA ACCIÓN\n" + action + "\n\n" + (reviewing ? currentExplanation : mentor.OriginalTerminalExplanation)
                : currentExplanation + "\n\nGUÍA DE LA ACCIÓN\n" + action);
        }

        void RefreshStatus()
        {
            if (mentor == null || game == null) return;
            string source = game.mode == SimMode.Ataque ? "RECONSTRUCCIÓN" : "PRUEBA PROTEGIDA";
            mentor.SetStatus(source + " · " + game.CurrentTime.ToString("dd/MM HH:mm", Invariant) +
                "\n" + game.OrdersSent + " ÓRDENES · EVIDENCIAS " + TrainingEvidenceCount + "/3 · CONTROLES " + game.DefenseCount + "/3");
        }

        void BuildMarker()
        {
            var obj = new GameObject("Objetivo del entrenamiento", typeof(TextMeshPro));
            marker = obj.GetComponent<TextMeshPro>();
            marker.font = TMP_Settings.defaultFontAsset;
            marker.fontSize = 3.4f;
            marker.alignment = TextAlignmentOptions.Center;
            marker.color = new Color(.35f, 1f, .76f);
            marker.rectTransform.sizeDelta = new Vector2(3.3f, .9f);
        }

        void UpdateAttention()
        {
            if (marker == null) return;
            bool visible = attentionTarget != null && !mentor.IsTerminalOpen && IsSpatialStage();
            marker.gameObject.SetActive(visible);
            if (visible)
            {
                marker.transform.position = attentionTarget.AimPosition + Vector3.up * (1.15f + .04f * Mathf.Sin(Time.unscaledTime * 2));
                marker.transform.rotation = Quaternion.LookRotation(marker.transform.position - exploration.viewCamera.transform.position);
                marker.text = "▼ OBJETIVO\n" + attentionTarget.title;
            }
            var looked = exploration.CurrentTarget;
            mentor.SetWorldHint(mentor.IsTerminalOpen ? "CONSOLA DE ENTRENAMIENTO · comandos internos, datos ficticios" :
                looked != null ? (looked.kind == ExplorationTarget.Kind.Inspection ? "TOCA · INSPECCIONAR " : "TOCA · IR A ") + looked.title :
                "Cámara: joystick o giroscopio · Toca un equipo o usa Inspeccionar");
        }

        void SetSimulatorInputSuspended(bool suspended)
        {
            if (simulator == null || suspended == inputSuspended) return;
            inputSuspended = suspended;
            if (suspended && simulatorInputs != null && simulatorInputs.actionAssets != null)
            {
                // Suspend only the simulator's actions, not the UI or the tracked HMD.
                // This also prevents command letters from toggling simulated devices.
                suspendedActions = simulatorInputs.actionAssets.Where(asset => asset != null)
                    .SelectMany(asset => asset.actionMaps).SelectMany(map => map.actions)
                    .Where(action => action.enabled).ToArray();
                foreach (var action in suspendedActions) action.Disable();
            }
            else if (!suspended)
            {
                foreach (var action in suspendedActions) action.Enable();
                suspendedActions = Array.Empty<InputAction>();
            }
            // Leave the tracked simulated HMD alive; only suppress camera locomotion
            // while text fields or modal cards own keyboard/mouse input.
            simulator.translateXSpeed = suspended ? 0 : moveX;
            simulator.translateYSpeed = suspended ? 0 : moveY;
            simulator.translateZSpeed = suspended ? 0 : moveZ;
            simulator.rotateXSensitivity = suspended ? 0 : rotateX;
            simulator.rotateYSensitivity = suspended ? 0 : rotateY;
        }
    }
}
