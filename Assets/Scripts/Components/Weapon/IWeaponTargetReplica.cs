using System;
using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>
    /// Online: the host reports every target change of a weapon (AI, radar and orders alike); a client weapon
    /// ignores its own choices and takes the host's through the replica methods.
    /// </summary>
    public interface IWeaponTargetReplica
    {
        event Action<AttackData, AttackType> TargetAdded;
        event Action TargetReset;

        void AddReplicaTarget(AttackData attackData, AttackType attackType);
        void ResetReplicaTarget();

        /// <summary>
        /// Host AI takeover: drops every target group of this entity (its old life); true when any was tracked.
        /// <paramref name="wasMain"/> tells whether it was the main target.
        /// </summary>
        bool RemoveTargetsOf(long entityId, out bool wasMain);
    }
}
