using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class ReinforcementDeployInput : IReinforcementDeployInput
    {
        // Gamepad-only players have no keyboard and simply never batch-deploy or turn the preview.
        public bool IsDeployAllHeld => Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

        public bool IsAimHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;

        public int TurnDirection => Keyboard.current == null
            ? 0
            : (Keyboard.current.eKey.isPressed ? 1 : 0) - (Keyboard.current.qKey.isPressed ? 1 : 0);

        public bool WasTurnPressed => Keyboard.current != null &&
                                      (Keyboard.current.qKey.wasPressedThisFrame ||
                                       Keyboard.current.eKey.wasPressedThisFrame);

        public bool WasFacingResetPressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
    }
}
