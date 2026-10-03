using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    /// <summary>Lists the button bindings and composite parts of the given maps that a player may rebind.</summary>
    public static class BindingSlotCatalog
    {
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
            string compositeName = string.Empty;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite)
                {
                    compositeName = binding.name;
                }
                else if (binding.isPartOfComposite)
                {
                    string label = $"{Humanize(action.name)} {Humanize(binding.name)} ({compositeName})";
                    slots.Add(new BindingSlot(action: action, bindingIndex: i, label: label));
                }
                // Value and pass-through actions without a composite read pointer axes, which are not rebindable.
                else if (action.type == InputActionType.Button)
                {
                    slots.Add(new BindingSlot(action: action, bindingIndex: i, label: Humanize(action.name)));
                }
            }
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
