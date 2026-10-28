using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Entities.CaptureSites;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CaptureSiteBuildUiTests
    {
        [TestCase(640f, 360f)]
        [TestCase(1280f, 720f)]
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
                foreach (SiteFacilityOptionView option in options)
                    Assert.That(option.DisplayName, Is.EqualTo(data.GetCost(option.FacilityType).Name.ToUpperInvariant()));
                ui.SetFacilities(data.GetAllowedFacilities(CaptureSiteKind.Mining));
                Assert.That(options.Where(option => option.gameObject.activeSelf).Select(option => option.FacilityType),
                    Is.EqualTo(new[] { SiteFacilityType.Mining, SiteFacilityType.BattleAsteroid }));
                ui.Dispose();
                ui.Initialize();
                ui.SetFacilities(data.GetAllowedFacilities(CaptureSiteKind.Satellite));
                Assert.That(options.Count, Is.EqualTo(5), "Reinitialization must not duplicate pooled rows.");
            }
            finally { Object.DestroyImmediate(ui.gameObject); }
        }

        private sealed class Presenter : ICaptureSiteBuildPresenter
        {
            public void Build(SiteFacilityType type) { }
        }
    }
}
