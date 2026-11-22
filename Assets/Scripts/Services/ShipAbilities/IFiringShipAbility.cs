using System;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>
    /// An ability whose fire moment the authority decides. Replicas never fire by themselves; they play the
    /// authority's <see cref="Fired"/> through <see cref="FireReplica"/>.
    /// </summary>
    public interface IFiringShipAbility : IShipAbility
    {
        event Action<ShipAbilityFire> Fired;
        void FireReplica(ShipAbilityFire fire);
    }
}
