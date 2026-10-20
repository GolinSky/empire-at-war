using System.Linq;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Entities
{
    public sealed class EntityBindingsTests
    {
        [Test]
        public void EntityPrefabs_HaveExplicitRequiredAndUniqueInjectionReferences()
        {
            int count = 0;
            foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/Prefabs/") && p.EndsWith(".prefab")))
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                EntityView view = root.GetComponent<EntityView>();
                if (view == null) continue;
                count++;
                EntityBindings bindings = view.Bindings;
                Assert.That(bindings, Is.Not.Null, path);
                Assert.That(bindings.Selection, Is.Not.Empty, path);
                Assert.That(bindings.Radar, Is.Not.Empty, path);
                Assert.That(bindings.FogVisibility, Is.Not.Empty, path);
                Assert.That(bindings.Injectables, Is.All.Not.Null, path);
                Assert.That(bindings.Injectables.Distinct().Count(), Is.EqualTo(bindings.Injectables.Length), path);
                Assert.That(bindings.Injectables.Contains(view), Is.False, path);
                if (view is Squadron)
                {
                    Assert.That(bindings.SquadronHealth, Is.Not.Null, path);
                    Assert.That(bindings.SquadronFlight, Is.Not.Null, path);
                    Assert.That(bindings.SquadronIcon, Is.Not.Null, path);
                }
                else
                    Assert.That(bindings.Health, Is.Not.Empty, path);
                if (view is EmpireAtWar.Ship.Ship)
                {
                    Assert.That(bindings.ShipMove, Is.Not.Null, path);
                    Assert.That(bindings.AudioShip, Is.Not.Null, path);
                    Assert.That(bindings.AudioDialog, Is.Not.Null, path);
                }
            }
            Assert.That(count, Is.GreaterThan(0), "The entity prefab audit must inspect at least one entity.");
        }
    }
}
