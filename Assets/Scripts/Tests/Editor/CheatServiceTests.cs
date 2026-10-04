using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Ship;
using EmpireAtWar.Services.Stations;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CheatServiceTests
    {
        private EconomyData _economyData;
        private ReinforcementData _reinforcementData;
        private EconomyModel _economyModel;
        private ReinforcementModel _reinforcementModel;
        private SuperWeaponModel _superWeaponModel;
        private CheatService _cheatService;

        [SetUp]
        public void SetUp()
        {
            _economyData = ScriptableObject.CreateInstance<EconomyData>();
            _reinforcementData = ScriptableObject.CreateInstance<ReinforcementData>();
            _economyModel = new EconomyModel(_economyData, 100f);
            _reinforcementModel = new ReinforcementModel(_reinforcementData);
            _superWeaponModel = new SuperWeaponModel();
            _cheatService = new CheatService(
                economyModel: _economyModel,
                reinforcementModel: _reinforcementModel,
                shipFactory: new ShipFactory(),
                shipSpawnPoints: new NoShipSpawnPoints(),
                entityLocator: new EmptyEntityLocator(),
                stationRegistry: new OperationalStationRegistry(),
                superWeaponModel: _superWeaponModel,
                rangeDebugModel: new RangeDebugModel(),
                owner: TestPlayers.CreateDuel().Get(TestPlayers.Human));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_economyData);
            UnityEngine.Object.DestroyImmediate(_reinforcementData);
        }

        [Test]
        public void AddMoney_PositiveAmount_UpdatesEconomy()
        {
            _cheatService.AddMoney(250f);

            Assert.That(_economyModel.Money, Is.EqualTo(350f));
        }

        [Test]
        public void AddMoney_NonPositiveAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _cheatService.AddMoney(0f));
        }

        [Test]
        public void AddShipReinforcement_NotifiesReinforcementModel()
        {
            FactionData factionData = new FactionData();
            ShipUnitRequest request = new ShipUnitRequest(factionData, ShipType.Venator);
            UnitRequest addedRequest = null;
            _reinforcementModel.OnReinforcementAdded += added => addedRequest = added;

            _cheatService.AddShipReinforcement(request);

            Assert.That(addedRequest, Is.SameAs(request));
            Assert.That(addedRequest.FactionData, Is.SameAs(factionData));
        }

        [Test]
        public void ForceSpawnShipAtDefaultZone_WithoutOwnedZone_ReturnsFalse()
        {
            ShipUnitRequest request = new ShipUnitRequest(new FactionData(), ShipType.Venator);

            bool spawned = _cheatService.ForceSpawnShipAtDefaultZone(request);

            Assert.That(spawned, Is.False);
        }

        [Test]
        public void GrantSuperWeapon_MakesWeaponReady()
        {
            bool granted = _cheatService.GrantSuperWeapon(SuperWeaponType.IonCannon);

            Assert.That(granted, Is.True);
            Assert.That(_superWeaponModel.GetState(SuperWeaponType.IonCannon),
                Is.EqualTo(SuperWeaponState.Ready));
        }

        [Test]
        public void GrantSuperWeapon_WhileCharging_LeavesChargeAlone()
        {
            _superWeaponModel.StartCharging(SuperWeaponType.PlasmaCannon);

            bool granted = _cheatService.GrantSuperWeapon(SuperWeaponType.PlasmaCannon);

            Assert.That(granted, Is.False);
            Assert.That(_superWeaponModel.GetState(SuperWeaponType.PlasmaCannon),
                Is.EqualTo(SuperWeaponState.Charging));
        }

        private sealed class NoShipSpawnPoints : IShipSpawnPoints
        {
            public bool TryGetRandomSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
            {
                position = default;
                return false;
            }
        }

        private sealed class OperationalStationRegistry : IStationRegistry
        {
            public string Id => nameof(OperationalStationRegistry);

            public bool IsStationOperational(PlayerId owner) => true;

            public bool TryGetLivingStation(PlayerId owner, out IEntity station) =>
                throw new NotImplementedException();
        }

        private sealed class EmptyEntityLocator : IEntityLocator
        {
            public event Action<IEntity> EntityAdded { add { } remove { } }

            public event Action<IEntity> EntityRemoved { add { } remove { } }

            public string Id => nameof(EmptyEntityLocator);
            public IReadOnlyCollection<IEntity> Entities => Array.Empty<IEntity>();

            public void AddEntity(IEntity entity) { }

            public void RemoveEntity(IEntity entity) { }

            public IEntity GetEntity(long entityId) => throw new NotImplementedException();

            public bool TryGetEntity(long entityId, out IEntity entity) =>
                throw new NotImplementedException();

            public bool TryGetEntity(RaycastHit raycastHit, out IEntity entity)
            {
                entity = null;
                return false;
            }

            public bool TryGetEntity(Collider collider, out IEntity entity)
            {
                entity = null;
                return false;
            }
        }
    }
}
