using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>A squadron under <see cref="EnemySquadronCommander"/> orders: its combat rating and release hook.</summary>
    public readonly struct CommandedSquadron
    {
        public UnitCombatProfile Profile { get; }
        public Action ReleaseHandler { get; }

        /// <summary>Launched from a ship hangar rather than bought at the station: it escorts its carrier while guarding.</summary>
        public bool IsHangarLaunched { get; }

        /// <summary>Order state of a hangar-launched squadron; unused for station purchases.</summary>
        public IUnitOrderObserverFacade Orders { get; }

        public CommandedSquadron(UnitCombatProfile profile, Action releaseHandler)
        {
            Profile = profile;
            ReleaseHandler = releaseHandler;
            IsHangarLaunched = false;
            Orders = null;
        }

        public CommandedSquadron(UnitCombatProfile profile, Action releaseHandler, IUnitOrderObserverFacade orders)
        {
            Profile = profile;
            ReleaseHandler = releaseHandler;
            IsHangarLaunched = true;
            Orders = orders;
        }
    }
}
