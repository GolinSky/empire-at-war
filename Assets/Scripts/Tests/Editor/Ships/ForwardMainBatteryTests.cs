using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ForwardMainBatteryTests
    {
        [TestCase("Empire/ISDI", 150f,
            "HP_ISD_1_New_HICT_02|HP_ISD_1_New_HTLT_03|HP_ISD_1_New_HTLT_05|HP_ISD_1_New_TL_03|HP_ISD_1_New_TL_04|HP_ISDI_PD_03|HP_ISDI_PD_04")]
        [TestCase("Empire/ISDI", 300f,
            "HP_ISD_1_New_HICT_01")]
        [TestCase("Empire/ISDII", 150f,
            "HP_ISD_New_TL_03|HP_ISD_New_TL_04")]
        [TestCase("Empire/ISDII", 300f,
            "HP_ISDII_LC_03|HP_ISDII_LC_04")]
        [TestCase("Empire/ImperialVenator", 300f,
            "IonProjectileR|IonProjectileL")]
        [TestCase("Empire/Procursator", 300f,
            "HP_Procursator_TLTL_02|HP_Procursator_TLTL_03|HP_Procursator_TLTL_04|HP_Procursator_TLTL_06|HP_Procursator_TLTL_07")]
        [TestCase("Empire/Tector", 150f,
            "HP_ISD2_Weapon_03|HP_ISD2_Weapon_04")]
        [TestCase("Rebellion/BothanAssaultCruiser", 150f,
            "MediumTurbolaser_0_0|MediumTurbolaser_0_1|MediumTurbolaser_1_0|MediumTurbolaser_1_1")]
        [TestCase("Rebellion/BothanAssaultCruiser", 300f,
            "DualHeavyTurbolaser_1_1")]
        [TestCase("Rebellion/CD273", 150f,
            "HP_LC_05|HP_LC_06|DualLaser_HP_TL_03_FP_01|DualLaser_HP_TL_03_FP_02")]
        [TestCase("Rebellion/CD273", 300f,
            "HP_LC_01|HP_LC_02|HP_ML_02_FP_01")]
        [TestCase("Rebellion/CorellianCorvette", 300f,
            "Laser_6|Laser_7")]
        [TestCase("Rebellion/Dauntless", 300f,
            "DauntlessWeapon04|DauntlessWeapon09|DauntlessWeapon14")]
        [TestCase("Rebellion/Endurance", 150f,
            "HeavyOctupleTurbolaser_HP_3")]
        [TestCase("Rebellion/HomeOne", 300f,
            "Turbolaser_BL|IonCannon_BL|Turbolaser_BR|IonCannon_BR|HeavyQuadTurbolaser_HP_11|HeavyQuadTurbolaser_HP_12|HeavyQuadTurbolaser_HP_13|HeavyQuadTurbolaser_HP_14|DualMediumTurbolaser_ML|DualMediumTurbolaser_MR|Added_HeavyTurbolaser_3|Added_HeavyTurbolaser_4|Added_HeavyTurbolaser_5|Added_HeavyTurbolaser_7")]
        [TestCase("Rebellion/MC75Profundity", 150f,
            "HP_MC75_Profundity_PD_01|HP_MC75_Profundity_PD_06")]
        [TestCase("Rebellion/MC75Profundity", 300f,
            "HP_MC75_Profundity_HLC_01|HP_MC75_Profundity_HLC_02|HP_MC75_Profundity_HLC_03")]
        [TestCase("Rebellion/MC80Independence", 150f,
            "HP_MC80_Independence_MTL_05|HP_MC80_Independence_HLC_06|Added_14_4")]
        [TestCase("Rebellion/MC80Independence", 300f,
            "HP_MC80_Independence_MTL_03|HP_MC80_Independence_MTL_04|HP_MC80_Independence_MTL_06|HP_MC80_Independence_MTL_07|HP_MC80_Independence_MTL_08|HP_MC80_Independence_LTIC_04|HP_MC80_Independence_LTIC_06|HP_MC80_Independence_LTL_01|HP_MC80_Independence_LTL_02|HP_MC80_Independence_LTL_05|HP_MC80_Independence_LTL_06|HP_MC80_Independence_LTL_07|HP_MC80_Independence_LTL_08|HP_MC80_Independence_HLC_01|HP_MC80_Independence_HLC_07|HP_MC80_Independence_HLC_08")]
        [TestCase("Rebellion/MC80Justice", 300f,
            "HP_MC80_Justice_LC_01|HP_MC80_Justice_LC_02|HP_MC80_Justice_LC_03|HP_MC80_Justice_LC_04|HP_MC80_Justice_LC_05|HP_MC80_Justice_LC_06|HP_MC80_Justice_LC_07|HP_MC80_Justice_LC_08")]
        [TestCase("Rebellion/MonCalCruiser", 300f,
            "IonCannon_ML|Added_HeavyTurbolaser_6")]
        [TestCase("Rebellion/Nebula", 150f,
            "AssaultConcussion_MUZZLEA_01|PointDefense_MUZZLEA_04|Added_HeavyTurbolaser_3|Added_HeavyTurbolaser_4|Added_HeavyTurbolaser_7|Added_HeavyTurbolaser_8")]
        [TestCase("Rebellion/Nebula", 300f,
            "AssaultConcussion_MUZZLEA_02")]
        [TestCase("Rebellion/NebulonB", 150f,
            "Laser_BL|Laser_BR|Laser_BL2|Laser_BR2")]
        [TestCase("Rebellion/QuasarFire", 150f,
            "PointDefenseLeft|PointDefenseRight")]
        [TestCase("Republic/Carrack", 150f,
            "DualLaser02|DualLaser04")]
        [TestCase("Republic/Carrack", 300f,
            "DualLaser03")]
        [TestCase("Republic/Centax", 150f,
            "Laser03|Laser04|Laser07|Laser08")]
        [TestCase("Republic/Centax", 300f,
            "Laser01|Laser02|Laser05|Laser06")]
        [TestCase("Republic/HeavyDreadnought", 150f,
            "MediumLaser_HP_0|DualTurboLaserDualProjectileR|MediumLaser_HP_8|DualTurboLaserDualProjectileR_Additional")]
        [TestCase("Republic/Imperator", 300f,
            "HeavyTwinIon_L|Turbo_1R|Turbo_1L")]
        [TestCase("Republic/Pelta", 150f,
            "Laser_5|Laser_6")]
        [TestCase("Republic/Pelta", 300f,
            "Turbolaser_1|Turbolaser_2|Turbolaser_3|Turbolaser_4")]
        [TestCase("Republic/Resolute", 300f,
            "Weapon_TL00|Weapon_TL01")]
        [TestCase("Republic/Rothana", 150f,
            "DBY827_Broadside_3|DBY827_Broadside_4")]
        [TestCase("Republic/Rothana", 300f,
            "RepublicMediumProtonTorpedo_HP_21")]
        public void SavedConvergenceMountsReachForwardTarget(string layout, float distance, string mountNames)
        {
            string[] parts = layout.Split('/');
            string path = $"Assets/Prefabs/Models/Ships/{parts[0]}/View/{parts[1]}ShipView.prefab";
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            WeaponHardPoint[] mounts = BalancePrefabBindings.Components(view).OfType<WeaponComponent>()
                .SelectMany(component => BalancePrefabBindings.References(component, "hardPoints"))
                .Cast<WeaponHardPoint>().ToArray();
            Vector3 target = view.transform.position + view.transform.forward * distance;

            foreach (string mountName in mountNames.Split('|'))
            {
                WeaponHardPoint mount = mounts.Single(candidate => candidate.name == mountName);
                Assert.That(WeaponTargetSelector.TryCalculateAim(target, mount.Position,
                    mount.transform.parent.rotation, weapons.GetProfile(mount.WeaponType).Range,
                    mount.MinYaw, mount.MaxYaw, out _, out _), Is.True,
                    $"{layout}/{mountName}: target {distance} units ahead of hull");
            }
        }

        [TestCase("Providence", 16)]
        [TestCase("Captor", 10)]
        [TestCase("Invincible", 26)]
        [TestCase("PatrolFrigate", 8)]
        [TestCase("C9979", 6)]
        [TestCase("Dispatcher", 19)]
        [TestCase("Bulwark", 19)]
        [TestCase("Wavecrest", 15)]
        public void SavedSeparatistForwardMountsConvergeAheadOfHull(string ship, int expectedCount)
        {
            string path = $"Assets/Prefabs/Models/Ships/Separatist/View/{ship}ShipView.prefab";
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            WeaponHardPoint[] mounts = BalancePrefabBindings.Components(view).OfType<WeaponComponent>()
                .SelectMany(component => BalancePrefabBindings.References(component, "hardPoints"))
                .Cast<WeaponHardPoint>()
                .Where(mount => mount.MinYaw <= 45f && mount.MaxYaw >= -45f).ToArray();

            Assert.That(mounts, Has.Length.EqualTo(expectedCount));
            foreach (WeaponHardPoint mount in mounts)
            {
                foreach (float distance in new[] { 300f, 600f })
                foreach (float angle in new[] { -4f, 0f, 4f })
                {
                    Vector3 target = view.transform.position + view.transform.rotation *
                        Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
                    // Check convergence from the actual mount offset, independently of weapon range.
                    Assert.That(WeaponTargetSelector.TryCalculateAim(target, mount.Position,
                        mount.transform.parent.rotation, float.MaxValue, mount.MinYaw, mount.MaxYaw,
                        out _, out _), Is.True, $"{ship}/{mount.name}: {distance} units, {angle} degrees");
                }
            }
        }

        [TestCase("Empire/ISDII", 8)]
        [TestCase("Empire/Tector", 8)]
        [TestCase("Empire/ImperialVenator", 8)]
        [TestCase("Empire/Victory", 6)]
        [TestCase("Empire/VictoryI", 6)]
        [TestCase("Empire/VictoryII", 6)]
        [TestCase("Empire/Executor", 33)]
        [TestCase("Republic/Venator", 8)]
        [TestCase("Republic/Resolute", 8)]
        [TestCase("Republic/Imperator", 6)]
        [TestCase("Republic/Rothana", 8)]
        public void SavedMainBatteriesShareForwardFiringSector(string layout, int expectedCount)
        {
            string[] parts = layout.Split('/');
            string path = $"Assets/Prefabs/Models/Ships/{parts[0]}/View/{parts[1]}ShipView.prefab";
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            WeaponComponent weapon = view.GetComponentsInChildren<WeaponComponent>(true).Single();
            SerializedProperty bindings = new SerializedObject(weapon).FindProperty("hardPoints");
            WeaponHardPoint[] mainBatteries = Enumerable.Range(0, bindings.arraySize)
                .Select(index => (WeaponHardPoint)bindings.GetArrayElementAtIndex(index).objectReferenceValue)
                .Where(mount => new SerializedObject(mount).FindProperty("mainBattery").boolValue).ToArray();

            Assert.That(mainBatteries, Has.Length.EqualTo(expectedCount));
            foreach (WeaponHardPoint mount in mainBatteries)
            {
                foreach (float angle in new[] { -4f, 0f, 4f })
                {
                    Vector3 aim = view.transform.rotation * Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    Assert.That(WeaponTargetSelector.TryCalculateAim(mount.Position + aim, mount.Position,
                        mount.transform.parent.rotation, 2f, mount.MinYaw, mount.MaxYaw, out _, out _),
                        Is.True, $"{layout}/{mount.name} at {angle} degrees");
                }
            }
        }
    }
}
