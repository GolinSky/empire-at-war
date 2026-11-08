using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>
    /// Online: an armed entity's target changes on the host, replayed on clients; and the host's AI takeover
    /// moving targets to rebuilt entities. Not used by gameplay.
    /// </summary>
    public interface IWeaponTargetReplicaFacade : IEntityFacade
    {
        event Action<AttackData, AttackType> TargetAdded;
        event Action TargetReset;

        /// <param name="hardPointId">A focused hardpoint of the target, or -1 for <paramref name="hardPointType"/>.</param>
        void AddTarget(IEntity target, AttackType attackType, int hardPointId, HardPointType hardPointType);
        void ResetTarget();

        /// <summary>Host: <paramref name="rebuilt"/> replaced an entity with its id; targets on the old one move to it.</summary>
        void RetargetRebuilt(IEntity rebuilt);
    }
}
