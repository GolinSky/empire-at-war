using EmpireAtWar.Editor;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ShipPickHullTests
    {
        private const float BOUNDS_TOLERANCE = 0.05f;
        // Older boxes were fitted to slightly different geometry; a few percent is padding, not a stale box.
        private const float BOX_TOLERANCE_FRACTION = 0.03f;

        [Test]
        public void LargeHullsArePickedByTheirConvexHull_OtherShipsByTheirBox()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ShipData), new[] { "Assets/Settings/Data/Ship" }))
            {
                ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid));
                GameObject view = WeaponLoadoutBaker.LoadShipView(data);
                // Spawn clearance reads the root box as hull bounds, so every view keeps exactly one.
                BoxCollider box = view.GetComponents<BoxCollider>().Length == 1 ? view.GetComponent<BoxCollider>() : null;
                Assert.That(box, Is.Not.Null, $"{view.name} must keep one root hull box.");
                MeshCollider[] pickHulls = view.GetComponents<MeshCollider>();

                if (!ShipPickHullBaker.NeedsPickHull(data.ShipClass))
                {
                    Assert.That(box.enabled, Is.True, $"{view.name} is picked by its hull box.");
                    Assert.That(pickHulls, Is.Empty, $"{view.name} has no pick hull.");
                    continue;
                }

                // A big hull's box encloses open space that must not catch taps aimed past or above the ship.
                Assert.That(box.enabled, Is.False, $"{view.name} hull box must not catch pointer rays.");
                Assert.That(pickHulls.Length, Is.EqualTo(1), $"{view.name} needs one pick hull; run Bake Pick Hulls.");
                MeshCollider pickHull = pickHulls[0];
                Assert.That(pickHull.enabled && pickHull.convex && !pickHull.isTrigger, Is.True, view.name);
                Assert.That(pickHull.sharedMesh, Is.Not.Null, view.name);
                // A stale bake after a model change no longer spans the bound hull renderers.
                Bounds pick = pickHull.sharedMesh.bounds;
                Bounds hull = ShipPickHullBaker.MeasureHull(view);
                Assert.That(Vector3.Distance(pick.min, hull.min), Is.LessThan(BOUNDS_TOLERANCE),
                    $"{view.name} pick hull is stale; run Bake Pick Hulls.");
                Assert.That(Vector3.Distance(pick.max, hull.max), Is.LessThan(BOUNDS_TOLERANCE),
                    $"{view.name} pick hull is stale; run Bake Pick Hulls.");
            }
        }

        [Test]
        public void HullBoxesMatchTheVisibleHull()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ShipData), new[] { "Assets/Settings/Data/Ship" }))
            {
                ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid));
                GameObject view = WeaponLoadoutBaker.LoadShipView(data);
                // The box sizes the spawn footprint and, below battlecruisers, catches taps; a model rescale or
                // flip must move it too. Mediator's 1.3x rescale and a half-size Munificent box went unnoticed.
                BoxCollider box = view.GetComponent<BoxCollider>();
                Bounds hull = ShipPickHullBaker.MeasureHull(view);
                Bounds bounds = new Bounds(box.center, box.size);
                float tolerance = Mathf.Max(hull.size.x, hull.size.y, hull.size.z) * BOX_TOLERANCE_FRACTION;
                Assert.That(Vector3.Distance(bounds.min, hull.min), Is.LessThan(tolerance),
                    $"{view.name} hull box no longer matches its hull.");
                Assert.That(Vector3.Distance(bounds.max, hull.max), Is.LessThan(tolerance),
                    $"{view.name} hull box no longer matches its hull.");
            }
        }
    }
}
