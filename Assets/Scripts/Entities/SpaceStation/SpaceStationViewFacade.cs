using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStationViewFacade:PlaceholderFactory<PlayerType, FactionType, Vector3, SpaceStationView>
    {
        private readonly DiContainer _container;
        private readonly IRepository _repository;

        public SpaceStationViewFacade(DiContainer container, IRepository repository)
        {
            _container = container;
            _repository = repository;
        }
    }
}