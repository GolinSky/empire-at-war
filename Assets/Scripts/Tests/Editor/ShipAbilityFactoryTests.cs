using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using UnityEditor;
using Zenject;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAbilityFactoryTests
    {
        [Test]
        public void EverySettingsType_CreatesANewAbilityEachTime()
        {
            DiContainer container = new DiContainer();
            container.Bind<IEntityLocator>().FromInstance(new EntityLocator());

            foreach (Type settingsType in TypeCache.GetTypesDerivedFrom<ShipAbilitySettings>())
            {
                if (settingsType.IsAbstract) continue;
                ShipAbilitySettings settings = (ShipAbilitySettings)Activator.CreateInstance(settingsType);
                IShipAbility first = settings.CreateAbility(container);
                IShipAbility second = settings.CreateAbility(container);
                Assert.That(first, Is.Not.Null, settingsType.Name);
                Assert.That(second, Is.TypeOf(first.GetType()), settingsType.Name);
                Assert.That(second, Is.Not.SameAs(first), settingsType.Name);
            }
        }
    }
}
