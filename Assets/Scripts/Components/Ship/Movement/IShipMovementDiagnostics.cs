namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>Movement internals that order telemetry records to explain a ship that reports moving but does not travel.</summary>
    public interface IShipMovementDiagnostics
    {
        /// <summary>Semicolon separated: phase;queuedPlan;inPlanQueue;braking;settling;failedDestination;destX;destZ;speed.</summary>
        string MovementDiagnostics { get; }

        /// <summary>The last hull-spacing settle decision since the previous call, or null.</summary>
        string TakeSettleEvent();
    }
}
