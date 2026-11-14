using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Squadrons.Icon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronIconPrefabTests
    {
        private const string SQUADRON_PREFAB_FOLDER = "Assets/Prefabs/Models/Squadrons";
        private const string FRAME_SHADER = "EmpireAtWar/UI/Squadron Frame";
        private const string ICON_SHADER = "EmpireAtWar/UI/Squadron Icon";

        [Test]
        public void ClickAndMarquee_UseTheDisplayedRectangleWhileJoiningAGrid()
        {
            GameObject root = new GameObject("Marker", typeof(RectTransform));
            try
            {
                var marker = new BattleMarker(null, (RectTransform)root.transform, null, null);
                var viewport = new Rect(0f, 0f, 400f, 400f);
                marker.Display(new Vector2(100f, 100f), viewport, 1f, 132f, 1f);
                marker.Display(new Vector2(300f, 100f), viewport, 1f, 84f, 0.25f, 1);
                Assert.That(marker.ScreenRect.center.x, Is.EqualTo(150f));
                Assert.That(marker.ScreenRect.width, Is.EqualTo(120f));
                Assert.That(marker.ContainsScreenPoint(new Vector2(209f, 100f)), Is.True);
                Assert.That(marker.OverlapsMarquee(new MarqueeRectangle(
                    new MarqueePoint(209f, 98f), new MarqueePoint(212f, 102f))), Is.True);
                Assert.That(marker.ContainsScreenPoint(new Vector2(300f, 100f)), Is.False);

                marker.Hide();
                Assert.That(marker.ContainsScreenPoint(new Vector2(150f, 100f)), Is.False);
                Assert.That(marker.OverlapsMarquee(new MarqueeRectangle(
                    new MarqueePoint(149f, 98f), new MarqueePoint(151f, 102f))), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SquadronViews_MarkerRootIsAssignedAndHasNoOwnCanvas()
        {
            int checkedCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { SQUADRON_PREFAB_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                SquadronIconComponent icon = prefab.GetComponent<SquadronIconComponent>();
                if (icon == null) continue;

                SerializedProperty root = new SerializedObject(icon).FindProperty("iconRoot");
                RectTransform marker = (RectTransform)root.objectReferenceValue;
                Assert.That(marker, Is.Not.Null, path);
                // A canvas per marker rebuilds one batch per squadron every frame; the shared layer owns the canvas.
                Assert.That(marker.GetComponentsInChildren<Canvas>(true), Is.Empty, path);
                Assert.That(marker.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("WorldUi")), path);
                // The frame shader reads centre offsets from UV1: batched into the shared canvas, vertices are screen positions.
                Graphic frame = (Graphic)new SerializedObject(icon).FindProperty("frameImage").objectReferenceValue;
                if (frame.material.shader.name == FRAME_SHADER)
                    Assert.That(frame.GetComponent<SquadronFrameMeshEffect>(), Is.Not.Null, path);
                Image unitIcon = (Image)new SerializedObject(icon).FindProperty("iconImage").objectReferenceValue;
                Assert.That(unitIcon, Is.Not.Null, path);
                Assert.That(unitIcon.enabled, Is.True, path);
                Assert.That(unitIcon.material.shader.name, Is.EqualTo(ICON_SHADER), path);
                Assert.That(unitIcon.sprite, Is.Not.Null, path);
                string unitName = prefab.name.Replace("SquadronView", "");
                string iconFolder = unitName == "YWingBomber" || unitName == "TIEAvenger"
                    ? "ShipIcon"
                    : "SquadronIcon";
                // Icons are grouped by faction: <iconFolder>/<Faction>/<Unit>Icon.png.
                Assert.That(AssetDatabase.GetAssetPath(unitIcon.sprite),
                    Does.StartWith($"Assets/Art/Textures/Ui/Icons/{iconFolder}/").And.EndWith($"/{unitName}Icon.png"), path);
                Assert.That(unitIcon.preserveAspect, Is.True, path);
                Assert.That(unitIcon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(-13.5f, -13.5f)), path);
                Assert.That(unitIcon.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(0f, -1.5f)), path);
                int unitImageCount = 0;
                foreach (Image image in marker.GetComponentsInChildren<Image>(true))
                {
                    Assert.That(image.name, Is.Not.EqualTo("Silhouette"), path);
                    if (image == frame || image.name == "Background") continue;
                    Assert.That(image, Is.SameAs(unitIcon), path);
                    unitImageCount++;
                }
                Assert.That(unitImageCount, Is.EqualTo(1), path);
                checkedCount++;
            }

            Assert.That(checkedCount, Is.GreaterThan(0));
        }
    }
}
