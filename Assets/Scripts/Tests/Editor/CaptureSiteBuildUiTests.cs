using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Entities.CaptureSites;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Capture")]
    public sealed class CaptureSiteBuildUiTests
    {
        [TestCase(640f, 360f)]
        [TestCase(1280f, 720f)]
        [TestCase(1920f, 1080f)]
        public void SelectedSiteKind_RebuildsChoicesAndKeepsMiningOptions(float width, float height)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Ui/SkirmishGame/CaptureSiteBuildUi.prefab");
            var data = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            CaptureSiteBuildUi ui = Object.Instantiate(prefab).GetComponent<CaptureSiteBuildUi>();
            try
            {
                RectTransform root = (RectTransform)ui.transform;
                root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                var miniMapSlot = new GameObject("MiniMapSlot", typeof(RectTransform)).GetComponent<RectTransform>();
                miniMapSlot.SetParent(root, false);
                miniMapSlot.anchorMin = Vector2.zero;
                miniMapSlot.anchorMax = Vector2.zero;
                miniMapSlot.pivot = Vector2.zero;
                miniMapSlot.anchoredPosition = new Vector2(20f, 22f);
                miniMapSlot.sizeDelta = new Vector2(296f, 292f);
                ui.SetMiniMapSlot(miniMapSlot);
                ui.SetData(data);
                ui.AssignPresenter(new Presenter());
                ui.Initialize();
                ui.SetFacilities(data.GetAllowedFacilities(CaptureSiteKind.Satellite));
                var options = (List<SiteFacilityOptionView>)typeof(CaptureSiteBuildUi)
                    .GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
                Assert.That(options.Count(option => option.gameObject.activeSelf), Is.EqualTo(5));
                RectTransform panel = (RectTransform)typeof(CaptureSiteBuildUi)
                    .GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
                Vector2 scaled = Vector2.Scale(panel.rect.size, panel.localScale);
                Assert.That(scaled.x, Is.LessThanOrEqualTo(width + 0.01f));
                Assert.That(scaled.y, Is.LessThanOrEqualTo(height + 0.01f));
                AssertPanelAvoidsMiniMap(panel, miniMapSlot, root);
                foreach (SiteFacilityOptionView option in options)
                    Assert.That(option.DisplayName, Is.EqualTo(data.GetCost(option.FacilityType).Name.ToUpperInvariant()));
                ui.SetFacilities(data.GetAllowedFacilities(CaptureSiteKind.Mining));
                Assert.That(options.Where(option => option.gameObject.activeSelf).Select(option => option.FacilityType),
                    Is.EqualTo(new[] { SiteFacilityType.Mining, SiteFacilityType.BattleAsteroid }));
                AssertPanelAvoidsMiniMap(panel, miniMapSlot, root);
                ui.Dispose();
                ui.Initialize();
                ui.SetFacilities(data.GetAllowedFacilities(CaptureSiteKind.Satellite));
                Assert.That(options.Count, Is.EqualTo(5), "Reinitialization must not duplicate pooled rows.");
                AssertPanelAvoidsMiniMap(panel, miniMapSlot, root);
            }
            finally { Object.DestroyImmediate(ui.gameObject); }
        }

        private static void AssertPanelAvoidsMiniMap(RectTransform panel, RectTransform miniMapSlot,
            RectTransform root)
        {
            var panelCorners = new Vector3[4];
            var miniMapCorners = new Vector3[4];
            var rootCorners = new Vector3[4];
            panel.GetWorldCorners(panelCorners);
            miniMapSlot.GetWorldCorners(miniMapCorners);
            root.GetWorldCorners(rootCorners);
            var panelBounds = Rect.MinMaxRect(panelCorners[0].x, panelCorners[0].y,
                panelCorners[2].x, panelCorners[2].y);
            var miniMapBounds = Rect.MinMaxRect(miniMapCorners[0].x, miniMapCorners[0].y,
                miniMapCorners[2].x, miniMapCorners[2].y);
            Assert.That(panelBounds.Overlaps(miniMapBounds), Is.False,
                "Build options must not cover minimap input.");
            Assert.That(panelBounds.xMin, Is.GreaterThanOrEqualTo(rootCorners[0].x - 0.01f));
            Assert.That(panelBounds.yMin, Is.GreaterThanOrEqualTo(rootCorners[0].y - 0.01f));
            Assert.That(panelBounds.xMax, Is.LessThanOrEqualTo(rootCorners[2].x + 0.01f));
            Assert.That(panelBounds.yMax, Is.LessThanOrEqualTo(rootCorners[2].y + 0.01f));
        }

        private sealed class Presenter : ICaptureSiteBuildPresenter
        {
            public void Build(SiteFacilityType type) { }
        }
    }
}
