using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EmpressDefensePlatformCombatTests
    {
        private const string DATA = "Assets/Settings/Data/Models/DefendPlatform/AotrEmpressDefensePlatformData.asset";
        private const string VIEW = "Assets/Prefabs/Models/DefendStation/AotrEmpressDefensePlatformView.prefab";
        private const string WEAPONS = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";

        private static float LongestWeaponRange()
        {
            WeaponsData profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS);
            var root = PrefabUtility.LoadPrefabContents(VIEW);
            try
            {
                return root.GetComponentsInChildren<WeaponHardPoint>(true)
                    .Max(point => profiles.GetProfile(point.WeaponType).Range);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void DetectionAndVision_ReachLongestBattery()
        {
            var empress = AssetDatabase.LoadAssetAtPath<DefendPlatformData>(DATA).ComponentData;
            float longest = LongestWeaponRange();

            Assert.That(empress.Range, Is.GreaterThanOrEqualTo(longest));
            Assert.That(empress.VisionRange, Is.GreaterThanOrEqualTo(longest));
        }

        [TestCase(400f)]
        [TestCase(800f)]
        public void LongRangeBatteries_CanAcquireTargetsFromEveryDirection(float distance)
        {
            WeaponsData profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS);
            var root = PrefabUtility.LoadPrefabContents(VIEW);
            try
            {
                var weapons = root.GetComponentsInChildren<WeaponHardPoint>(true).Where(point =>
                    point.WeaponType == WeaponType.HeavyLongRangeTripleTurbolaser ||
                    point.WeaponType == WeaponType.HeavyLongRangeTripleIonCannon ||
                    point.WeaponType == WeaponType.MediumLongRangeDualTurbolaser).ToArray();
                foreach (float heading in new[] { 0f, 73f })
                {
                    root.transform.rotation = Quaternion.Euler(0f, heading, 0f);
                    for (int yaw = 0; yaw < 360; yaw += 15)
                    {
                        Vector3 target = root.transform.position +
                            Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * distance;
                        bool canFire = weapons.Any(point => WeaponTargetSelector.TryCalculateAim(
                            target, point.transform.position, point.transform.parent.rotation,
                            profiles.GetProfile(point.WeaponType).Range, point.MinYaw, point.MaxYaw, out _, out _));

                        Assert.That(canFire, Is.True,
                            $"No battery can fire at {distance} units, approach {yaw} degrees, station heading {heading}.");
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
