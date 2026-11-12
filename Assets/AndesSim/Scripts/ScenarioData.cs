using System;
using System.Globalization;
using UnityEngine;

namespace AndesSim
{
    [CreateAssetMenu(fileName = "DataCenterScenario", menuName = "Andes Sim/Datos de escenario")]
    public sealed class ScenarioData : ScriptableObject
    {
        const string DateFormat = "yyyy-MM-dd HH:mm";
        internal static readonly DateTime DefaultStart = new DateTime(2016, 2, 4, 20, 36, 0);
        internal static readonly DateTime DefaultEnd = new DateTime(2016, 2, 5, 3, 59, 0);
        internal static readonly DateTime DefaultOrdersDeadline = new DateTime(2016, 2, 4, 22, 6, 0);

        public const string PrinterEvidenceId = "impresora_apagada";
        public const string SwitchEvidenceId = "switch_barato";

        [Header("Cronología de la reconstrucción")]
        [SerializeField, Tooltip(DateFormat)] string startTimestamp = "2016-02-04 20:36";
        [SerializeField, Tooltip(DateFormat)] string ordersDeadlineTimestamp = "2016-02-04 22:06";
        [SerializeField, Tooltip(DateFormat)] string endTimestamp = "2016-02-05 03:59";
        [Min(0.1f)] public float durationSeconds = 90f;
        [Min(1)] public int totalOrders = 36;
        [Min(0)] public long totalAmountUsd = 951000000;

        [Header("Narración")]
        public string scenarioTitle = "La transferencia fantasma";
        [TextArea] public string preparationNotice = "Desde la terminal SWIFT puedes iniciar la reconstrucción de la noche.";
        [TextArea] public string sessionNotice = "Se inicia una sesión fuera de horario con las credenciales del operador. El acceso pasa inadvertido.";
        [TextArea] public string ordersNotice = "La terminal ha enviado todas las órdenes del ataque. La impresora de confirmaciones permanece apagada.";
        [TextArea] public string closingNotice = "El intruso cierra la sesión. Las evidencias de la noche siguen disponibles para su revisión.";
        [TextArea] public string logGapNotice = "Los saltos en la numeración revelan que se borraron registros locales.";

        public DateTime StartTime => ParseOrDefault(startTimestamp, DefaultStart);
        public DateTime EndTime => ParseOrDefault(endTimestamp, DefaultEnd);
        public DateTime OrdersDeadline => ParseOrDefault(ordersDeadlineTimestamp, DefaultOrdersDeadline);

        public string GetEvidenceTitle(string id)
        {
            switch (id)
            {
                case LogViewer.EvidenceId: return "Huecos en el registro SWIFT";
                case PrinterEvidenceId: return "Impresora de confirmaciones apagada";
                case SwitchEvidenceId: return "Switch de la red local";
                default: return string.IsNullOrWhiteSpace(id) ? "Evidencia sin identificar" : id;
            }
        }

        internal bool TryCreateClock(out SimClock clock, out string error)
        {
            clock = null;
            if (!TryParse(startTimestamp, out DateTime start) ||
                !TryParse(ordersDeadlineTimestamp, out DateTime deadline) ||
                !TryParse(endTimestamp, out DateTime end) || start >= deadline || deadline > end)
            {
                error = "La cronología debe usar el formato yyyy-MM-dd HH:mm y ordenar inicio, envío y cierre.";
                return false;
            }

            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) ||
                durationSeconds <= 0 || totalOrders <= 0 || totalAmountUsd < 0)
            {
                error = "La duración y el número de órdenes deben ser positivos. El monto debe ser mayor o igual a cero.";
                return false;
            }

            clock = new SimClock(start, deadline, end, durationSeconds, totalOrders);
            error = null;
            return true;
        }

        static DateTime ParseOrDefault(string value, DateTime fallback) => TryParse(value, out var parsed) ? parsed : fallback;

        static bool TryParse(string value, out DateTime parsed) =>
            DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
    }
}
