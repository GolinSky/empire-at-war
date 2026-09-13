using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStationFacade:PlaceholderFactory<PlayerType, FactionType, Vector3, SpaceStation>
    {
        private readonly DiContainer _container;
        private readonly IAssetService _repository;

        public SpaceStationFacade(DiContainer container, IAssetService repository)
        {
            _container = container;
            _repository = repository;
        }
    }
}
