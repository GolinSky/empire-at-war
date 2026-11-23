using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Shipped beam colours: Venator-class beams and the Imperator are blue, the Executor is orange.</summary>
    [Category("Abilities")]
    public sealed class BeamColorDataTests
    {
        private const string CATALOG_PATH = "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset";
        private const string WEAPONS_PATH = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";

        [Test]
        public void VenatorLaserBeamWeapon_IsBlue() =>
            AssertBlue(AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_PATH).GetProfile(WeaponType.LaserBeam).Color);

        [TestCase(ShipAbilityId.LaserBeam)] // Imperator
        [TestCase(ShipAbilityId.ProtonBeam)] // Resolute (Venator class)
        public void BeamAbility_IsBlue(ShipAbilityId id) => AssertBlue(BeamColor(id));

        [Test]
        public void ExecutorLaserBeam_IsOrange()
        {
            Color color = BeamColor(ShipAbilityId.ExecutorLaserBeam);
            Assert.That(color.r, Is.GreaterThan(color.g));
            Assert.That(color.g, Is.GreaterThan(color.b));
        }

        private static Color BeamColor(ShipAbilityId id) =>
            ((ProtonBeamSettings)AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(CATALOG_PATH).Get(id).Settings).BeamColor;

        private static void AssertBlue(Color color) =>
            Assert.That(color.b, Is.GreaterThan(color.r).And.GreaterThan(color.g));
    }
}
