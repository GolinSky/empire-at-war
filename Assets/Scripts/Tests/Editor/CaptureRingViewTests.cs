using System.Reflection;
using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Entities.CaptureSites;
using UnityEditor;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CaptureRingViewTests
    {
        [TestCase(CaptureSiteState.Neutral, OwnerRelation.Neutral, OwnerRelation.Neutral, false, false, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Own, OwnerRelation.Neutral, false, true, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Ally, OwnerRelation.Neutral, false, false, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Enemy, OwnerRelation.Neutral, false, false, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Own, OwnerRelation.Enemy, false, false, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Own, OwnerRelation.Neutral, true, false, true)]
        [TestCase(CaptureSiteState.Constructing, OwnerRelation.Own, OwnerRelation.Neutral, false, false, false)]
        [TestCase(CaptureSiteState.Operational, OwnerRelation.Own, OwnerRelation.Neutral, false, false, false)]
        public void SiteState_DrivesBlinkAndVisibility(CaptureSiteState state, OwnerRelation owner,
            OwnerRelation capturer, bool contested, bool blink, bool visible)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/CaptureSites/CaptureSite.prefab");
            GameObject root = Object.Instantiate(prefab);
            try
            {
                CaptureSiteView view = root.GetComponent<CaptureSiteView>();
                var ring = (CaptureRingView)new SerializedObject(view).FindProperty("captureRing").objectReferenceValue;
                var renderer = (MeshRenderer)new SerializedObject(ring).FindProperty("ringRenderer").objectReferenceValue;
                view.Render(owner, state, SiteFacilityType.Mining, "MINE", capturer, 0.2f, 0.4f, contested);
                view.SetVisibility(true, false);
                Assert.That(typeof(CaptureRingView).GetField("_blink", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ring), Is.EqualTo(blink));
                Assert.That(renderer.enabled, Is.EqualTo(visible));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void BuiltRingStaysHiddenAfterVisibilityRefresh_ReleaseShowsIt()
        {
            var root = new GameObject("Capture ring test");
            try
            {
                MeshRenderer renderer = root.AddComponent<MeshRenderer>();
                CaptureRingView ring = root.AddComponent<CaptureRingView>();
                typeof(CaptureRingView).GetField("ringRenderer", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(ring, renderer);
                ring.Render(OwnerRelation.Own, false, false, true);
                ring.SetVisibility(true);
                Assert.That(renderer.enabled, Is.False);
                ring.Render(OwnerRelation.Neutral, false, false, false);
                Assert.That(renderer.enabled, Is.True);
                ring.SetVisibility(false);
                Assert.That(renderer.enabled, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
