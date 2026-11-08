using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>Activate (optionally at a target) or cancel one ability on the issuer's ships.</summary>
    public readonly struct ShipAbilityCommand
    {
        public PlayerId Issuer { get; }
        public ShipAbilityId AbilityId { get; }
        public long[] CasterIds { get; }
        public long TargetId { get; }
        public bool Cancel { get; }

        public ShipAbilityCommand(PlayerId issuer, ShipAbilityId abilityId, long[] casterIds, long targetId, bool cancel)
        {
            Issuer = issuer;
            AbilityId = abilityId;
            CasterIds = casterIds;
            TargetId = targetId;
            Cancel = cancel;
        }

        public static ShipAbilityCommand Create(PlayerId issuer, ShipAbilityId abilityId, IReadOnlyList<IEntity> casters,
            IEntity target, bool cancel)
        {
            long[] ids = new long[casters.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = casters[i].Id;
            return new ShipAbilityCommand(issuer, abilityId, ids, target == null ? UnitOrderCommand.NO_TARGET : target.Id, cancel);
        }
    }
}
