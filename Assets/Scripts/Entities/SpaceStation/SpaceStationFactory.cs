using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStationFactory:PlaceholderFactory<PlayerId, FactionType, Vector3, SpaceStation>
    {
        private readonly DiContainer _container;
        private readonly IAssetService _assetService;

        public SpaceStationFactory(DiContainer container, IAssetService assetService)
        {
            _container = container;
            _assetService = assetService;
        }
    }
}
