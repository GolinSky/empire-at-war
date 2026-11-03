using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class ReinforcementDeployInput : IReinforcementDeployInput
    {
        // Gamepad-only players have no keyboard and simply never batch-deploy.
        public bool IsDeployAllHeld => Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
    }
}
