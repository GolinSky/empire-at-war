using System;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;

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

        public ISquadron LaunchFromStation(PlayerType playerType, SquadronType squadronType)
        {
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.PlayerType == playerType &&
                    entity.Model is ISpaceStationModelObserver &&
                    !entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IHangarCommand hangar))
                {
                    return hangar.Launch(squadronType);
                }
            }

            throw new InvalidOperationException(
                $"{playerType} has no operational space station with a hangar to launch {squadronType}.");
        }
    }
}
