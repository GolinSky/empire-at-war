using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>Launches bought squadrons from the hangar of the owner's space station.</summary>
    public sealed class SquadronLauncher : ISquadronLauncher
    {
        private readonly IEntityLocator _entityLocator;

        public SquadronLauncher(IEntityLocator entityLocator)
        {
            _entityLocator = entityLocator;
        }

        public ISquadron LaunchFromStation(PlayerId owner, SquadronType squadronType)
        {
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.Owner == owner &&
                    entity.Model is ISpaceStationModelObserver &&
                    !entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IHangarCommand hangar))
                {
                    return hangar.Launch(squadronType);
                }
            }

            throw new InvalidOperationException(
                $"{owner} has no operational space station with a hangar to launch {squadronType}.");
        }
    }
}
