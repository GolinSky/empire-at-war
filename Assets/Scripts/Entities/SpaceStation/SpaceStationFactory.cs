using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStationFactory:PlaceholderFactory<PlayerId, FactionType, Vector3, SpaceStation>
    {
        private readonly IAssetService _assetService;

        private readonly DiContainer _container;

        public SpaceStationFactory(IAssetService assetService, DiContainer container)
        {
            _container = container;
            _assetService = assetService;
        }
    }
}
