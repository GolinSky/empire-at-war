using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Two slots conflict when they need exactly the same keys: Ctrl+A and A do not conflict,
    /// A in Camera and A in Battle do, because both maps are active together.
    /// </summary>
    public static class BindingConflicts
    {
        public static List<BindingSlot> Find(BindingSlot slot, IReadOnlyList<BindingSlot> slots)
        {
            List<BindingSlot> conflicts = new List<BindingSlot>();
            string chord = ChordOf(slot);
            if (chord.Length == 0)
            {
                return conflicts;
            }

            foreach (BindingSlot other in slots)
            {
                if (!other.Matches(slot) && ChordOf(other) == chord)
                {
                    conflicts.Add(other);
                }
            }

            return conflicts;
        }

        /// <summary>Sorted lower-case paths of every key the slot needs; empty when any of them is unbound.</summary>
        private static string ChordOf(BindingSlot slot)
        {
            List<string> parts = new List<string>(slot.PartCount);
            for (int i = slot.FirstPartIndex; i < slot.FirstPartIndex + slot.PartCount; i++)
            {
                string path = slot.Action.bindings[i].effectivePath;
                if (string.IsNullOrEmpty(path))
                {
                    return string.Empty;
                }

                parts.Add(path.ToLowerInvariant());
            }

            parts.Sort(StringComparer.Ordinal);
            return string.Join("+", parts);
        }
    }
}
