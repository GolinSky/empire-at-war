using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Economy")]
    public sealed class ReinforcementDeployableCountTests
    {
        private const int MAX_CAPACITY = 20;

        private ReinforcementData _data;
        private ReinforcementModel _model;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<ReinforcementData>();
            SetField(_data, "<MaxUnitCapacity>k__BackingField", MAX_CAPACITY);
            _model = new ReinforcementModel(_data);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_data);

        [TestCase(4, 0, 4, 4)]
        [TestCase(4, 11, 4, 2)]
        [TestCase(4, 17, 4, 0)]
        [TestCase(2, 0, 3, 2)]
        public void DeployableCount_IsReserveLimitedByPopulation(int reserve, int usedCapacity, int unitCost,
            int expected)
        {
            ShipUnitRequest request = new ShipUnitRequest(Faction(unitCost), default);
            for (int i = 0; i < reserve; i++) _model.AddReinforcement(request);
            SetField(_model, "_currentUnitCapacity", usedCapacity);

            Assert.That(_model.GetDeployableCount(request), Is.EqualTo(expected));
        }

        [Test]
        public void DeployableCount_FreeUnitIsWholeReserve()
        {
            SquadronUnitRequest request = new SquadronUnitRequest(Faction(0), default);
            for (int i = 0; i < 3; i++) _model.AddReinforcement(request);
            SetField(_model, "_currentUnitCapacity", MAX_CAPACITY);

            Assert.That(_model.GetDeployableCount(request), Is.EqualTo(3));
        }

        [Test]
        public void UsesCapacity_OnlyShipsAndSquadrons()
        {
            Assert.That(ReinforcementModel.UsesCapacity(new ShipUnitRequest(Faction(1), default)), Is.True);
            Assert.That(ReinforcementModel.UsesCapacity(new SquadronUnitRequest(Faction(1), default)), Is.True);
            Assert.That(ReinforcementModel.UsesCapacity(new MiningFacilityUnitRequest(Faction(0), default)),
                Is.False);
        }

        private static FactionData Faction(int unitCapacity)
        {
            FactionData data = new FactionData();
            SetField(data, "<UnitCapacity>k__BackingField", unitCapacity);
            return data;
        }

        private static void SetField(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
