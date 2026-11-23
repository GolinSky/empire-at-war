using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    /// <summary>
    /// What a ship knows about who threatens it: recent damage, attackers of itself or a team station, and the
    /// hostiles its team can see. Ship states rank their main target with it.
    /// </summary>
    public interface IShipThreatSense
    {
        /// <summary>Hull or shields dropped within <see cref="ShipThreatSense.RECENT_DAMAGE_SECONDS"/>.</summary>
        bool WasDamagedRecently { get; }

        /// <summary>The hostile tracks this ship and this ship is within the hostile's weapon range.</summary>
        bool IsAttackingSelf(IEntity hostile);

        /// <summary>The hostile's attack order targets a living station of this ship's team.</summary>
        bool IsAttackingTeamStation(IEntity hostile);

        /// <summary>Appends hostiles the team can see within <paramref name="radius"/> of the point.</summary>
        void FillTeamVisibleHostiles(Vector3 center, float radius, List<IEntity> results);
    }
}
