using System;

namespace EmpireAtWar.Entities.ShipUi
{
    /// <summary>Reports that the player pressed the auto-power toggle; read-only.</summary>
    public interface IAutoPowerToggleObserver
    {
        event Action AutoPowerToggled;
    }
}
