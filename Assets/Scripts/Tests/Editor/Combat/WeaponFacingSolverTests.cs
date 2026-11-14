using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class WeaponFacingSolverTests
    {
        [Test]
        public void MainBatteryTakesPriorityOverHigherDpsSecondaries()
        {
            WeaponFacingSolver solver = new WeaponFacingSolver();
            solver.AddTarget(Vector3.forward * 100f);
            solver.AddMount(Vector3.zero, Quaternion.identity, 150f, -50f, -30f, 1, mainBattery: true);
            solver.AddMount(Vector3.zero, Quaternion.identity, 150f, 30f, 50f, 100, mainBattery: false);

            Assert.That(solver.FindTurn(), Is.EqualTo(30f).Within(2f));
        }

        [Test]
        public void OutOfReachMainBatteryFallsBackToReachableSecondary()
        {
            WeaponFacingSolver solver = new WeaponFacingSolver();
            solver.AddTarget(Vector3.forward * 100f);
            solver.AddMount(Vector3.zero, Quaternion.identity, 90f, -50f, -30f, 100, mainBattery: true);
            solver.AddMount(Vector3.zero, Quaternion.identity, 150f, 30f, 50f, 1, mainBattery: false);

            Assert.That(solver.FindTurn(), Is.EqualTo(-30f).Within(2f));
        }

        [Test]
        public void RotatingAnOffsetMountCanBringANearbyHardpointIntoRange()
        {
            WeaponFacingSolver solver = new WeaponFacingSolver();
            solver.AddTarget(Vector3.forward * 100f);
            solver.AddTarget(Vector3.right * 150f);
            solver.AddMount(Vector3.left * 80f, Quaternion.identity, 30f, -180f, 180f, 1, mainBattery: true);

            Assert.That(solver.FindTurn(), Is.InRange(74f, 90f));
        }

        [Test]
        public void CurrentHeadingIsStableInsideBestMainBatterySector()
        {
            WeaponFacingSolver solver = new WeaponFacingSolver();
            solver.AddTarget(Vector3.forward * 100f);
            solver.AddMount(Vector3.zero, Quaternion.identity, 150f, -10f, 70f, 1, mainBattery: true);
            solver.AddMount(Vector3.zero, Quaternion.identity, 150f, -60f, -40f, 100, mainBattery: false);

            Assert.That(solver.FindTurn(), Is.Zero);
        }

        [Test]
        public void DisabledDestroyedAndTargetIneligibleMountsCannotInfluenceFacing()
        {
            GameObject mountObject = new GameObject("Facing mount");
            try
            {
                WeaponHardPoint mount = mountObject.AddComponent<WeaponHardPoint>();
                WeaponProfile profile = new WeaponProfile(WeaponType.Laser, "Laser", WeaponFamily.Laser,
                    DamageType.Laser, 10f, 1, 0f, 1f, 100f, true, false, null, 0f,
                    Color.white, Vector3.one, false, false, default, default, 0f);
                typeof(WeaponHardPoint).GetField("_profile",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(mount, profile);
                CombatModifiers modifiers = new CombatModifiers();

                Assert.That(mount.CanContributeToFacing(ShipClass.Capital, modifiers), Is.False);
                Assert.That(mount.CanContributeToFacing(ShipClass.Fighter, modifiers), Is.True);

                modifiers.SetMainBatteries(true, 1f);
                Assert.That(mount.CanContributeToFacing(ShipClass.Fighter, modifiers), Is.False);

                SerializedObject serialized = new SerializedObject(mount);
                serialized.FindProperty("mainBattery").boolValue = true;
                serialized.FindProperty("spawnDestroyedExplosion").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(mount.CanContributeToFacing(ShipClass.Fighter, modifiers), Is.True);

                mount.UpdateData(0f);
                Assert.That(mount.CanContributeToFacing(ShipClass.Fighter, modifiers), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(mountObject);
            }
        }
    }
}
