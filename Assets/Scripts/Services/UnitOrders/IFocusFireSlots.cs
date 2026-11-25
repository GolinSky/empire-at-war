using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>
    /// Focus fire cap: a target accepts a limited number of main attackers per team (allies share one pool). Ships and
    /// squadrons hold separate pools. Attack-move, stations, abilities and superweapons claim nothing.
    /// </summary>
    public interface IFocusFireSlots
    {
        /// <summary>A target's holder count changed (claim or release).</summary>
        event Action<IEntity> Changed;

        /// <summary>Slots <paramref name="target"/> offers one team's ships or squadrons; 0 = unlimited.</summary>
        int GetCap(IEntity target, bool isSquadron);

        /// <summary>Living attackers of <paramref name="team"/> and its allies holding <paramref name="target"/>.</summary>
        int CountHolders(PlayerId team, IEntity target, bool isSquadron);

        /// <summary>Slots left for <paramref name="team"/>; <see cref="int.MaxValue"/> when unlimited.</summary>
        int FreeSlots(PlayerId team, IEntity target, bool isSquadron);

        bool Holds(IEntity attacker, IEntity target);

        /// <summary>The attacker holds the target already or a slot is free for it.</summary>
        bool HasSlotFor(IEntity attacker, IEntity target);

        /// <summary>Moves the attacker's claim to <paramref name="target"/>; false, with no claim left, when it is full.</summary>
        bool TryClaim(IEntity attacker, IEntity target);

        void Release(long attackerId);
    }
}
