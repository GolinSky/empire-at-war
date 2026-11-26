using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Units.Matchups
{
    /// <summary>
    /// One direction of designer knowledge: <see cref="Attacker"/> units beat <see cref="Target"/> units. The reverse
    /// ("target is weak against attacker") is read from the same rule, so it is never written twice.
    /// </summary>
    [Serializable]
    public struct MatchupRule
    {
        [SerializeField] private MatchupSide attacker;
        [SerializeField] private MatchupSide target;

        public MatchupSide Attacker => attacker;
        public MatchupSide Target => target;

        public MatchupRule(MatchupSide attacker, MatchupSide target)
        {
            this.attacker = attacker;
            this.target = target;
        }
    }
}
