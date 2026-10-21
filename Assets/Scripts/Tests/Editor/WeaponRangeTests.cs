using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Utils;
using NUnit.Framework;
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
                model.SetRanges(900f, new Dictionary<ShipClass, float>());
                typeof(WeaponComponent).BaseType.GetMethod("SetModel", PRIVATE_INSTANCE)
                    .Invoke(weapon, new object[] { model });
                typeof(WeaponComponent).GetField("_attackRangeCircle", PRIVATE_INSTANCE).SetValue(weapon, circle);
                typeof(WeaponComponent).GetField("_viewTransform", PRIVATE_INSTANCE)
                    .SetValue(weapon, root.transform);
                typeof(WeaponComponent).GetField("_modifiers", PRIVATE_INSTANCE)
                    .SetValue(weapon, new CombatModifiers());

                weapon.Tick();

                Assert.That(ring.transform.position, Is.EqualTo(root.transform.position));
                Assert.That(ring.GetComponent<LineRenderer>().GetPosition(0).magnitude,
                    Is.EqualTo(model.MaxWeaponRange).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(ring);
                Object.DestroyImmediate(root);
            }
        }
    }
}
