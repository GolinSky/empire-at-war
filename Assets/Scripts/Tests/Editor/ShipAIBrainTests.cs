using System.Linq;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Models.Health;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAIBrainTests
    {
        [Test]
        public void Constructor_DependsDirectlyOnHealthModelObserver()
        {
            var constructor = typeof(ShipAIBrain).GetConstructors().Single();

            Assert.That(
                constructor.GetParameters()[1].ParameterType,
                Is.EqualTo(typeof(IHealthModelObserver)));
        }
    }
}
