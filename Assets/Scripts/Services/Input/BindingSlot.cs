using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    /// <summary>One rebindable binding: a plain binding or a single part of a composite.</summary>
    public readonly struct BindingSlot
    {
        public InputAction Action { get; }
        public int BindingIndex { get; }
        public string Label { get; }

        public BindingSlot(InputAction action, string label, int bindingIndex)
        {
            Action = action;
            BindingIndex = bindingIndex;
            Label = label;
        }

        public bool Matches(BindingSlot other)
        {
            return Action == other.Action && BindingIndex == other.BindingIndex;
        }
    }
}
