using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.MiniMap;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class SpaceHazardPresentationTests
    {
        [Test]
        public void MinimapFillCoversOnlyTheGameplayOutline()
        {
            var data = ScriptableObject.CreateInstance<SpaceHazardData>();
            var root = new GameObject("HazardMinimap", typeof(RectTransform));
            var mesh = new Mesh();
            try
            {
                HazardShape shape = TestHazards.CreateEllipse(data, new Vector3(0f, 0f, 50f), Vector3.right, 400f, 100f);
                var view = root.AddComponent<MiniMapHazardView>();
                view.rectTransform.sizeDelta = new Vector2(1000f, 1000f);
                var range = new Vector2Range();
                typeof(Vector2Range).BaseType.GetProperty("Min").SetValue(range, new Vector2(-500f, -500f));
                typeof(Vector2Range).BaseType.GetProperty("Max").SetValue(range, new Vector2(500f, 500f));
                view.SetData(new[] { new MiniMapHazard(shape) }, range);
                using (var vertices = new VertexHelper())
                {
                    typeof(MiniMapHazardView).GetMethod("OnPopulateMesh",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                        null, new[] { typeof(VertexHelper) }, null)
                        .Invoke(view, new object[] { vertices });
                    vertices.FillMesh(mesh);
                }
                // The rect maps one pixel to one metre, so local mesh coordinates are world x/z.
                Vector3[] points = mesh.vertices;
                Assert.That(points.Length, Is.GreaterThan(0));
                for (int i = 0; i < points.Length; i += 4)
                {
                    Vector3 center = (points[i] + points[i + 2]) * 0.5f;
                    Assert.That(shape.Contains(new Vector3(center.x, 0f, center.y), shape.CellSize, false), Is.True);
                }
                Assert.That(shape.Contains(new Vector3(0f, 0f, 50f), 0f, false), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void MapAndMinimapHavePersistedHazardBindingsAndRenderBelowFog()
        {
            var scene = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab");
            MapLayoutView map = scene.GetComponentInChildren<MapLayoutView>(true);
            var mapSerialized = new SerializedObject(map);
            Assert.That(mapSerialized.FindProperty("hazardPrefab").objectReferenceValue, Is.TypeOf<HazardCloudView>());
            var effects = scene.GetComponentInChildren<HazardEffectsView>(true);
            Assert.That(effects, Is.Not.Null);
            Assert.That(new SerializedObject(effects).FindProperty("hazardEntryPrefab").objectReferenceValue, Is.TypeOf<HazardEntryCue>());
            string[] presets = AssetDatabase.FindAssets("t:SpaceHazardData", new[] { "Assets/Settings" });
            Assert.That(presets.Length, Is.GreaterThanOrEqualTo(2));
            foreach (string guid in presets)
            {
                var data = AssetDatabase.LoadAssetAtPath<SpaceHazardData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(data.DisplayName, Is.Not.Empty, data.name);
                Assert.That(data.MinimapIcon, Is.Not.Null, data.name);
                Assert.That(data.MinimapColor.a, Is.GreaterThan(0f), data.name);
                Assert.That(data.VisualMaterial, Is.Not.Null, data.name);
                Assert.That(data.VisualMaterial.renderQueue, Is.EqualTo(2950), data.name);
                Assert.That(ShaderUtil.ShaderHasError(data.VisualMaterial.shader), Is.False, data.name);
            }
            var mini = AssetDatabase.LoadAssetAtPath<MiniMapUi>("Assets/Prefabs/Ui/MiniMap/MiniMapUi.prefab");
            var miniSerialized = new SerializedObject(mini);
            var hazard = (MiniMapHazardView)miniSerialized.FindProperty("hazardView").objectReferenceValue;
            var fog = (UnityEngine.UI.RawImage)miniSerialized.FindProperty("fogImage").objectReferenceValue;
            Assert.That(hazard.transform.GetSiblingIndex(), Is.LessThan(fog.transform.GetSiblingIndex()));
        }

        [TestCase("ShipUi")]
        [TestCase("ShipGroupEntryUi")]
        public void SelectedShipStatusTracksOverlappingHazardsAndClearsAfterExit(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<ShipUi>($"Assets/Prefabs/Ui/Ship/{prefabName}.prefab");
            ShipUi view = Object.Instantiate(prefab);
            try
            {
                var modifiers = new CombatModifiers();
                var label = (TMP_Text)new SerializedObject(view).FindProperty("hazardStatus").objectReferenceValue;
                view.SetModifiers(modifiers);
                modifiers.AddAbilitySuppression();
                Assert.That(label.text, Is.EqualTo("Abilities Jammed"));
                modifiers.AddShieldSuppression(2f);
                Assert.That(label.text, Is.EqualTo("Shields Offline"));
                modifiers.RemoveShieldSuppression();
                Assert.That(label.text, Is.EqualTo("Abilities Jammed"));
                modifiers.RemoveAbilitySuppression();
                Assert.That(label.text, Is.Empty);
                view.SetModifiers(null);
            }
            finally { Object.DestroyImmediate(view.gameObject); }
        }
    }
}
