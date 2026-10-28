using System;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Services.IdGeneration;
using UnityEngine;
using DefendPlatformEntity = EmpireAtWar.Entities.DefendPlatform.DefendPlatform;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.Services.CaptureSites
{
    /// <summary>Lives in one player's container and registers itself so scene-wide capture sites can reach it.</summary>
    public sealed class SiteFacilityBuilder : ISiteFacilityBuilder, IInitializable, ILateDisposable
    {
        private readonly IPlayerRegistry _playerRegistry;

        private readonly EconomyModel _economyModel;
        private readonly AsteroidMiningFacilityFactory _miningFacilityFactory;
        private readonly AsteroidDefendPlatformFactory _battleAsteroidFactory;
        private readonly IUniqueIdGenerator _ids;
        private readonly SatelliteDefendPlatformFactory _satellites;

        private readonly PlayerId _owner;

        public SiteFacilityBuilder(
            IPlayerRegistry playerRegistry,
            PlayerSlot owner,
            EconomyModel economyModel,
            AsteroidMiningFacilityFactory miningFacilityFactory,
            AsteroidDefendPlatformFactory battleAsteroidFactory,
            IUniqueIdGenerator ids,
            SatelliteDefendPlatformFactory satellites)
        {
            _ids = ids;
            _satellites = satellites;
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

        public static bool IsSatellite(SiteFacilityType type) => type is SiteFacilityType.QuadLaserSatellite or
            SiteFacilityType.TurbolaserSatellite or SiteFacilityType.IonSatellite or
            SiteFacilityType.MassDriverSatellite or SiteFacilityType.MissileSatellite;

        public static DefendPlatformType SatelliteType(SiteFacilityType type) => type switch
        {
            SiteFacilityType.QuadLaserSatellite => DefendPlatformType.QuadLaserSatellite,
            SiteFacilityType.TurbolaserSatellite => DefendPlatformType.TurbolaserSatellite,
            SiteFacilityType.IonSatellite => DefendPlatformType.IonSatellite,
            SiteFacilityType.MassDriverSatellite => DefendPlatformType.MassDriverSatellite,
            SiteFacilityType.MissileSatellite => DefendPlatformType.MissileSatellite,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public long Build(SiteFacilityType facilityType, Vector3 position, Action onDestroyed)
        {
            long entityId = _ids.GenerateUniqueId();
            switch (facilityType)
            {
                case SiteFacilityType.Mining:
                    MiningFacilityEntity facility = _miningFacilityFactory.Create(
                        _owner, MiningFacilityType.AsteroidMiner, position, entityId);
                    facility.OnRelease += onDestroyed;
                    break;
                case SiteFacilityType.BattleAsteroid:
                    DefendPlatformEntity battleAsteroid = _battleAsteroidFactory.Create(
                        _owner, DefendPlatformType.BattleAsteroid, position, entityId);
                    battleAsteroid.OnRelease += onDestroyed;
                    break;
                case SiteFacilityType.QuadLaserSatellite:
                case SiteFacilityType.TurbolaserSatellite:
                case SiteFacilityType.IonSatellite:
                case SiteFacilityType.MassDriverSatellite:
                case SiteFacilityType.MissileSatellite:
                    var satellite = _satellites.Create(_owner, SatelliteType(facilityType), position, entityId);
                    satellite.OnRelease += onDestroyed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(facilityType), facilityType, null);
            }

            return entityId;
        }
    }
}
