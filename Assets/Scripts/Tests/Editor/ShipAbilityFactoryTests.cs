using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAbilityFactoryTests
    {
        private static readonly HashSet<Type> _statModifierSettings = new HashSet<Type>
        {
            typeof(BoostEnginePowerSettings),
            typeof(BoostShieldPowerSettings),
            typeof(BoostWeaponPowerSettings),
            typeof(InvulnerabilitySettings)
        };

        [Test]
        public void EverySettingsType_CreatesANewAbilityEachTime()
        {
            ShipAbilityDependencies dependencies = new ShipAbilityDependencies(
                new EntityLocator(), TestPlayers.CreateDuel(), new ImpactEffectPresenter(null, null),
                AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset"),
                new BoltFlightBatch());
            IShipAbilityFactory factory = new ShipAbilityFactory(dependencies);

            foreach (Type settingsType in TypeCache.GetTypesDerivedFrom<ShipAbilitySettings>())
            {
                if (settingsType.IsAbstract) continue;
                ShipAbilitySettings settings = (ShipAbilitySettings)Activator.CreateInstance(settingsType);
                ShipAbilityDefinition definition = new ShipAbilityDefinition();
                typeof(ShipAbilityDefinition).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(definition, settings);
                IShipAbility first = factory.Create(definition);
                IShipAbility second = factory.Create(definition);
                Type abilityType = _statModifierSettings.Contains(settingsType)
                    ? typeof(StatModifierAbility)
                    : settingsType.Assembly.GetType(settingsType.FullName.Replace("Settings", "Ability"));
                Assert.That(first, Is.TypeOf(abilityType), settingsType.Name);
                Assert.That(second, Is.TypeOf(abilityType), settingsType.Name);
                Assert.That(second, Is.Not.SameAs(first), settingsType.Name);
            }
        }
    }
}
