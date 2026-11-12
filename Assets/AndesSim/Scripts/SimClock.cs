using System;

namespace AndesSim
{
    /// <summary>Maps unscaled elapsed time to the fictional night and its order count.</summary>
    public sealed class SimClock
    {
        readonly DateTime start;
        readonly long nightTicks;
        readonly long orderWindowTicks;
        readonly double durationSeconds;
        readonly int totalOrders;
        double elapsedSeconds;

        public DateTime CurrentTime { get; private set; }
        public int OrdersSent { get; private set; }
        public bool Completed => elapsedSeconds >= durationSeconds;

        internal SimClock(DateTime start, DateTime deadline, DateTime end, double durationSeconds, int totalOrders)
        {
            this.start = start;
            this.durationSeconds = durationSeconds;
            this.totalOrders = totalOrders;
            nightTicks = (end - start).Ticks;
            orderWindowTicks = (deadline - start).Ticks;
            CurrentTime = start;
        }

        public void Advance(double deltaSeconds)
        {
            if (Completed || deltaSeconds <= 0 || double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds)) return;

            elapsedSeconds = Math.Min(durationSeconds, elapsedSeconds + deltaSeconds);
            long elapsedTicks = Completed ? nightTicks : (long)(elapsedSeconds / durationSeconds * nightTicks);
            ApplyElapsedTicks(elapsedTicks);
        }

        /// <summary>Moves forwards to an exact point of the fictional night, clamped to its end.</summary>
        public void AdvanceTo(DateTime target)
        {
            if (Completed || target <= CurrentTime) return;
            long elapsedTicks = Math.Min(nightTicks, Math.Max(0, (target - start).Ticks));
            elapsedSeconds = elapsedTicks >= nightTicks
                ? durationSeconds
                : elapsedTicks / (double)nightTicks * durationSeconds;
            ApplyElapsedTicks(elapsedTicks);
        }

        void ApplyElapsedTicks(long elapsedTicks)
        {
            CurrentTime = start.AddTicks(elapsedTicks);
            OrdersSent = elapsedTicks >= orderWindowTicks
                ? totalOrders
                : (int)(elapsedTicks / (double)orderWindowTicks * totalOrders);
        }
    }
}
