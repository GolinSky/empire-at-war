using System;
using EmpireAtWar.Controllers.Factions;
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
        private CheatService _service;

        [SetUp]
        public void SetUp()
        {
            _economyData = ScriptableObject.CreateInstance<EconomyData>();
            _reinforcementData = ScriptableObject.CreateInstance<ReinforcementData>();
            _economyModel = new EconomyModel(_economyData, 100f);
            _reinforcementModel = new ReinforcementModel(_reinforcementData);
            _service = new CheatService(
                _economyModel,
                _reinforcementModel,
                new ShipFacadeFactory(),
                new FakeReinforcementZonesSystem());
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

        private sealed class FakeReinforcementZonesSystem : IReinforcementZonesSystem
        {
            public event Action OwnershipChanged
            {
                add { }
                remove { }
            }

            public bool IsPositionInAnyZone(Vector3 position)
            {
                return false;
            }

            public bool IsPositionInOwnedZone(PlayerType playerType, Vector3 position)
            {
                return false;
            }

            public bool TryGetDefaultSpawnPosition(PlayerType playerType, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetRandomSpawnPosition(PlayerType playerType, out Vector3 position)
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
    }
}
