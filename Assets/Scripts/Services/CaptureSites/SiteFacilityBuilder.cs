using System;
using Zenject;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Economy;
using UnityEngine;
using DefendPlatformEntity = EmpireAtWar.Entities.DefendPlatform.DefendPlatform;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.Services.CaptureSites
{
    /// <summary>Lives in one player's container and registers itself so scene-wide capture sites can reach it.</summary>
    public sealed class SiteFacilityBuilder : ISiteFacilityBuilder, IInitializable, ILateDisposable
    {
        private readonly PlayerId _owner;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly EconomyModel _economyModel;
        private readonly AsteroidMiningFacilityFactory _miningFacilityFactory;
        private readonly AsteroidDefendPlatformFactory _battleAsteroidFactory;

        public SiteFacilityBuilder(
            PlayerSlot owner,
            IPlayerRegistry playerRegistry,
            EconomyModel economyModel,
            AsteroidMiningFacilityFactory miningFacilityFactory,
            AsteroidDefendPlatformFactory battleAsteroidFactory)
        {
            _owner = owner.Id;
            _playerRegistry = playerRegistry;
            _economyModel = economyModel;
            _miningFacilityFactory = miningFacilityFactory;
            _battleAsteroidFactory = battleAsteroidFactory;
        }

        public void Initialize()
        {
            _playerRegistry.RegisterSiteBuilder(_owner, this);
        }

        public void LateDispose()
        {
            _playerRegistry.UnregisterSiteBuilder(_owner);
        }

        public bool CanAfford(float price)
        {
            return _economyModel.Money >= price;
        }

        public bool TrySpend(float price)
        {
            return _economyModel.TrySpend(price);
        }

        public void Build(SiteFacilityType facilityType, Vector3 position, Action onDestroyed)
        {
            switch (facilityType)
            {
                case SiteFacilityType.Mining:
                    MiningFacilityEntity facility = _miningFacilityFactory.Create(
                        _owner, MiningFacilityType.AsteroidMiner, position);
                    facility.OnRelease += onDestroyed;
                    break;
                case SiteFacilityType.BattleAsteroid:
                    DefendPlatformEntity battleAsteroid = _battleAsteroidFactory.Create(
                        _owner, DefendPlatformType.BattleAsteroid, position);
                    battleAsteroid.OnRelease += onDestroyed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(facilityType), facilityType, null);
            }
        }
    }
}
