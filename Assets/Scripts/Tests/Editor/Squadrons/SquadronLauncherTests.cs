using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Services.Stations;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronLauncherTests
    {
        [Test]
        public void TryLaunchFromStation_WithoutLivingStation_ReturnsFalse()
        {
            SquadronLauncher launcher = new SquadronLauncher(new NoStationRegistry());

            bool launched = launcher.TryLaunchFromStation(TestPlayers.Enemy, SquadronType.Vulture, out ISquadron squadron);

            Assert.That(launched, Is.False);
            Assert.That(squadron, Is.Null);
        }

        private sealed class NoStationRegistry : IStationRegistry
        {
            public string Id => nameof(NoStationRegistry);

            public bool HasProductionBase(PlayerId owner) => false;

            public bool TryGetProductionBase(PlayerId owner, out IEntity productionBase)
            {
                productionBase = null;
                return false;
            }

            public bool TryGetLivingStation(PlayerId owner, out IEntity station)
            {
                station = null;
                return false;
            }
        }
    }
}
