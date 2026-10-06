using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class TractorBeamSettingsTests
    {
        [TestCase(ShipClass.Corvette, true)]
        [TestCase(ShipClass.Frigate, true)]
        [TestCase(ShipClass.Fighter, false)]
        [TestCase(ShipClass.Bomber, false)]
        [TestCase(ShipClass.Interceptor, false)]
        [TestCase(ShipClass.Capital, false)]
        [TestCase(ShipClass.HeavyCapital, false)]
        [TestCase(ShipClass.Structure, false)]
        public void CanTarget_DefaultsToCorvettesAndFrigates(ShipClass shipClass, bool expected) =>
            Assert.That(new TractorBeamSettings().CanTarget(shipClass), Is.EqualTo(expected));
    }
}
