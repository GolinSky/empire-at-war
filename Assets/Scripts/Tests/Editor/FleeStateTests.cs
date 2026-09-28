using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FleeStateTests
    {
        [Test]
        public void Constructor_FleesToOwnStationByPlayerIdOnly()
        {
            var parameterTypes = typeof(FleeState)
                .GetConstructors()
                .Single()
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            // The owner alone finds the station, so mirror matches (same faction twice) still flee home.
            Assert.That(parameterTypes, Does.Contain(typeof(PlayerId)));
            Assert.That(parameterTypes.Contains(typeof(IGameModelObserver)), Is.False);
            Assert.That(parameterTypes.Contains(typeof(FactionType)), Is.False);
        }
    }
}
