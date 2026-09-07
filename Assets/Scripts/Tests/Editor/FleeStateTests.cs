using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FleeStateTests
    {
        [Test]
        public void Constructor_UsesShipPlayerTypeAndSharedGameModel()
        {
            var parameterTypes = typeof(FleeState)
                .GetConstructors()
                .Single()
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            Assert.That(parameterTypes, Does.Contain(typeof(PlayerType)));
            Assert.That(parameterTypes, Does.Contain(typeof(IGameModelObserver)));
            Assert.That(parameterTypes.Contains(typeof(FactionType)), Is.False);
        }
    }
}
