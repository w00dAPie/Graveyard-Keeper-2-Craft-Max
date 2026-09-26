using System;

namespace GK2CraftMax.Helpers
{
    internal static class CraftQuantityStep
    {
        internal static int GetDelta(int current, int minimum, int maximum, bool increase)
        {
            // A reduced resource limit must never make RB decrease an existing selection.
            // Likewise, LB must remain usable when the selection exceeds available resources.
            if (increase)
                return (int)Math.Max(0L, Math.Min(10L, (long)maximum - current));

            return -(int)Math.Max(0L, Math.Min(10L, (long)current - minimum));
        }

        internal static int LimitByResource(
            int maximum,
            long available,
            long required,
            long reserved,
            int committedCount
        )
        {
            if (required <= 0)
                return maximum;

            long affordable = Math.Max(0L, available - reserved) / required;
            return (int)Math.Min(maximum, affordable + committedCount);
        }
    }
}
