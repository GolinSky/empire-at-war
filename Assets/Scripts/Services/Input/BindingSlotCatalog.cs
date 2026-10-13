using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Lists the button bindings and composite parts of the given maps that a player may rebind.
    /// A modifier composite (Ctrl+A) is one slot so the whole chord is shown and captured together.
    /// </summary>
    public static class BindingSlotCatalog
    {
        private const string ONE_MODIFIER_COMPOSITE = "OneModifier";
        private const string TWO_MODIFIERS_COMPOSITE = "TwoModifiers";
        private const string ALTERNATE_SUFFIX = " (Alt)";

        public static List<BindingSlot> Build(params InputActionMap[] maps)
        {
            List<BindingSlot> slots = new List<BindingSlot>();
            foreach (InputActionMap map in maps)
            {
                foreach (InputAction action in map.actions)
                {
                    AddSlots(action, slots);
                }
            }

            return slots;
        }

        private static void AddSlots(InputAction action, List<BindingSlot> slots)
        {
            string actionLabel = Humanize(action.name);
            int compositeCount = 0;
            string compositeSuffix = string.Empty;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite)
                {
                    int partCount = CountParts(action.bindings, i);
                    if (IsModifierComposite(binding))
                    {
                        slots.Add(new BindingSlot(action: action, label: actionLabel, bindingIndex: i, modifierCount: partCount - 1));
                        i += partCount;
                        continue;
                    }

                    // Composite names such as "WASD" describe default keys and go stale once rebound.
                    compositeSuffix = compositeCount switch
                    {
                        0 => string.Empty,
                        1 => ALTERNATE_SUFFIX,
                        _ => $" (Alt {compositeCount})",
                    };
                    compositeCount++;
                }
                else if (binding.isPartOfComposite)
                {
                    string label = $"{actionLabel} {Humanize(binding.name)}{compositeSuffix}";
                    slots.Add(new BindingSlot(action: action, label: label, bindingIndex: i));
                }
                // Value and pass-through actions without a composite read pointer axes, which are not rebindable.
                else if (action.type == InputActionType.Button)
                {
                    slots.Add(new BindingSlot(action: action, label: actionLabel, bindingIndex: i));
                }
            }
        }

        private static bool IsModifierComposite(InputBinding composite)
        {
            string name = composite.GetNameOfComposite();
            return name == ONE_MODIFIER_COMPOSITE || name == TWO_MODIFIERS_COMPOSITE;
        }

        private static int CountParts(IReadOnlyList<InputBinding> bindings, int compositeIndex)
        {
            int count = 0;
            for (int i = compositeIndex + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++)
            {
                count++;
            }

            return count;
        }

        private static string Humanize(string name)
        {
            StringBuilder builder = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char character = name[i];
                if (i == 0)
                {
                    builder.Append(char.ToUpperInvariant(character));
                    continue;
                }

                if (char.IsUpper(character))
                {
                    builder.Append(' ');
                }

                builder.Append(character);
            }

            return builder.ToString();
        }
    }
}
