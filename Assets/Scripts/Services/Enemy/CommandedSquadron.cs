using System;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>A squadron under <see cref="EnemySquadronCommander"/> orders: its combat rating and release hook.</summary>
    public readonly struct CommandedSquadron
    {
        public UnitCombatProfile Profile { get; }
        public Action ReleaseHandler { get; }

        public CommandedSquadron(UnitCombatProfile profile, Action releaseHandler)
        {
            Profile = profile;
            ReleaseHandler = releaseHandler;
        }
    }
}
