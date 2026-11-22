using EmpireAtWar.Services.ShipAbilities;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → clients, reliable: a running ability fired; clients play this shot instead of deciding their own.</summary>
    public struct ShipAbilityFireMessage : NetworkMessage
    {
        public long CasterId;
        public ShipAbilityId Ability;
        public Vector3 Origin;
        public Vector3 Direction;
        public float Rise;
        public float Range;

        public ShipAbilityFire Fire => new ShipAbilityFire(Origin, Direction, Rise, Range);
    }
}
