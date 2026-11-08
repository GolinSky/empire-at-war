using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Services.PlayerCommands
{
    public interface IShipAbilityCommandHandler
    {
        /// <summary>Authority side of a player's ability press or target pick; casters are already issuer-owned.</summary>
        void Execute(IReadOnlyList<IEntity> casters, ShipAbilityId id, IEntity target, bool cancel);
    }
}
