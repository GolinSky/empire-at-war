using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStationFacade:PlaceholderFactory<PlayerType, FactionType, Vector3, SpaceStation>
    {
        private readonly DiContainer _container;
        private readonly IRepository _repository;

        public SpaceStationFacade(DiContainer container, IRepository repository)
        {
            _container = container;
            _repository = repository;
        }
    }
}
