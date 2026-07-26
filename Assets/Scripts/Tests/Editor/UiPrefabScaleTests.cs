using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public class UiPrefabScaleTests
    {
        private const string UI_PREFAB_FOLDER = "Assets/Prefabs/Ui";

        [Test]
        public void ProjectUiPrefabs_HaveNoZeroScaleRectTransforms()
        {
            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { UI_PREFAB_FOLDER });
            List<string> zeroScaleTransforms = new();

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                Assert.That(prefab, Is.Not.Null, $"Failed to load UI prefab at {prefabPath}.");

                RectTransform[] rectTransforms =
                    prefab.GetComponentsInChildren<RectTransform>(true);

                foreach (RectTransform rectTransform in rectTransforms)
                {
                    Vector3 scale = rectTransform.localScale;
                    if (!HasZeroAxis(scale))
                        continue;

                    zeroScaleTransforms.Add(
                        $"{prefabPath}/{GetHierarchyPath(rectTransform)} has local scale {scale}.");
                }
            }

            Assert.That(
                zeroScaleTransforms,
                Is.Empty,
                "UI RectTransforms must not have a zero local-scale axis:\n" +
                string.Join("\n", zeroScaleTransforms));
        }

        private static bool HasZeroAxis(Vector3 scale)
        {
            return Mathf.Approximately(scale.x, 0f) ||
                   Mathf.Approximately(scale.y, 0f) ||
                   Mathf.Approximately(scale.z, 0f);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }
}
