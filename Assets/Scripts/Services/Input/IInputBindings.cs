using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Edits the live binding overrides. Nothing is persisted here: the settings screen copies
    /// <see cref="ExportOverrides"/> into the settings draft, and Apply/Discard load the result back.
    /// </summary>
    public interface IInputBindings
    {
        event Action BindingsChanged;

        /// <summary>Camera and Battle bindings the player may change; pointer axes and Escape are excluded.</summary>
        IReadOnlyList<BindingSlot> RebindableSlots { get; }

        /// <summary>Conflicting slots left by the last rebind, awaiting <see cref="ResolveConflict"/>.</summary>
        IReadOnlyList<BindingSlot> PendingConflicts { get; }

        string GetBindingDisplayString(BindingSlot slot);

        /// <summary>Waits for the next key or button with all input suspended; Escape cancels.</summary>
        void StartRebind(BindingSlot slot, Action<RebindResult> completed);

        /// <summary>Returns false when a swap would leave another conflict; the conflict then stays pending.</summary>
        bool ResolveConflict(ConflictResolution resolution);

        bool HasConflicts();

        void ResetBinding(BindingSlot slot);

        void ResetAll();

        string ExportOverrides();
    }
}
