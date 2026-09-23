using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using Zenject;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAbilityFactoryTests
    {
        [Test]
        public void EveryAbilityId_CreatesANewAbility()
        {
            DiContainer container = new DiContainer();
            container.Bind<IEntityLocator>().FromInstance(new EntityLocator());
            ShipAbilityFactory factory = new ShipAbilityFactory(container);

            foreach (ShipAbilityId id in Enum.GetValues(typeof(ShipAbilityId)))
            {
                if (id == ShipAbilityId.None) continue;
                IShipAbility first = factory.Create(id);
                IShipAbility second = factory.Create(id);
                Assert.That(first, Is.Not.Null, id.ToString());
                Assert.That(second, Is.TypeOf(first.GetType()), id.ToString());
                Assert.That(second, Is.Not.SameAs(first), id.ToString());
            }
        }
    }
}
