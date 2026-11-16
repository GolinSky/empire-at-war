using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>
    /// Configuration and caster requirements owned by one ability feature.
    /// Each subclass creates its effect with the settings passed to its constructor.
    /// </summary>
    [Serializable]
    public abstract class ShipAbilitySettings
    {
        internal virtual HardPointType RequiredSubsystem => HardPointType.Any;
        internal virtual bool CanActivateWhileCloaked => false;

        internal bool HasRequiredSubsystem(IHealthModelObserver health)
        {
            HardPointType type = RequiredSubsystem;
            if (type == HardPointType.Any) return true;

            var points = health.HardPointModels;
            for (int i = 0; i < points.Count; i++)
            {
                IReadOnlyHardPointModel point = points[i];
                if (point.HardPointType == type && !point.IsDestroyed) return true;
            }
            return false;
        }

        public virtual float GetEffectDuration(float activeDuration) => activeDuration;

        public abstract IShipAbility CreateAbility(ShipAbilityDependencies dependencies);
    }
}
