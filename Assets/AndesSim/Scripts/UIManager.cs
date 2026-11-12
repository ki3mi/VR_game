using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static AndesSim.ExplorationController;

namespace AndesSim
{
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {
        public ExplorationController exploration;
        public GameManager game;
        public AudioManager audioManager;
        public DefenseControl[] defenseControls = System.Array.Empty<DefenseControl>();

        public bool SummaryVisible => view == View.Summary && exploration.PanelVisible;
        public bool LogVisible => view == View.Log && exploration.PanelVisible;
        public string StatusText => $"{stateLabel?.text}\n{clockLabel?.text}\n{ordersLabel?.text}\n{evidenceLabel?.text}\n{noticeLabel?.text}";

        enum View { None, Controls, Log, Summary }

        static readonly Color Mint = new Color(.44f, 1f, .77f);
        static readonly Color Pale = new Color(.83f, .94f, .95f);
        static readonly Color Muted = new Color(.60f, .75f, .78f);
        static readonly Color AlarmRed = new Color(1f, .38f, .32f);
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        TextMeshProUGUI stateLabel, clockLabel, ordersLabel, evidenceLabel, noticeLabel;
        Image statusAccent;
        RectTransform comparisonColumns;
        TextMeshProUGUI comparisonAttack, comparisonDefense;
        ScenarioState previousState;
        int previousOrders;
        View view;
        LogViewer openLog;
        EvidenceItem openLogEvidence;
        string noticeOverride;
        float noticeExpiry;

        void Start()
        {
            // The guided experience presents its own terminal and objectives.
            // Return before subscribing so legacy handlers cannot replace its UI.
            if (TryGetComponent<TrainingDirector>(out var training) && training.enabled)
            {
                enabled = false;
                return;
            }
            if (exploration == null || exploration.HudRoot == null || game == null || game.data == null)
            {
                Debug.LogError("[ANDES-SIM] UIManager necesita el explorador y los datos del escenario.", this);
                enabled = false;
                return;
            }

            BuildStatus();
            previousState = game.State;
            previousOrders = game.OrdersSent;
            exploration.TargetActivated += OnTargetActivated;
            exploration.InspectionClosed += OnInspectionClosed;
            game.Changed += OnScenarioChanged;
            RefreshStatus();
        }

        void Update()
        {
            if (noticeOverride != null && Time.unscaledTime >= noticeExpiry)
            {
                noticeOverride = null;
                RefreshStatus();
            }

            var keyboard = Keyboard.current;
            if (keyboard == null || exploration.PanelVisible) return;
            if (keyboard.iKey.wasPressedThisFrame) ShowInstructions();
            else if (game.State == ScenarioState.Resultado && keyboard.fKey.wasPressedThisFrame)
                ShowSummary();
        }

        void OnDestroy()
        {
            if (exploration != null)
            {
                exploration.TargetActivated -= OnTargetActivated;
                exploration.InspectionClosed -= OnInspectionClosed;
            }
            if (game != null) game.Changed -= OnScenarioChanged;
        }

        void BuildStatus()
        {
            var sceneMode = exploration.HudRoot.Find("Header/SceneModeLabel");
            if (sceneMode != null) sceneMode.GetComponent<TextMeshProUGUI>().text = "ETAPA 03  ·  I INSTRUCCIONES";
            if (exploration.HudRoot.Find("InspectionPanel") is RectTransform inspection)
            {
                inspection.anchoredPosition = new Vector2(0, -75);
                BuildComparison(inspection);
            }

            var card = exploration.Box(exploration.HudRoot, "AttackStatus", new Vector2(467, 260),
                new Vector2(606, 194), new Color(.016f, .048f, .058f));
            card.SetAsFirstSibling();
#if UNITY_EDITOR
            card.anchorMin = card.anchorMax = new Vector2(.5f, 1f);
            card.anchoredPosition = new Vector2(467, -190);
#endif
            statusAccent = exploration.Box(card, "Accent", new Vector2(-300, 0), new Vector2(6, 194), Mint).GetComponent<Image>();
            stateLabel = AddStatusLabel(card, "AttackState", new Vector2(0, 69), new Vector2(554, 28), 19, Mint);
            clockLabel = AddStatusLabel(card, "AttackClock", new Vector2(-91, 26), new Vector2(371, 46), 30, Pale);
            ordersLabel = AddStatusLabel(card, "AttackOrders", new Vector2(205, 28), new Vector2(144, 36), 27, Pale);
            AddStatusLabel(card, "OrdersCaption", new Vector2(205, 0), new Vector2(144, 23), 14, Muted).text = "ÓRDENES";
            evidenceLabel = AddStatusLabel(card, "AttackEvidence", new Vector2(0, -20), new Vector2(554, 27), 18, Mint);
            noticeLabel = AddStatusLabel(card, "AttackNotice", new Vector2(0, -63), new Vector2(554, 57), 17, Muted);
            noticeLabel.enableAutoSizing = true;
            noticeLabel.fontSizeMin = 14;
            noticeLabel.fontSizeMax = 17;
        }

        TextMeshProUGUI AddStatusLabel(RectTransform card, string name, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var label = exploration.Label(card, "", position, size, fontSize, color);
            label.name = name;
            return label;
        }

        void OnTargetActivated(ExplorationTarget target)
        {
            if (target.kind != ExplorationTarget.Kind.Inspection) return;
            view = View.None;
            if (target.uniqueId == "panel_controles" && defenseControls.Length == DefenseControl.RequiredCount)
            {
                ShowDefenseControls();
                return;
            }
            target.TryGetComponent(out EvidenceItem item);
            if (target.TryGetComponent(out LogViewer log))
            {
                if (game.State == ScenarioState.Preparacion) ShowTerminal();
                else ShowLog(log, item);
            }
            else if (item != null && game.mode == SimMode.Ataque && game.State != ScenarioState.Preparacion)
            {
                exploration.ShowInspection(target.title, target.body,
                    new PanelAction("Registrar evidencia", () => Collect(item)));
            }
        }

        void OnInspectionClosed()
        {
            view = View.None;
            openLog = null;
            openLogEvidence = null;
            comparisonColumns.gameObject.SetActive(false);
        }

        void ShowTerminal()
        {
            string start = game.data.StartTime.ToString("dd/MM/yyyy HH:mm", Invariant);
            bool defense = game.mode == SimMode.Defensa;
            var changeMode = defenseControls.Length == DefenseControl.RequiredCount
                ? new PanelAction(defense ? "Modo ataque" : "Modo defensa", SelectOtherMode) : default;
            if (defense)
            {
                string controls = game.DefensesReady
                    ? "Los tres controles están activos. Puedes simular el intento de acceso."
                    : "Activa los controles en el panel de seguridad.\nFaltan: " +
                      string.Join(", ", defenseControls.Where(control => !game.HasDefense(control.defenseId)).Select(control => control.Title)) + ".";
                exploration.ShowInspection("TERMINAL SWIFT · DEFENSA", $"Intento previsto  ·  {start}\n\n{controls}",
                    new PanelAction("Simular intento de acceso", game.DefensesReady ? StartDefense : null), changeMode);
                return;
            }

            exploration.ShowInspection("TERMINAL SWIFT · ATAQUE", $"<b>{game.data.scenarioTitle}</b>\n\n" +
                $"{game.data.preparationNotice}\n\nInicio previsto  ·  {start}",
                new PanelAction("Iniciar reconstrucción", StartAttack), changeMode);
        }

        void SelectOtherMode()
        {
            noticeOverride = null;
            if (game.SelectMode(game.mode == SimMode.Ataque ? SimMode.Defensa : SimMode.Ataque)) ShowTerminal();
        }

        void ShowDefenseControls()
        {
            if (game.mode == SimMode.Ataque)
            {
                exploration.ShowInspection("CONTROLES DE SEGURIDAD", "MFA, alerta fuera de horario y SIEM.\n\n" +
                    "Elige el modo defensa desde la terminal SWIFT para activar los tres controles.");
                return;
            }

            view = View.Controls;
            string body = $"<b>{game.DefenseCount}/3 controles activos</b>\n" +
                string.Join("\n", defenseControls.Select(control =>
                    $"<b>{(game.HasDefense(control.defenseId) ? "Activo" : "Pendiente")}  ·  {control.Title}</b>\n{control.Description}"));
            if (game.State == ScenarioState.Preparacion)
                exploration.ShowInspection("CONTROLES DE SEGURIDAD", body,
                    DefenseAction(defenseControls[0], "MFA"), DefenseAction(defenseControls[1], "Fuera de horario"),
                    DefenseAction(defenseControls[2], "SIEM"));
            else exploration.ShowInspection("CONTROLES DE SEGURIDAD", body);
        }

        PanelAction DefenseAction(DefenseControl control, string label) =>
            new PanelAction(game.HasDefense(control.defenseId) ? label + " · Activo" : label, () => ActivateDefense(control));

        void ActivateDefense(DefenseControl control)
        {
            if (control.Activate(game))
            {
                audioManager?.PlayEvidence();
                Notify("Control activo · " + control.Title);
            }
            else if (game.HasDefense(control.defenseId)) Notify("Control ya activo · " + control.Title);
        }

        void StartAttack()
        {
            noticeOverride = null;
            if (game.StartAttack()) exploration.CloseInspection();
            else Notify("No se pudo iniciar la reconstrucción. Revisa los datos del escenario.");
        }

        void StartDefense()
        {
            noticeOverride = null;
            if (game.StartDefense()) exploration.CloseInspection();
        }

        void Collect(EvidenceItem item)
        {
            if (item.Collect(game))
            {
                audioManager?.PlayEvidence();
                Notify("Registrada · " + game.data.GetEvidenceTitle(item.evidenceId));
            }
            else if (game.HasEvidence(item.evidenceId))
                Notify("Evidencia ya registrada · " + game.data.GetEvidenceTitle(item.evidenceId));
        }

        void Notify(string message)
        {
            noticeOverride = message;
            noticeExpiry = Time.unscaledTime + 4f;
            RefreshStatus();
        }

        void ShowLog(LogViewer log, EvidenceItem item)
        {
            view = View.Log;
            openLog = log;
            openLogEvidence = item;
            bool defense = game.mode == SimMode.Defensa;
            var primary = !defense && item != null ? new PanelAction("Marcar como evidencia", () => Collect(item)) : default;
            var secondary = game.State == ScenarioState.Resultado ? new PanelAction("Ver comparación", ShowSummary) : default;
            string logText = log.BuildLog(game).Replace("[HUECO] REGISTROS BORRADOS",
                "<color=#FFC880>[HUECO] REGISTROS BORRADOS</color>").Replace("[REGISTRO PROTEGIDO]",
                "<color=#75F2C1>[REGISTRO PROTEGIDO]</color>");
            exploration.ShowInspection(defense ? "REGISTRO PROTEGIDO" : "REGISTRO SWIFT",
                defense ? logText : logText + "\n\n" + game.data.logGapNotice, primary, secondary);
        }

        void ShowSummary()
        {
            if (game.State != ScenarioState.Resultado) return;
            view = View.Summary;
            var evidence = game.mode == SimMode.Ataque ? new PanelAction("Ver evidencias", ShowAttackEvidence) : default;
            exploration.ShowInspection("COMPARACIÓN DE RESULTADOS", "", new PanelAction("Reiniciar escenario", ResetRun), evidence);
            string start = game.data.StartTime.ToString("dd/MM HH:mm", Invariant);
            string end = game.data.EndTime.ToString("dd/MM HH:mm", Invariant);
            int attackOrders = game.mode == SimMode.Ataque ? game.OrdersSent : game.AttackResultOrders ?? game.data.totalOrders;
            int defenseOrders = game.mode == SimMode.Defensa ? game.OrdersSent : game.DefenseResultOrders ?? 0;
            comparisonAttack.text = $"<color=#FFC880><b>ATAQUE</b></color>  ·  {ResultSource(SimMode.Ataque, game.AttackResultOrders)}\n" +
                $"<size=34><b>{attackOrders} órdenes</b></size>\n{FormatUsd(game.data.totalAmountUsd)} intentados\n" +
                $"{start} → {end}\nAcceso inadvertido\nHuecos en el registro local";
            comparisonDefense.text = $"<color=#75F2C1><b>DEFENSA</b></color>  ·  {ResultSource(SimMode.Defensa, game.DefenseResultOrders)}\n" +
                $"<size=34><b>{defenseOrders} órdenes</b></size>\nUS$0 en órdenes enviadas\n{start}\nAcceso bloqueado por MFA y horario\nEvento protegido en SIEM";
            comparisonColumns.gameObject.SetActive(true);
        }

        string ResultSource(SimMode columnMode, int? recordedOrders)
        {
            if (game.mode == columnMode) return "ACTUAL";
            return recordedOrders.HasValue ? "EJECUCIÓN PREVIA" : "REFERENCIA";
        }

        void BuildComparison(RectTransform inspection)
        {
            comparisonColumns = exploration.Box(inspection, "ComparisonColumns", new Vector2(0, -15),
                new Vector2(760, 195), Color.clear);
            var attack = exploration.Box(comparisonColumns, "AttackColumn", new Vector2(-194, 0),
                new Vector2(372, 192), new Color(.16f, .11f, .06f));
            var defense = exploration.Box(comparisonColumns, "DefenseColumn", new Vector2(194, 0),
                new Vector2(372, 192), new Color(.045f, .15f, .12f));
            comparisonAttack = AddStatusLabel(attack, "ComparisonAttack", Vector2.zero, new Vector2(342, 174), 18, Pale);
            comparisonDefense = AddStatusLabel(defense, "ComparisonDefense", Vector2.zero, new Vector2(342, 174), 18, Pale);
            comparisonAttack.alignment = comparisonDefense.alignment = TextAlignmentOptions.TopLeft;
            comparisonColumns.gameObject.SetActive(false);
        }

        void ShowAttackEvidence()
        {
            view = View.None;
            comparisonColumns.gameObject.SetActive(false);
            exploration.ShowInspection("EVIDENCIAS DEL ATAQUE", $"<b>Registradas {game.EvidenceCount}/3</b>\n\n" +
                EvidenceLine(LogViewer.EvidenceId) + "\n" + EvidenceLine(ScenarioData.SwitchEvidenceId) + "\n" +
                EvidenceLine(ScenarioData.PrinterEvidenceId), new PanelAction("Volver a comparación", ShowSummary));
        }

        void ShowInstructions()
        {
            exploration.ShowInspection("INSTRUCCIONES", "Usa teletransporte para desplazarte y joystick o giroscopio para mirar.\n" +
                "Espacio o clic para inspeccionar y teletransportarte.\n" +
                "En la terminal, 2 cambia el modo y 1 inicia la simulación.\n" +
                "En el panel, 1 activa MFA, 2 el horario y 3 el SIEM.\n" +
                "Al terminar, F abre la comparación. Reinicia con 1.", new PanelAction("Ver créditos", ShowCredits));
        }

        void ShowCredits()
        {
            exploration.ShowInspection("CRÉDITOS", "<b>ANDES-SIM · Equipo 03</b>\n" +
                "Escena, modelos, materiales y sonidos sintéticos del proyecto.\n" +
                "XR Interaction Toolkit y XR Core Utilities · Unity Technologies, Unity Companion License.\n" +
                "Liberation Sans · Google Corporation y Red Hat, SIL OFL 1.1.\n" +
                "Asistencia de IA para código, geometría y documentación · Codex, OpenAI.",
                new PanelAction("Volver a instrucciones", ShowInstructions));
        }

        string EvidenceLine(string id) => $"{(game.HasEvidence(id) ? "Registrada" : "Pendiente")}  ·  {game.data.GetEvidenceTitle(id)}";

        static string FormatUsd(long amount)
        {
            const long million = 1_000_000;
            return amount >= million && amount % million == 0
                ? $"US${(amount / million).ToString(Invariant)} millones"
                : $"US${amount.ToString(Invariant)}";
        }

        void ResetRun()
        {
            exploration.CloseInspection();
            noticeOverride = null;
            game.ResetScenario();
        }

        void OnScenarioChanged()
        {
            if (previousState != game.State)
            {
                if (game.State == ScenarioState.Ejecucion) audioManager?.PlayNotice();
                if (game.State == ScenarioState.Resultado && !game.SessionBlocked) audioManager?.PlayResult();
                previousState = game.State;
            }
            else if (previousOrders < game.data.totalOrders && game.OrdersSent == game.data.totalOrders)
                audioManager?.PlayNotice();
            previousOrders = game.OrdersSent;

            RefreshStatus();
            if (LogVisible) ShowLog(openLog, openLogEvidence);
            else if (SummaryVisible) ShowSummary();
            else if (view == View.Controls && exploration.PanelVisible) ShowDefenseControls();
        }

        void RefreshStatus()
        {
            bool defense = game.mode == SimMode.Defensa;
            string state = game.State switch
            {
                ScenarioState.Ejecucion => "SESIÓN ACTIVA",
                ScenarioState.Resultado => game.SessionBlocked ? "SESIÓN BLOQUEADA" : "SESIÓN CERRADA",
                _ => "PREPARACIÓN"
            };
            stateLabel.text = $"MODO {(defense ? "DEFENSA" : "ATAQUE")}  /  {state}";
            stateLabel.color = statusAccent.color = game.SessionBlocked ? AlarmRed : Mint;
            clockLabel.text = game.CurrentTime.ToString("dd/MM/yyyy HH:mm", Invariant);
            ordersLabel.text = defense ? game.OrdersSent.ToString(Invariant) : $"{game.OrdersSent:00} / {game.data.totalOrders:00}";
            evidenceLabel.text = defense ? $"CONTROLES  {game.DefenseCount} / 3" : $"EVIDENCIAS  {game.EvidenceCount} / 3";
            string notice = game.State switch
            {
                ScenarioState.Ejecucion when game.OrdersSent == game.data.totalOrders => game.data.ordersNotice,
                ScenarioState.Ejecucion => game.data.sessionNotice,
                ScenarioState.Resultado when game.SessionBlocked => "Alarma activa. Evento protegido en SIEM. F para ver la comparación.",
                ScenarioState.Resultado => game.data.closingNotice + " F para ver la comparación.",
                _ when defense && game.DefensesReady => "Defensas listas. Simula el intento desde la terminal SWIFT.",
                _ when defense => "Activa MFA, alerta fuera de horario y SIEM en el panel de seguridad.",
                _ => game.data.preparationNotice
            };
            noticeLabel.text = noticeOverride ?? notice;
        }
    }
}
