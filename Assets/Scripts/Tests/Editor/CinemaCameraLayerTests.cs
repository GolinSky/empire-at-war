using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Services.Layer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Services.FogOfWar;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CinemaCameraLayerTests
    {
        private const string LAYER_DATA_PATH = "Assets/Settings/Data/Models/Layer/LayerData.asset";
        private const string PREFAB_FOLDER = "Assets/Prefabs";
        private const string UI_LAYER_NAME = "UI";

        private LayerData _layerData;
        private LayerService _layerService;

        [SetUp]
        public void SetUp()
        {
            _layerData = AssetDatabase.LoadAssetAtPath<LayerData>(LAYER_DATA_PATH);
            _layerService = new LayerService(_layerData);
        }

        [Test]
        public void LayerData_WorldUiAndFog_MapToDedicatedProjectLayers()
        {
            Assert.AreEqual(LayerMask.NameToLayer("WorldUi"), _layerService.GetLayer(LayerKey.WorldUi));
            Assert.AreEqual(LayerMask.NameToLayer("Fog"), _layerService.GetLayer(LayerKey.Fog));
        }

        [Test]
        public void CoreGameCameraMask_RendersUiWorldUiAndFog()
        {
            foreach (int layer in GameOnlyLayers())
            {
                Assert.IsTrue(Contains(_layerService.CoreGameCameraMask, layer), LayerMask.LayerToName(layer));
            }
        }

        [Test]
        public void CinemaCameraMask_HidesUiWorldUiAndFog_AndKeepsUnits()
        {
            foreach (int layer in GameOnlyLayers())
            {
                Assert.IsFalse(Contains(_layerService.CinemaCameraMask, layer), LayerMask.LayerToName(layer));
            }

            Assert.IsTrue(Contains(_layerService.CinemaCameraMask, _layerService.GetLayer(LayerKey.Unit)));
            Assert.IsTrue(Contains(_layerService.CinemaCameraMask, LayerMask.NameToLayer("Default")));
        }

        [Test]
        public void Apply_WithChildren_KeepsWorldUiChildLayer()
        {
            GameObject root = new GameObject("Unit");
            GameObject canvas = new GameObject("SelectedCanvas");
            GameObject image = new GameObject("SelectedImage");
            GameObject model = new GameObject("Model");
            try
            {
                canvas.transform.SetParent(root.transform);
                image.transform.SetParent(canvas.transform);
                model.transform.SetParent(root.transform);
                _layerService.Apply(canvas, LayerKey.WorldUi, true);

                _layerService.Apply(root, LayerKey.Dead, true);

                Assert.AreEqual(_layerService.GetLayer(LayerKey.Dead), model.layer);
                Assert.AreEqual(_layerService.GetLayer(LayerKey.WorldUi), canvas.layer);
                Assert.AreEqual(_layerService.GetLayer(LayerKey.WorldUi), image.layer);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Prefabs_WorldSpaceCanvases_AreHiddenFromCinemaCamera()
        {
            List<string> failures = new List<string>();
            foreach (GameObject prefab in LoadPrefabs())
            {
                foreach (Canvas canvas in prefab.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.renderMode != RenderMode.WorldSpace)
                    {
                        continue;
                    }

                    foreach (Transform node in canvas.GetComponentsInChildren<Transform>(true))
                    {
                        if (Contains(_layerService.CinemaCameraMask, node.gameObject.layer))
                        {
                            failures.Add($"{AssetDatabase.GetAssetPath(prefab)}: {node.name}");
                        }
                    }
                }
            }

            CollectionAssert.IsEmpty(failures);
        }

        [Test]
        public void Prefabs_FogOfWar_IsOnFogLayer()
        {
            int fogCount = 0;
            foreach (GameObject prefab in LoadPrefabs())
            {
                foreach (FogOfWarSystem fog in prefab.GetComponentsInChildren<FogOfWarSystem>(true))
                {
                    fogCount++;
                    Assert.AreEqual(_layerService.GetLayer(LayerKey.Fog), fog.gameObject.layer,
                        AssetDatabase.GetAssetPath(prefab));
                }
            }

            Assert.Greater(fogCount, 0);
        }

        private IEnumerable<int> GameOnlyLayers()
        {
            yield return LayerMask.NameToLayer(UI_LAYER_NAME);
            yield return _layerService.GetLayer(LayerKey.WorldUi);
            yield return _layerService.GetLayer(LayerKey.Fog);
        }

        private static IEnumerable<GameObject> LoadPrefabs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PREFAB_FOLDER }))
            {
                yield return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        private static bool Contains(LayerMask mask, int layer)
        {
            return (mask.value & (1 << layer)) != 0;
        }
    }
}
