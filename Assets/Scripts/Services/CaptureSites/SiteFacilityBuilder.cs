using System;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.Services.CaptureSites
{
    public sealed class SiteFacilityBuilder : ISiteFacilityBuilder
    {
        private readonly PlayerType _playerType;
        private readonly EconomyModel _economyModel;
        private readonly AsteroidMiningFacilityFactory _miningFacilityFactory;

        public SiteFacilityBuilder(
            PlayerType playerType,
            EconomyModel economyModel,
            AsteroidMiningFacilityFactory miningFacilityFactory)
        {
            _playerType = playerType;
            _economyModel = economyModel;
            _miningFacilityFactory = miningFacilityFactory;
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
                        _playerType, MiningFacilityType.AsteroidMiner, position);
                    facility.OnRelease += onDestroyed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(facilityType), facilityType, null);
            }
        }
    }
}
