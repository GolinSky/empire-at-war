using EmpireAtWar.Models.Economy;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EconomyModelTests
    {
        [Test]
        public void Constructor_UsesSelectedStartingMoney()
        {
            EconomyData data = ScriptableObject.CreateInstance<EconomyData>();
            EconomyModel model = new EconomyModel(data, 2500f);

            Assert.That(model.Money, Is.EqualTo(2500f));
        }

        [Test]
        public void TrySpend_AllowsSpendingExactBalance()
        {
            EconomyData data = ScriptableObject.CreateInstance<EconomyData>();
            EconomyModel model = new EconomyModel(data, 500f);

            bool result = model.TrySpend(500f);

            Assert.That(result, Is.True);
            Assert.That(model.Money, Is.Zero);
        }
    }
}
