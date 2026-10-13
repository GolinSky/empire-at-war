using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// One rebindable binding: a plain binding, a single part of a non-modifier composite,
    /// or a whole modifier chord such as Ctrl+A, addressed by its composite binding.
    /// </summary>
    public readonly struct BindingSlot
    {
        public InputAction Action { get; }
        public int BindingIndex { get; }
        public string Label { get; }

        /// <summary>Modifier keys the chord requires; 0 for a slot holding a single key.</summary>
        public int ModifierCount { get; }

        public bool IsChord => ModifierCount > 0;

        /// <summary>First binding whose path this slot owns: the first chord part, or the binding itself.</summary>
        public int FirstPartIndex => IsChord ? BindingIndex + 1 : BindingIndex;

        /// <summary>Chord parts are the modifiers followed by the key; other slots own one binding.</summary>
        public int PartCount => ModifierCount + 1;

        /// <summary>The binding that receives the pressed key during interactive rebinding.</summary>
        public int KeyPartIndex => FirstPartIndex + ModifierCount;

        public BindingSlot(InputAction action, string label, int bindingIndex, int modifierCount = 0)
        {
            Action = action;
            BindingIndex = bindingIndex;
            Label = label;
            ModifierCount = modifierCount;
        }

        public bool Matches(BindingSlot other)
        {
            return Action == other.Action && BindingIndex == other.BindingIndex;
        }
    }
}
