using System.Linq;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ForwardMainBatteryTests
    {
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
