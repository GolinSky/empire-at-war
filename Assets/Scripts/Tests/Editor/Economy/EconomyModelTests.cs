using EmpireAtWar.Models.Economy;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Economy")]
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

        [Test]
        public void Refund_ReturnsMoneyWithoutCountingAsEarnings()
        {
            EconomyData data = ScriptableObject.CreateInstance<EconomyData>();
            EconomyModel model = new EconomyModel(data, 500f);
            model.TrySpend(200f);
            float earned = model.TotalEarned;

            model.Refund(200f);
            Assert.That(model.Money, Is.EqualTo(500f));
            Assert.That(model.TotalEarned, Is.EqualTo(earned));

            model.AddMoney(100f);
            Assert.That(model.TotalEarned, Is.GreaterThan(earned));
        }
    }
}
