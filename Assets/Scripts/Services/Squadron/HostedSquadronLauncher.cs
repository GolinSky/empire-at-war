using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Stations;
using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    public sealed class HostedSquadronLauncher : ISquadronLauncher
    {
        private readonly IProductionAccess _production;
        private readonly ISquadronSpawner _spawner;

        public HostedSquadronLauncher(IProductionAccess production, ISquadronSpawner spawner)
        {
            _production = production;
            _spawner = spawner;
        }

        public bool TryLaunchFromStation(PlayerId owner, SquadronType squadronType, out ISquadron squadron)
        {
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
