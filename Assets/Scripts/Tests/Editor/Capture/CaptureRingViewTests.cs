using System.Reflection;
using DG.Tweening;
using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEditor;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Capture")]
    public sealed class CaptureRingViewTests
    {
        [TestCase(CaptureSiteState.Neutral, OwnerRelation.Neutral, OwnerRelation.Neutral, false, false, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Own, OwnerRelation.Neutral, false, true, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Ally, OwnerRelation.Neutral, false, false, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Enemy, OwnerRelation.Neutral, false, false, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Own, OwnerRelation.Enemy, false, true, true)]
        [TestCase(CaptureSiteState.Owned, OwnerRelation.Own, OwnerRelation.Neutral, true, true, true)]
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
                view.Render(owner, state, SiteFacilityType.Mining, "MINE", capturer, 0.2f, 0.4f, contested,
                    contested ? OwnerRelation.Own : OwnerRelation.Neutral,
                    contested ? OwnerRelation.Enemy : OwnerRelation.Neutral);
                view.SetVisibility(true, false);
                Assert.That(typeof(CaptureRingView).GetField("_blink", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ring), Is.EqualTo(blink));
                Assert.That(renderer.enabled, Is.EqualTo(visible));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CaptureColor_CyclesInTwoSeconds_AndRepeatedRenderDoesNotRestart()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/CaptureSites/CaptureSite.prefab");
            GameObject root = Object.Instantiate(prefab);
            try
            {
                CaptureSiteView view = root.GetComponent<CaptureSiteView>();
                var ring = (CaptureRingView)new SerializedObject(view).FindProperty("captureRing").objectReferenceValue;
                var renderer = (MeshRenderer)new SerializedObject(ring).FindProperty("ringRenderer").objectReferenceValue;
                ring.Render(OwnerRelation.Neutral, false, false, false, OwnerRelation.Own);
                ring.SetVisibility(true);
                var tween = (Tween)typeof(CaptureRingView).GetField("_blinkTween", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ring);
                var block = new MaterialPropertyBlock();
                Color neutral = ring.GetColor(OwnerRelation.Neutral, false);
                Color player = ring.GetColor(OwnerRelation.Own, false);

                tween.Goto(0.5f);
                ring.Render(OwnerRelation.Neutral, false, false, false, OwnerRelation.Own);
                renderer.GetPropertyBlock(block);
                Color halfway = block.GetColor("_BaseColor");
                Assert.That(halfway.r, Is.EqualTo((neutral.r + player.r) * 0.5f).Within(0.001f));
                Assert.That(halfway.g, Is.EqualTo((neutral.g + player.g) * 0.5f).Within(0.001f));
                tween.Goto(1f);
                renderer.GetPropertyBlock(block);
                AssertColorEqual(block.GetColor("_BaseColor"), player);
                tween.Goto(2f);
                renderer.GetPropertyBlock(block);
                AssertColorEqual(block.GetColor("_BaseColor"), neutral);

                ring.Render(OwnerRelation.Own, false, false, false);
                Assert.That(typeof(CaptureRingView).GetField("_blinkTween", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ring), Is.Null);
                renderer.GetPropertyBlock(block);
                AssertColorEqual(block.GetColor("_BaseColor"), player);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ContestedNeutralSite_UsesBothSides_AndKeepsCapturePanelHidden()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/CaptureSites/CaptureSite.prefab");
            GameObject root = Object.Instantiate(prefab);
            try
            {
                CaptureSiteView view = root.GetComponent<CaptureSiteView>();
                var serializedView = new SerializedObject(view);
                var ring = (CaptureRingView)serializedView.FindProperty("captureRing").objectReferenceValue;
                var canvas = (Canvas)serializedView.FindProperty("statusCanvas").objectReferenceValue;
                var renderer = (MeshRenderer)new SerializedObject(ring).FindProperty("ringRenderer").objectReferenceValue;
                view.Render(OwnerRelation.Neutral, CaptureSiteState.Neutral, SiteFacilityType.Mining, "MINE",
                    OwnerRelation.Neutral, 0f, 0f, true, OwnerRelation.Own, OwnerRelation.Enemy);
                view.SetVisibility(true, true);
                Assert.That(canvas.gameObject.activeSelf, Is.False);
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                AssertColorEqual(block.GetColor("_BaseColor"), ring.GetColor(OwnerRelation.Own, false));
                var tween = (Tween)typeof(CaptureRingView).GetField("_blinkTween", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ring);
                tween.Goto(1f);
                renderer.GetPropertyBlock(block);
                AssertColorEqual(block.GetColor("_BaseColor"), ring.GetColor(OwnerRelation.Enemy, false));
                ring.SetVisibility(false);
                Assert.That(typeof(CaptureRingView).GetField("_blinkTween", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ring), Is.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ReinforcementZone_CapturePanelStaysHidden_WhileRingBlinks()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab");
            GameObject root = Object.Instantiate(prefab);
            try
            {
                ReinforcementZoneView view = root.GetComponent<ReinforcementZoneView>();
                var serializedView = new SerializedObject(view);
                var canvas = (Canvas)serializedView.FindProperty("captureCanvas").objectReferenceValue;
                var ring = (CaptureRingView)serializedView.FindProperty("captureRing").objectReferenceValue;
                view.Render(OwnerRelation.Neutral, OwnerRelation.Own, 0.2f, false);
                view.SetVisibility(true, true);
                Assert.That(canvas.gameObject.activeSelf, Is.False);
                Assert.That(typeof(CaptureRingView).GetField("_captureBlink", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ring), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void AssertColorEqual(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.00001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.00001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.00001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.00001f));
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
