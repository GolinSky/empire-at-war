using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Stations;
using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>
    /// Launches from the owner's own station hangar; without one, from the team base it produces at
    /// (a hosted AI's host, or an ally after the owner's station fell).
    /// </summary>
    public sealed class TeamBaseSquadronLauncher : ISquadronLauncher
    {
        private readonly SquadronLauncher _ownStation;
        private readonly IProductionAccess _production;
        private readonly ISquadronSpawner _spawner;

        public TeamBaseSquadronLauncher(SquadronLauncher ownStation, IProductionAccess production,
            ISquadronSpawner spawner)
        {
            _ownStation = ownStation;
            _production = production;
            _spawner = spawner;
        }

        public bool TryLaunchFromStation(PlayerId owner, SquadronType squadronType, out ISquadron squadron)
        {
            if (_ownStation.TryLaunchFromStation(owner, squadronType, out squadron)) return true;

            if (!_production.TryGetProductionBase(out IEntity productionBase))
            {
                squadron = null;
                return false;
            }

            Transform origin = productionBase.GetFacade<IEntityTransformFacade>().Transform;
            squadron = _spawner.Spawn(owner, squadronType, origin.position, origin.rotation);
            return true;
        }
    }
}
