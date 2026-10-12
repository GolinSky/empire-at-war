using EmpireAtWar.Components.Squadrons.Icon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SquadronIconPrefabTests
    {
        private const string SQUADRON_PREFAB_FOLDER = "Assets/Prefabs/Models/Squadrons";

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
                checkedCount++;
            }

            Assert.That(checkedCount, Is.GreaterThan(0));
        }
    }
}
