using System;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    public static class ControlGroupBindings
    {
        public const int SLOT_COUNT = 10;
        public const string ACTION_PREFIX = "ControlGroup";

        public static bool IsFixedAction(InputAction action)
        {
            return action.actionMap.name == "Battle" && action.name.StartsWith(ACTION_PREFIX, StringComparison.Ordinal);
        }

        public static bool IsReserved(BindingSlot slot)
        {
            string path = slot.Action.bindings[slot.KeyPartIndex].effectivePath;
            for (int digit = 0; digit < SLOT_COUNT; digit++)
            {
                if (string.Equals(path, $"<Keyboard>/{digit}", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(path, $"<Keyboard>/digit{digit}", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
