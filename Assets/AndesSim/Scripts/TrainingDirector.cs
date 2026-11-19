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
        float lastActionTime;
        int trainingGeneration;
        string objective, chapter;
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
            mentor.RestartRequested += BeginNewTraining;
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
                mentor.RestartRequested -= BeginNewTraining;
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
            inspectedEvidence = null;
            mentor.CloseTerminal();
            attentionTarget = null;
            string explanation;
            string action = null;
            Action callback = null;
            bool modal = false;
            switch (next)
            {
                case TrainingStage.Welcome:
                    chapter = "01 / BIENVENIDA AL EQUIPO";
                    objective = "Reconstruye el incidente y aprende a impedir que se repita";
                    explanation = "Soy Elliot Alderson. Investigaremos una sesión nocturna y probaremos cómo prevenir otro incidente. Los datos del ejercicio son ficticios.";
                    action = "Comenzar entrenamiento";
                    callback = () => Enter(TrainingStage.Orientation);
                    modal = true;
                    break;
                case TrainingStage.Orientation:
                    chapter = "02 / RECONSTRUIR EL ACCESO";
                    objective = "Acércate a la TERMINAL SWIFT y selecciónala";
                    explanation = "SWIFT transmite órdenes de pago. Cuando una orden se envía, su procesamiento aún está pendiente.\n\nArrastra para mirar. Usa «Llévame al objetivo» o toca un destino de teletransporte. Cuando llegues, toca la terminal marcada.";
                    attentionTarget = Target("terminal_swift");
                    break;
                case TrainingStage.AccessQuery:
                    chapter = "02 / RECONSTRUIR EL ACCESO";
                    objective = "Ejecuta la consulta de accesos y revisa la sesión de las 20:36";
                    explanation = "La sesión empezó el 4 de febrero de 2016 a las 20:36. Los filtros --from y --to delimitan el horario que vamos a revisar.";
                    break;
                case TrainingStage.AccessReview:
                    chapter = "02 / RECONSTRUIR EL ACCESO";
                    objective = "Identifica el acceso fuera de horario en la salida";
                    explanation = "La cuenta abrió una sesión fuera de horario y sin segundo factor.";
                    break;
                case TrainingStage.Transmission:
                    chapter = "03 / OBSERVAR LOS ENVÍOS";
                    objective = "Observa el reloj y el contador hasta las 22:06";
                    explanation = "Observa cómo suben las órdenes hasta las 22:06, mientras la impresora sigue apagada.";
                    break;
                case TrainingStage.PaymentQuery:
                    chapter = "03 / OBSERVAR LOS ENVÍOS";
                    objective = "Selecciona la terminal y consulta las órdenes enviadas";
                    explanation = "Son las 22:06 y hay 36 órdenes. Toca la terminal para comprobar cuáles salieron.";
                    attentionTarget = Target("terminal_swift");
                    break;
                case TrainingStage.PaymentReview:
                    chapter = "03 / OBSERVAR LOS ENVÍOS";
                    objective = "Relaciona las 36 órdenes con la impresora apagada";
                    explanation = "La cola confirma 36 órdenes enviadas, aunque la impresora estaba apagada.";
                    break;
                case TrainingStage.SessionEnd:
                    chapter = "03 / FIN DE LA RECONSTRUCCIÓN";
                    objective = "Revisa el cierre a las 03:59 del día siguiente";
                    explanation = "La sesión terminó a las 03:59 del 5 de febrero. Busca los tres hallazgos de la sala para reconstruir qué ocurrió.";
                    action = "Investigar la sala";
                    callback = () => Enter(TrainingStage.Investigate);
                    break;
                case TrainingStage.Investigate:
                    chapter = "04 / CONSERVAR LAS EVIDENCIAS";
                    objective = "Inspecciona y guarda las tres evidencias de la sala";
                    explanation = InvestigationMessage();
                    attentionTarget = NextEvidenceTarget();
                    break;
                case TrainingStage.PrepareDefense:
                    chapter = "05 / PREPARAR LA DEFENSA";
                    objective = "Ve al PANEL DE SEGURIDAD y selecciónalo";
                    explanation = "Ya tienes las tres evidencias. Toca el panel de seguridad para activar los controles antes de repetir el intento.";
                    attentionTarget = Target("panel_controles");
                    break;
                case TrainingStage.Mfa:
                    chapter = "05 / AUTENTICACIÓN";
                    objective = "Activa MFA en el panel de seguridad";
                    explanation = "MFA exige un segundo factor, como una llave de seguridad. Actívalo y observa cómo cambia el indicador.";
                    action = "Activar MFA";
                    callback = () => ActivateControl(DefenseControl.MfaId, TrainingStage.Schedule);
                    attentionTarget = Target("panel_controles");
                    break;
                case TrainingStage.Schedule:
                    chapter = "05 / AUTORIZACIÓN";
                    objective = "Activa la política de horario con bloqueo y alerta";
                    explanation = "La política permite el acceso de 08:00 a 18:00. Fuera de ese horario, bloquea la sesión y envía una alerta.";
                    action = "Activar horario y bloqueo";
                    callback = () => ActivateControl(DefenseControl.ScheduleId, TrainingStage.Siem);
                    attentionTarget = Target("panel_controles");
                    break;
                case TrainingStage.Siem:
                    chapter = "05 / OBSERVABILIDAD Y EVIDENCIA";
                    objective = "Activa el SIEM con protección del registro";
                    explanation = "El SIEM reúne los eventos para investigarlos. Activa la copia central con acceso restringido y protección de integridad.";
                    action = "Activar SIEM protegido";
                    callback = () => ActivateControl(DefenseControl.SiemId, TrainingStage.DefenseReady);
                    attentionTarget = Target("panel_controles");
                    break;
                case TrainingStage.DefenseReady:
                    chapter = "06 / PROBAR LOS CONTROLES";
                    objective = "Regresa a la TERMINAL SWIFT y selecciona el intento protegido";
                    explanation = "Con los tres controles activos, vuelve a la terminal y repite el intento de las 20:36.";
                    attentionTarget = Target("terminal_swift");
                    break;
                case TrainingStage.DefenseResult:
                    chapter = "06 / INTENTO BLOQUEADO";
                    objective = "Comprueba el bloqueo, la baliza roja y las 0 órdenes";
                    explanation = "La sesión se bloqueó a las 20:36 con 0 órdenes. La baliza y la alarma avisan del rechazo. El SIEM conserva el evento.";
                    action = "Revisar el evento protegido";
                    callback = () => Enter(TrainingStage.ProtectedQuery);
                    break;
                case TrainingStage.ProtectedQuery:
                    chapter = "07 / VERIFICAR LA EVIDENCIA";
                    objective = "Selecciona la terminal y consulta el evento bloqueado";
                    explanation = "Toca la terminal para consultar el evento de bloqueo que quedó registrado en el SIEM.";
                    attentionTarget = Target("terminal_swift");
                    break;
                case TrainingStage.ProtectedReview:
                    chapter = "07 / VERIFICACIÓN COMPLETADA";
                    objective = "Identifica el evento centralizado de bloqueo";
                    explanation = "El evento de las 20:36 confirma el bloqueo y las 0 órdenes enviadas.";
                    break;
                case TrainingStage.Comparison:
                    chapter = "08 / COMPARAR LAS DOS EJECUCIONES";
                    objective = "Compara tus resultados reales: 36 órdenes frente a 0";
                    explanation = "ATAQUE · EJECUCIÓN PREVIA: " + game.AttackResultOrders + " órdenes enviadas.\nDEFENSA · EJECUCIÓN ACTUAL: " + game.DefenseResultOrders + " órdenes enviadas.\n\nReuniste " + TrainingEvidenceCount + "/3 evidencias y completaste " + QueryCount + "/3 consultas. Los controles estaban activos antes del segundo intento.";
                    action = "Revisar lo aprendido";
                    callback = () => Enter(TrainingStage.Debrief);
                    modal = true;
                    break;
                case TrainingStage.Debrief:
                    chapter = "08 / CIERRE CON ELLIOT";
                    objective = "Relaciona cada acción con su función de seguridad";
                    explanation = "MFA verifica un segundo factor. La política de horario decide cuándo se autoriza el acceso y bloquea las sesiones fuera de turno.\n\nEl SIEM conserva la evidencia del intento. Contrasta siempre los registros con otras fuentes.";
                    action = "Finalizar entrenamiento";
                    callback = () => Enter(TrainingStage.Completed);
                    modal = true;
                    break;
                default:
                    chapter = "ENTRENAMIENTO COMPLETADO";
                    objective = "Caso cerrado · 36 → 0 órdenes";
                    explanation = "Caso cerrado. Puedes empezar otro entrenamiento o repetir la prueba protegida con el expediente actual.\n\nCRÉDITOS\nEntorno y sonidos: ANDES-SIM.\nUnity XR Interaction Toolkit, Simulator y LegacyRuntime: Unity Technologies.\n\nLiberation Sans: Google y Red Hat, SIL OFL 1.1.\nRoboto Mono: Google, Apache 2.0.\nRetrato y asistencia en el desarrollo: OpenAI.";
                    action = "Nuevo entrenamiento";
                    callback = BeginNewTraining;
                    modal = true;
                    cues?.PlayResult();
                    break;
            }
            string report = StageConfirmation(next);
            mentor.Show(chapter, objective, explanation, action, Guard(callback, report), (int)next + 1,
                Enum.GetValues(typeof(TrainingStage)).Length, modal);
            mentor.SetPendingEvidence(next == TrainingStage.Investigate ? PendingEvidenceTitles() : null);
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
            };
        }

        void OnTargetActivated(ExplorationTarget target)
        {
            if (!mentor.IsReadyForAction) return;
            if (target.kind != ExplorationTarget.Kind.Inspection)
            {
                mentor.AddPlayerMessage("Me desplacé a " + target.title + ".");
                mentor.SetMessage("Estás en " + target.title + ". Toca el equipo señalado por el marcador.");
                return;
            }
            mentor.AddPlayerMessage("Inspeccioné " + target.title + ".");
            exploration.CloseInspection();
            if (target.uniqueId == "terminal_swift")
            {
                switch (Stage)
                {
                    case TrainingStage.Orientation:
                        if (!game.StartAttack()) { mentor.SetMessage("La reconstrucción no pudo empezar. Abre Menú y usa «Reiniciar entrenamiento»."); return; }
                        game.SetSimulationPaused(true);
                        Enter(TrainingStage.AccessQuery);
                        OpenQuery(); return;
                    case TrainingStage.AccessQuery:
                    case TrainingStage.PaymentQuery:
                    case TrainingStage.ProtectedQuery:
                        OpenQuery(); return;
                    case TrainingStage.DefenseReady:
                        mentor.SetMessage("Pulsa «Simular intento protegido» para repetir el intento de las 20:36.");
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
                return;
            string description = target.uniqueId == "rack_servidor" || target.uniqueId.StartsWith("rack", StringComparison.Ordinal)
                ? "Los racks alojan servidores. Para este caso, toca el equipo marcado."
                : "Sigue el marcador verde para completar el objetivo.";
            mentor.SetMessage(description);
        }

        void OpenQuery()
        {
            TrainingStage query = Stage;
            if (query == TrainingStage.PaymentQuery && game.CurrentTime < game.data.OrdersDeadline ||
                query == TrainingStage.ProtectedQuery && !game.ProtectedEventRecorded) return;

            string command = query == TrainingStage.AccessQuery ? AccessCommand :
                query == TrainingStage.PaymentQuery ? PaymentCommand : ProtectedCommand;
            string instruction = query == TrainingStage.AccessQuery
                ? "Pulsa «Ejecutar» para buscar la sesión de las 20:36. Fíjate en la cuenta, el origen y el segundo factor."
                : query == TrainingStage.PaymentQuery
                    ? "El filtro sent muestra las órdenes enviadas. Pulsa «Ejecutar» y comprueba la hora y el total."
                    : "El filtro blocked muestra los accesos rechazados. Pulsa «Ejecutar» y revisa el motivo y la hora del bloqueo.";
            LastQuerySucceeded = false;
            LastQueryOutput = string.Empty;
            mentor.SetActions(null, null);
            mentor.SetTerminal(command, instruction, Guard(() => RunQuery(command)));
            if (query == TrainingStage.ProtectedQuery) alarm?.Silence();
        }

        void RunQuery(string command)
        {
            mentor.AddPlayerMessage("Ejecuté en la terminal:\n" + command);
            TrainingStage review;
            Action next;
            if (Stage == TrainingStage.AccessQuery)
            {
                LastQueryOutput = "FUENTE: registro local / datos ficticios\n04/02/2016 20:36  LOGIN ACCEPTED\ncuenta: operador.swift   origen: 10.24.3.17\nsegundo_factor: AUSENTE   horario: FUERA DE 08:00–18:00";
                review = TrainingStage.AccessReview;
                next = StartTransmission;
            }
            else if (Stage == TrainingStage.PaymentQuery)
            {
                LastQueryOutput = "FUENTE: cola de pagos / ejecución actual\n" + game.CurrentTime.ToString("dd/MM/yyyy HH:mm", Invariant) +
                                  "  status: SENT\nórdenes enviadas: " + game.OrdersSent +
                                  "   monto intentado: US$951.000.000\nimpresora de confirmaciones: APAGADA";
                review = TrainingStage.PaymentReview;
                next = () => { game.AdvanceGuidedTo(game.data.EndTime); Enter(TrainingStage.SessionEnd); };
            }
            else
            {
                LastQueryOutput = "FUENTE: SIEM / ejecución protegida actual\n04/02/2016 20:36  ACCESS BLOCKED\ncuenta: operador.swift   segundo_factor: REQUERIDO\nregla: ACCESO FUERA DE 08:00–18:00 → RECHAZAR\nórdenes enviadas: " + game.OrdersSent +
                                  "\nintegridad: VERIFICADA   acceso al registro: RESTRINGIDO";
                review = TrainingStage.ProtectedReview;
                next = () => Enter(TrainingStage.Comparison);
            }
            completedQueries.Add(command);
            QueryCount = completedQueries.Count;
            LastQuerySucceeded = true;
            Stage = review;
            string explanation = review == TrainingStage.AccessReview
                ? "A las 20:36, la cuenta operador.swift inició sesión fuera de horario y sin segundo factor. Reconocer la cuenta no confirma quién la usó."
                : review == TrainingStage.PaymentReview
                    ? "La cola confirma 36 envíos. La impresora estaba apagada, pero el envío siguió. US$951 millones es el monto intentado."
                    : "La regla de horario bloqueó el acceso a las 20:36. El SIEM guardó el evento con integridad verificada y acceso restringido.";
            string interpretationReport = review == TrainingStage.AccessReview
                ? "Identifiqué la sesión de las 20:36: sin segundo factor y fuera de horario."
                : review == TrainingStage.PaymentReview
                    ? "Confirmé 36 órdenes enviadas aunque la impresora estaba apagada."
                    : "Identifiqué el evento central de bloqueo a las 20:36, con cero órdenes.";
            objective = review == TrainingStage.AccessReview ? "Identifica el acceso de las 20:36" :
                review == TrainingStage.PaymentReview ? "Confirma las 36 órdenes con la impresora apagada" :
                "Identifica el evento protegido de las 20:36";
            mentor.Show(chapter, objective, explanation, "Continuar", Guard(next, interpretationReport), (int)review + 1,
                Enum.GetValues(typeof(TrainingStage)).Length);
            mentor.SetTerminalOutput(LastQueryOutput, true);
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
                detail = "REGISTRO LOCAL\n001 · 20:36 Inicio de sesión\n005 · 22:06 Cola: 36 órdenes\n008 · 03:59 Cierre de sesión\n\nFaltan las entradas 002–004 y 006–007. El salto revela huecos en el registro. Guarda la copia y contrástala con otras fuentes.";
            else if (evidence.evidenceId == ScenarioData.PrinterEvidenceId)
                detail = "La impresora está apagada. La cola confirma 36 envíos, aunque no haya comprobantes impresos. Guarda la evidencia.";
            else
                detail = "El switch conecta la terminal con la red local. Guarda su identificación como contexto. Para evaluar una intrusión harían falta registros y configuración de red.";
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
                mentor.SetMessage("El control no se pudo activar. Puedes reiniciar el entrenamiento desde Menú.");
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
                mentor.SetMessage("La alarma se silenció. La baliza y el evento del SIEM siguen activos.");
            }
        }

        string InvestigationMessage()
        {
            return "Toca cualquiera de los equipos pendientes. Revisa el hallazgo y pulsa «Guardar evidencia».";
        }

        string[] PendingEvidenceTitles() => new[] { LogViewer.EvidenceId, ScenarioData.PrinterEvidenceId, ScenarioData.SwitchEvidenceId }
            .Where(id => !collectedEvidence.Contains(id)).Select(id => game.data.GetEvidenceTitle(id)).ToArray();

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
                mentor.SetMessage("Ya estás en la estación. Toca " + attentionTarget.title + " para continuar.");
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
                var fromCamera = marker.transform.position - exploration.viewCamera.transform.position;
                marker.transform.rotation = Quaternion.LookRotation(fromCamera);
                marker.transform.localScale = Vector3.one * Mathf.Clamp(fromCamera.magnitude * .13f, .25f, 1f);
                marker.text = "▼ " + attentionTarget.title;
            }
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
