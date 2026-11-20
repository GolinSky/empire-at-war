namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementDeployInput
    {
        /// <summary>Held when a reinforcement drag starts: deploy every unit of that kind that fits.</summary>
        bool IsDeployAllHeld { get; }

        /// <summary>Held to pin the preview and turn it toward the cursor.</summary>
        bool IsAimHeld { get; }

        /// <summary>-1 while turning left, +1 while turning right, 0 otherwise.</summary>
        int TurnDirection { get; }

        /// <summary>True on the frame a turn key goes down.</summary>
        bool WasTurnPressed { get; }

        /// <summary>True on the frame the player asks to restore automatic facing.</summary>
        bool WasFacingResetPressed { get; }
    }
}
