using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
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
        private CheatService _service;

        [SetUp]
        public void SetUp()
        {
            _economyData = ScriptableObject.CreateInstance<EconomyData>();
            _reinforcementData = ScriptableObject.CreateInstance<ReinforcementData>();
            _economyModel = new EconomyModel(_economyData, 100f);
            _reinforcementModel = new ReinforcementModel(_reinforcementData);
            _superWeaponModel = new SuperWeaponModel();
            _service = new CheatService(
                _economyModel,
                _reinforcementModel,
                new ShipFactory(),
                new FakeReinforcementZonesSystem(),
                new OperationalEntityLocator(),
                _superWeaponModel);
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
            _service.AddMoney(250f);

            Assert.That(_economyModel.Money, Is.EqualTo(350f));
        }

        [Test]
        public void AddMoney_NonPositiveAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.AddMoney(0f));
        }

        [Test]
        public void AddShipReinforcement_NotifiesReinforcementModel()
        {
            FactionData factionData = new FactionData();
            ShipUnitRequest request = new ShipUnitRequest(factionData, ShipType.Venator);
            string addedId = null;
            FactionData addedData = null;
            _reinforcementModel.OnReinforcementAdded += (id, data) =>
            {
                addedId = id;
                addedData = data;
            };

            _service.AddShipReinforcement(request);

            Assert.That(addedId, Is.EqualTo(nameof(ShipType.Venator)));
            Assert.That(addedData, Is.SameAs(factionData));
        }

        [Test]
        public void ForceSpawnShipAtDefaultZone_WithoutOwnedZone_ReturnsFalse()
        {
            ShipUnitRequest request = new ShipUnitRequest(new FactionData(), ShipType.Venator);

            bool spawned = _service.ForceSpawnShipAtDefaultZone(request);

            Assert.That(spawned, Is.False);
        }

        [Test]
        public void GrantSuperWeapon_MakesWeaponReady()
        {
            bool granted = _service.GrantSuperWeapon(SuperWeaponType.IonCannon);

            Assert.That(granted, Is.True);
            Assert.That(_superWeaponModel.GetState(SuperWeaponType.IonCannon),
                Is.EqualTo(SuperWeaponState.Ready));
        }

        [Test]
        public void GrantSuperWeapon_WhileCharging_LeavesChargeAlone()
        {
            _superWeaponModel.StartCharging(SuperWeaponType.PlasmaCannon);

            bool granted = _service.GrantSuperWeapon(SuperWeaponType.PlasmaCannon);

            Assert.That(granted, Is.False);
            Assert.That(_superWeaponModel.GetState(SuperWeaponType.PlasmaCannon),
                Is.EqualTo(SuperWeaponState.Charging));
        }

        private sealed class FakeReinforcementZonesSystem : IReinforcementZonesSystem
        {
            public event Action OwnershipChanged
            {
                add { }
                remove { }
            }

            public bool IsPositionInAnyZone(Vector3 position, float clearance = 0f)
            {
                return false;
            }

            public bool IsPositionInOwnedZone(PlayerType playerType, Vector3 position)
            {
                return false;
            }

            public int GetOwnedCapturableZoneCount(PlayerType playerType)
            {
                return 0;
            }

            public void CopyOwnedCapturableZoneCenters(PlayerType playerType, System.Collections.Generic.List<Vector3> destination)
            {
                destination.Clear();
            }

            public bool IsShipSpawnPositionClear(ShipType shipType, Vector3 position)
            {
                return true;
            }

            public bool TryGetDefaultSpawnPosition(PlayerType playerType, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetDefaultZoneCenter(PlayerType playerType, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetDefaultZoneExitPosition(
                PlayerType playerType,
                Vector3 shipPosition,
                float shipRadius,
                out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetRandomSpawnPosition(
                PlayerType playerType,
                ShipType shipType,
                out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position)
            {
                position = default;
                return false;
            }
        }

        private sealed class OperationalEntityLocator : IEntityLocator
        {
            public string Id => nameof(OperationalEntityLocator);
            public IReadOnlyCollection<IEntity> Entities => Array.Empty<IEntity>();
            public event Action<IEntity> EntityAdded { add { } remove { } }
            public event Action<IEntity> EntityRemoved { add { } remove { } }
            public bool IsStationOperational(PlayerType playerType) => true;
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
