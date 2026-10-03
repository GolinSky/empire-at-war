using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Two slots conflict when the same key completes the same chord: Ctrl+A and A do not conflict,
    /// A in Camera and A in Battle do, because both maps are active together.
    /// </summary>
    public static class BindingConflicts
    {
        private const string ONE_MODIFIER_COMPOSITE = "OneModifier";
        private const string TWO_MODIFIERS_COMPOSITE = "TwoModifiers";

        public static List<BindingSlot> Find(BindingSlot slot, IReadOnlyList<BindingSlot> slots)
        {
            List<BindingSlot> conflicts = new List<BindingSlot>();
            string path = EffectivePath(slot);
            if (path.Length == 0)
            {
                return conflicts;
            }

            string chord = ChordOf(slot);
            foreach (BindingSlot other in slots)
            {
                if (!other.Matches(slot) && EffectivePath(other) == path && ChordOf(other) == chord)
                {
                    conflicts.Add(other);
                }
            }

            return conflicts;
        }

        private static string EffectivePath(BindingSlot slot)
        {
            string path = slot.Action.bindings[slot.BindingIndex].effectivePath;
            return string.IsNullOrEmpty(path) ? string.Empty : path.ToLowerInvariant();
        }

        private static string ChordOf(BindingSlot slot)
        {
            IReadOnlyList<InputBinding> bindings = slot.Action.bindings;
            if (!bindings[slot.BindingIndex].isPartOfComposite)
            {
                return EffectivePath(slot);
            }

            int head = slot.BindingIndex;
            while (!bindings[head].isComposite)
            {
                head--;
            }

            string composite = bindings[head].GetNameOfComposite();
            if (composite != ONE_MODIFIER_COMPOSITE && composite != TWO_MODIFIERS_COMPOSITE)
            {
                return EffectivePath(slot);
            }

            List<string> parts = new List<string>();
            for (int i = head + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++)
            {
                parts.Add(EffectivePath(new BindingSlot(action: slot.Action, bindingIndex: i, label: string.Empty)));
            }

            parts.Sort(StringComparer.Ordinal);
            return string.Join("+", parts);
        }
    }
}
