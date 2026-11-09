using System;
using System.Collections.Generic;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>Turns one blocked/clear flag per degree of yaw into clear firing arcs.</summary>
    public static class HardPointArcCalculator
    {
        public const int RAY_COUNT = YawArc.FULL_CIRCLE;
        public const int SECOND_ARC_MIN_DEGREES = 30;
        private const int NOISE_DEGREES = 2;
        private const int YAW_OFFSET = 180;

        /// <summary>Yaw of ray <paramref name="index"/>; ray 0 points aft (-180).</summary>
        public static float YawOf(int index) => index - YAW_OFFSET;

        /// <summary>Clear spans, widest first. Blocked runs of up to 2° count as clear.</summary>
        public static List<YawArc> FindClearArcs(IReadOnlyList<bool> blocked)
        {
            bool[] clear = new bool[RAY_COUNT];
            for (int index = 0; index < RAY_COUNT; index++) clear[index] = !blocked[index];
            IgnoreNoise(clear);

            List<YawArc> arcs = new List<YawArc>();
            int firstBlocked = Array.IndexOf(clear, false);
            if (firstBlocked < 0)
            {
                arcs.Add(new YawArc(-YAW_OFFSET, YAW_OFFSET, RAY_COUNT, false));
                return arcs;
            }

            int offset = 0;
            while (offset < RAY_COUNT)
            {
                int index = (firstBlocked + offset) % RAY_COUNT;
                if (!clear[index])
                {
                    offset++;
                    continue;
                }
                int length = RunLength(clear, index, true);
                arcs.Add(ToArc(index, length));
                offset += length;
            }
            arcs.Sort((left, right) => right.Width.CompareTo(left.Width));
            return arcs;
        }

        private static void IgnoreNoise(bool[] clear)
        {
            int firstClear = Array.IndexOf(clear, true);
            if (firstClear < 0) return;
            int offset = 0;
            while (offset < RAY_COUNT)
            {
                int index = (firstClear + offset) % RAY_COUNT;
                if (clear[index])
                {
                    offset++;
                    continue;
                }
                int length = RunLength(clear, index, false);
                if (length <= NOISE_DEGREES)
                {
                    for (int step = 0; step < length; step++) clear[(index + step) % RAY_COUNT] = true;
                }
                offset += length;
            }
        }

        private static int RunLength(bool[] values, int start, bool value)
        {
            int length = 0;
            while (length < RAY_COUNT && values[(start + length) % RAY_COUNT] == value) length++;
            return length;
        }

        private static YawArc ToArc(int start, int length)
        {
            int min = start - YAW_OFFSET;
            int max = min + length;
            bool crossesRear = max > YAW_OFFSET;
            if (crossesRear) max -= RAY_COUNT;
            return new YawArc(min, max, length, crossesRear);
        }
    }
}
