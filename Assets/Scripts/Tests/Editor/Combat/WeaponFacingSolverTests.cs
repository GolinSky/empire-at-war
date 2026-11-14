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

        [TestCase(257)]
        [TestCase(1031)]
        public void SeededLayoutsPreserveReferenceTurnAndScore(int seed)
        {
            var random = new System.Random(seed);
            var solver = new WeaponFacingSolver();
            for (int layout = 0; layout < 80; layout++)
            {
                solver.Clear();
                var targets = new Vector3[1 + random.Next(10)];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = new Vector3(random.Next(-220, 221), random.Next(-55, 56),
                        random.Next(-220, 221));
                    solver.AddTarget(targets[i]);
                }

                var mounts = new (Vector3 Offset, Quaternion Parent, float Range, float MinYaw,
                    float MaxYaw, int Weight, bool Main)[1 + random.Next(12)];
                for (int i = 0; i < mounts.Length; i++)
                {
                    bool fullCircle = i % 5 == 0;
                    float minYaw = fullCircle ? -180f : random.Next(-180, 161);
                    float maxYaw = fullCircle ? 180f : random.Next((int)minYaw + 1, 181);
                    Vector3 offset = new Vector3(random.Next(-70, 71), random.Next(-20, 21), random.Next(-70, 71));
                    Quaternion parent = Quaternion.Euler(i % 2 == 0 ? 0 : random.Next(-85, 86),
                        random.Next(-180, 181), i % 2 == 0 ? 0 : random.Next(-180, 181));
                    mounts[i] = (offset, parent, random.Next(0, 350), minYaw, maxYaw, random.Next(1, 10),
                        layout % 4 != 0 && i % 3 != 0);
                    var mount = mounts[i];
                    solver.AddMount(mount.Offset, mount.Parent, mount.Range, mount.MinYaw, mount.MaxYaw,
                        mount.Weight, mount.Main);
                }

                float expected = FindReferenceTurn(targets, mounts, true, out int expectedScore);
                if (expectedScore == 0)
                    expected = FindReferenceTurn(targets, mounts, false, out expectedScore);

                float turn = solver.FindTurn(out int score);
                Assert.That(score, Is.EqualTo(expectedScore), $"Score on layout {layout}");
                Assert.That(turn, Is.EqualTo(expected), $"Turn on layout {layout}");
            }
        }

        [TestCase(0f, 0f, 100f, 0f, 0f)]
        [TestCase(90f, 0f, 100f, -5f, 5f)]
        [TestCase(32f, 44f, 100f, 179f, 180f)]
        [TestCase(32f, 44f, 99.99999f, -180f, -179f)]
        public void ReferenceParityAtYawAndRangeBoundaries(float pitch, float roll, float range,
            float minYaw, float maxYaw)
        {
            var solver = new WeaponFacingSolver();
            var targets = new[] { new Vector3(0f, 25f, 100f), new Vector3(0f, 25f, 0f) };
            Quaternion parent = Quaternion.Euler(pitch, 0f, roll);
            var mounts = new[] { (Vector3.zero, parent, range, minYaw, maxYaw, 1, true) };
            foreach (Vector3 target in targets) solver.AddTarget(target);
            solver.AddMount(Vector3.zero, parent, range, minYaw, maxYaw, 1, mainBattery: true);

            float expectedTurn = FindReferenceTurn(targets, mounts, true, out int expectedScore);
            float turn = solver.FindTurn(out int score);
            Assert.That(score, Is.EqualTo(expectedScore));
            Assert.That(turn, Is.EqualTo(expectedTurn));
        }

        private static float FindReferenceTurn(Vector3[] targets,
            (Vector3 Offset, Quaternion Parent, float Range, float MinYaw, float MaxYaw, int Weight, bool Main)[] mounts,
            bool main, out int bestScore)
        {
            bestScore = 0;
            float bestTurn = 0f;
            for (int step = 0; step < 180; step++)
            {
                int turn = (step + 1) / 2 * 2 * (step % 2 == 0 ? -1 : 1);
                Quaternion rotation = Quaternion.AngleAxis(-turn, Vector3.up);
                int score = 0;
                foreach (var mount in mounts)
                {
                    if (mount.Main != main) continue;
                    foreach (Vector3 offset in targets)
                    {
                        if (Mathf.Abs(new Vector2(offset.x, offset.z).magnitude -
                                      new Vector2(mount.Offset.x, mount.Offset.z).magnitude) > mount.Range)
                            continue;
                        Vector3 target = rotation * offset;
                        Vector3 direction = target - mount.Offset;
                        if (direction.x * direction.x + direction.z * direction.z > mount.Range * mount.Range)
                            continue;
                        if ((mount.MinYaw > -180f || mount.MaxYaw < 180f) &&
                            !WeaponTargetSelector.TryCalculateAim(target, mount.Offset, mount.Parent, mount.Range,
                                mount.MinYaw, mount.MaxYaw, out _, out _))
                            continue;
                        score += mount.Weight;
                        break;
                    }
                }
                if (score <= bestScore) continue;
                bestScore = score;
                bestTurn = turn;
            }
            return bestTurn;
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
