using System.Globalization;
using UnityEngine;

namespace AndesSim
{
    [DisallowMultipleComponent]
    public sealed class LogViewer : MonoBehaviour
    {
        public const string EvidenceId = "log_huecos";

        public string BuildLog(GameManager game)
        {
            if (game == null)
                return "Inicia la reconstrucción para ver el registro de la noche.";

            const string stamp = "dd/MM HH:mm";
            CultureInfo invariant = CultureInfo.InvariantCulture;
            if (game.ProtectedEventRecorded)
            {
                string timestamp = game.CurrentTime.ToString(stamp, invariant);
                return $"{timestamp}  [REGISTRO PROTEGIDO]\n" +
                       "Intento de acceso fuera de horario. SESIÓN BLOQUEADA.\n" +
                       "MFA activo. No se enviaron órdenes.\n" +
                       "SIEM · Evento centralizado con integridad y control de acceso.";
            }
            if (game.State == ScenarioState.Preparacion)
                return game.mode == SimMode.Defensa
                    ? "Simula el intento de acceso para consultar el evento protegido."
                    : "Inicia la reconstrucción para ver el registro de la noche.";

            ScenarioData data = game.data;
            var start = data != null ? data.StartTime : ScenarioData.DefaultStart;
            var end = data != null ? data.EndTime : ScenarioData.DefaultEnd;
            var deadline = data != null ? data.OrdersDeadline : ScenarioData.DefaultOrdersDeadline;
            var now = game.CurrentTime;

            string log = $"{start.ToString(stamp, invariant)}  001  Sesión iniciada\n" +
                         $"{start.ToString("dd/MM", invariant)} --:--  002–004  [HUECO] REGISTROS BORRADOS";
            if (now >= deadline)
                log += $"\n{deadline.ToString(stamp, invariant)}  005  Cola: {game.OrdersSent} órdenes enviadas";
            if (now >= end)
            {
                log += $"\n{end.ToString("dd/MM", invariant)} --:--  006–007  [HUECO] REGISTROS BORRADOS\n" +
                       $"{end.ToString(stamp, invariant)}  008  Sesión cerrada";
            }
            return log;
        }
    }
}
