using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipEngineHardpointTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(0)]
        [TestCase(1)]
        public void TwoEngines_EitherDestructionSlowsShipAndBothReachMinimum(int first)
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>("Assets/Settings/Data/Ship/ExecutorShipData.asset");
            var root = new GameObject("Engine regression") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var ship = root.AddComponent<EmpireAtWar.Ship.Ship>();
                var movement = root.AddComponent<ShipMoveComponent>();
                var model = new ShipMoveModel(data, new CombatModifiers());
                typeof(MonoComponent<ShipMoveModel>).GetMethod("SetModel", PRIVATE_INSTANCE)
                    .Invoke(movement, new object[] { model });
                typeof(EmpireAtWar.Ship.Ship).GetField("_shipMoveComponent", PRIVATE_INSTANCE).SetValue(ship, movement);
                typeof(EmpireAtWar.Ship.Ship).GetField("<Data>k__BackingField", PRIVATE_INSTANCE).SetValue(ship, data);
                var engines = (List<HardPointModel>)typeof(EmpireAtWar.Ship.Ship)
                    .GetField("_engineUnits", PRIVATE_INSTANCE).GetValue(ship);
                var changed = (Action)Delegate.CreateDelegate(typeof(Action), ship,
                    typeof(EmpireAtWar.Ship.Ship).GetMethod("HandleEnginesData", PRIVATE_INSTANCE));
                for (int i = 0; i < 2; i++)
                {
                    var engine = new HardPointModel(HardPointType.Engines, i);
                    engine.SetHealth(4000, 1);
                    engines.Add(engine);
                    engine.OnHardPointHealthChanged += changed;
                }

                changed();
                Assert.That(model.Speed, Is.EqualTo(15f));
                engines[first].ApplyDamage(4000);
                Assert.That(model.Speed, Is.EqualTo(15f * (1f + data.MinMoveCoefficient) / 2f).Within(.001f));
                engines[1 - first].ApplyDamage(100);
                Assert.That(model.Speed, Is.GreaterThan(15f * data.MinMoveCoefficient));
                engines[1 - first].ApplyDamage(3900);
                Assert.That(model.Speed, Is.EqualTo(15f * data.MinMoveCoefficient).Within(.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
