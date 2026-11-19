using System;
using EmpireAtWar.Components.Ship.Health;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class ShipClassExtensionsTests
    {
        private static readonly ShipClass[] HULLS_BY_SIZE =
        {
            ShipClass.Corvette, ShipClass.Frigate, ShipClass.Cruiser, ShipClass.HeavyCruiser, ShipClass.Capital,
            ShipClass.BattleCruiser, ShipClass.Dreadnought
        };

        private static readonly ShipClass[] UNTIERED =
        {
            ShipClass.Fighter, ShipClass.Bomber, ShipClass.Interceptor, ShipClass.HeroStrikecraft, ShipClass.Structure
        };

        [Test]
        public void HullTier_AscendsFromCorvetteToDreadnought()
        {
            Assert.That(HULLS_BY_SIZE[0].HullTier(), Is.GreaterThan(0));
            for (int i = 1; i < HULLS_BY_SIZE.Length; i++)
                Assert.That(HULLS_BY_SIZE[i].HullTier(), Is.GreaterThan(HULLS_BY_SIZE[i - 1].HullTier()),
                    HULLS_BY_SIZE[i].ToString());
        }

        [Test]
        public void HullTier_StrikecraftAndStructuresHaveNoTier()
        {
            foreach (ShipClass shipClass in UNTIERED)
                Assert.That(shipClass.HullTier(), Is.Zero, shipClass.ToString());
        }

        [Test]
        public void HullTier_EveryShipClassIsClassified()
        {
            // A new ShipClass member must be placed in the size order or declared untiered here.
            foreach (ShipClass shipClass in Enum.GetValues(typeof(ShipClass)))
                Assert.That(Array.IndexOf(HULLS_BY_SIZE, shipClass) >= 0 || Array.IndexOf(UNTIERED, shipClass) >= 0,
                    Is.True, shipClass.ToString());
        }
    }
}
