using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class WeaponRangeTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Tick_DrawsAttackRangeAtEntityRootWhenWeaponComponentIsOffset()
        {
            GameObject root = new GameObject("StationRoot");
            GameObject weaponObject = new GameObject("OffsetWeapon");
            DebugRangeCircle circle = new DebugRangeCircle(new RangeDebugModel { IsEnabled = true },
                new SelectionModel { IsSelected = true }, null, "AttackRange", Color.red);
            GameObject ring = (GameObject)typeof(DebugRangeCircle).GetField("_gameObject", PRIVATE_INSTANCE)
                .GetValue(circle);
            try
            {
                root.transform.position = new Vector3(300f, 40f, -200f);
                weaponObject.transform.SetParent(root.transform, false);
                weaponObject.transform.localPosition = new Vector3(-185f, 0f, -78f);
                WeaponComponent weapon = weaponObject.AddComponent<WeaponComponent>();
                WeaponModel model = new WeaponModel(new Dictionary<(DamageType, ShipClass), float>());
                model.SetAttackRange(900f);
                typeof(WeaponComponent).BaseType.GetMethod("SetModel", PRIVATE_INSTANCE)
                    .Invoke(weapon, new object[] { model });
                typeof(WeaponComponent).GetField("_attackRangeCircle", PRIVATE_INSTANCE).SetValue(weapon, circle);
                typeof(WeaponComponent).GetField("_viewTransform", PRIVATE_INSTANCE)
                    .SetValue(weapon, root.transform);

                weapon.Tick();

                Assert.That(ring.transform.position, Is.EqualTo(root.transform.position));
                Assert.That(ring.GetComponent<LineRenderer>().GetPosition(0).magnitude,
                    Is.EqualTo(model.OptimalAttackRange).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(ring);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StationWeapons_ReachCapitalShipRangeWithinDetectionAndVision()
        {
            SpaceStationData data = AssetDatabase.LoadAssetAtPath<SpaceStationData>(
                "Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset");
            Assert.That(data.ComponentData.WeaponRange, Is.GreaterThanOrEqualTo(750f));
            Assert.That(data.ComponentData.Range, Is.GreaterThanOrEqualTo(data.ComponentData.WeaponRange));
            Assert.That(data.ComponentData.VisionRange, Is.GreaterThanOrEqualTo(data.ComponentData.WeaponRange));
        }
    }
}
