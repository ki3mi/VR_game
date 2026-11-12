using UnityEngine;

namespace AndesSim
{
    [DisallowMultipleComponent]
    public sealed class DefenseControl : MonoBehaviour
    {
        public const string MfaId = "mfa";
        public const string ScheduleId = "fuera_horario";
        public const string SiemId = "siem";
        public const int RequiredCount = 3;

        public string defenseId;
        public Renderer indicator;

        public string Title => defenseId switch
        {
            MfaId => "MFA y cuentas privilegiadas",
            ScheduleId => "Alerta fuera de horario",
            SiemId => "SIEM y registros protegidos",
            _ => "Control de defensa"
        };

        public string Description => defenseId switch
        {
            MfaId => "Exige un segundo factor y protege las cuentas privilegiadas.",
            ScheduleId => "Detecta el acceso nocturno, activa la alarma y bloquea la sesión.",
            SiemId => "Centraliza el evento y lo protege con controles de acceso e integridad.",
            _ => "Selecciona un control del panel de seguridad."
        };

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        GameManager boundGame;
        MaterialPropertyBlock original;
        MaterialPropertyBlock active;
        bool indicatorActive;

        internal static bool IsKnownId(string id) => id == MfaId || id == ScheduleId || id == SiemId;

        public bool Activate(GameManager game)
        {
            if (game == null) return false;
            if (boundGame != game)
            {
                if (boundGame != null) boundGame.Changed -= OnGameChanged;
                boundGame = game;
                if (isActiveAndEnabled) boundGame.Changed += OnGameChanged;
            }
            bool applied = game.ApplyDefense(defenseId);
            Refresh(game);
            return applied;
        }

        public void Refresh(GameManager game)
        {
            bool isActive = game != null && game.HasDefense(defenseId);
            if (indicator == null || isActive == indicatorActive) return;

            if (isActive)
            {
                original ??= new MaterialPropertyBlock();
                active ??= new MaterialPropertyBlock();
                indicator.GetPropertyBlock(original);
                indicator.GetPropertyBlock(active);
                active.SetColor(BaseColor, new Color(0.16f, 0.84f, 0.36f));
                active.SetColor(EmissionColor, new Color(0.08f, 1.5f, 0.34f));
                indicator.SetPropertyBlock(active);
            }
            else indicator.SetPropertyBlock(original);

            indicatorActive = isActive;
        }

        void OnEnable()
        {
            if (boundGame != null) boundGame.Changed += OnGameChanged;
            Refresh(boundGame);
        }

        void OnDisable()
        {
            if (boundGame != null) boundGame.Changed -= OnGameChanged;
            Refresh(null);
        }

        void OnGameChanged() => Refresh(boundGame);
    }
}
