namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>Why one automatic ability check ended the way it did; written to the auto-power telemetry.</summary>
    public enum AutoPowerReason
    {
        Activated,
        RuleActivated,
        StopRuleCancelled,
        ShipToggleOff,
        ManualHold,
        AbilitiesRunning,
        NoAutoPowerRoles,
        NothingWanted,
        WantedNotReady,
        AwaitingConfirmation,
        RecheckChanged,
        ActivationRefused
    }
}
