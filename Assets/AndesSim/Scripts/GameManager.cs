using System;
using System.Collections.Generic;
using UnityEngine;

namespace AndesSim
{
    public enum SimMode { Ataque, Defensa }
    public enum ScenarioState { Preparacion, Ejecucion, Resultado }

    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public ScenarioData data;
        public SimMode mode = SimMode.Ataque;

        public ScenarioState State { get; private set; } = ScenarioState.Preparacion;
        public DateTime CurrentTime => clock?.CurrentTime ?? (data != null ? data.StartTime : ScenarioData.DefaultStart);
        public int OrdersSent => clock?.OrdersSent ?? 0;
        public int EvidenceCount => evidence.Count;
        public int DefenseCount => defenses.Count;
        public bool DefensesReady => mode == SimMode.Defensa && DefenseCount == DefenseControl.RequiredCount;
        public bool SessionBlocked { get; private set; }
        public bool ProtectedEventRecorded { get; private set; }
        public bool SimulationPaused { get; private set; }
        public int? AttackResultOrders { get; private set; }
        public int? DefenseResultOrders { get; private set; }

        public event Action Changed;

        readonly HashSet<string> evidence = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> defenses = new HashSet<string>(StringComparer.Ordinal);
        SimClock clock;

        void Awake()
        {
            if (I != null && I != this)
            {
                Debug.LogWarning("[ANDES-SIM] Ya existe un GameManager activo para el escenario.", this);
                enabled = false;
                return;
            }
            I = this;
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        void Update()
        {
            if (State != ScenarioState.Ejecucion || clock == null || SimulationPaused) return;

            long previousMinute = clock.CurrentTime.Ticks / TimeSpan.TicksPerMinute;
            int previousOrders = clock.OrdersSent;
            clock.Advance(Time.unscaledDeltaTime);

            if (clock.Completed)
            {
                EndScenario();
                return;
            }

            long currentMinute = clock.CurrentTime.Ticks / TimeSpan.TicksPerMinute;
            if (currentMinute != previousMinute || clock.OrdersSent != previousOrders) Changed?.Invoke();
        }

        public bool SelectMode(SimMode selectedMode)
        {
            if (State != ScenarioState.Preparacion ||
                (selectedMode != SimMode.Ataque && selectedMode != SimMode.Defensa)) return false;
            if (mode == selectedMode) return true;

            mode = selectedMode;
            ResetScenario();
            return true;
        }

        public bool StartAttack()
        {
            if (!isActiveAndEnabled || mode != SimMode.Ataque || State != ScenarioState.Preparacion) return false;
            if (data == null)
            {
                Debug.LogWarning("[ANDES-SIM] Asigna un recurso ScenarioData antes de iniciar el ataque.", this);
                return false;
            }
            if (!data.TryCreateClock(out SimClock attackClock, out string error))
            {
                Debug.LogWarning("[ANDES-SIM] " + error, this);
                return false;
            }

            clock = attackClock;
            SimulationPaused = false;
            State = ScenarioState.Ejecucion;
            Changed?.Invoke();
            return true;
        }

        public bool StartDefense()
        {
            if (!isActiveAndEnabled || State != ScenarioState.Preparacion || !DefensesReady) return false;

            State = ScenarioState.Ejecucion;
            SessionBlocked = true;
            ProtectedEventRecorded = true;
            EndScenario();
            return true;
        }

        public bool HasEvidence(string id) => !string.IsNullOrWhiteSpace(id) && evidence.Contains(id.Trim());

        public bool RegisterEvidence(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !evidence.Add(id.Trim())) return false;
            Changed?.Invoke();
            return true;
        }

        public bool HasDefense(string id) => !string.IsNullOrWhiteSpace(id) && defenses.Contains(id.Trim());

        /// <summary>Pauses the scenario clock only; exploration and training UI stay responsive.</summary>
        public void SetSimulationPaused(bool paused)
        {
            if (SimulationPaused == paused) return;
            SimulationPaused = paused;
            Changed?.Invoke();
        }

        /// <summary>
        /// Advances a guided reconstruction to an exact timestamp, including while paused.
        /// A past timestamp is rejected so a repeated explanation cannot rewind the simulation.
        /// </summary>
        public bool AdvanceGuidedTo(DateTime target)
        {
            if (State != ScenarioState.Ejecucion || clock == null || target < clock.CurrentTime) return false;
            DateTime previousTime = clock.CurrentTime;
            clock.AdvanceTo(target);
            if (clock.Completed) EndScenario();
            else if (clock.CurrentTime != previousTime) Changed?.Invoke();
            return true;
        }

        /// <summary>Clears comparative snapshots for a new training, without changing this run.</summary>
        public void ClearResultHistory()
        {
            if (!AttackResultOrders.HasValue && !DefenseResultOrders.HasValue) return;
            AttackResultOrders = null;
            DefenseResultOrders = null;
            Changed?.Invoke();
        }

        public bool ApplyDefense(string id)
        {
            string key = id?.Trim();
            if (mode != SimMode.Defensa || State != ScenarioState.Preparacion ||
                !DefenseControl.IsKnownId(key) || !defenses.Add(key)) return false;
            Changed?.Invoke();
            return true;
        }

        public void EndScenario()
        {
            if (State != ScenarioState.Ejecucion) return;
            if (SessionBlocked) DefenseResultOrders = OrdersSent;
            else if (clock != null && clock.Completed) AttackResultOrders = OrdersSent;

            State = ScenarioState.Resultado;
            Changed?.Invoke();
        }

        public void ResetScenario()
        {
            clock = null;
            evidence.Clear();
            defenses.Clear();
            SessionBlocked = false;
            ProtectedEventRecorded = false;
            SimulationPaused = false;
            State = ScenarioState.Preparacion;
            Changed?.Invoke();
        }
    }
}
