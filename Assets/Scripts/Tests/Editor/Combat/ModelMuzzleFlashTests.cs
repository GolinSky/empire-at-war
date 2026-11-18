using System;
using EmpireAtWar.Editor.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class ModelMuzzleFlashTests
    {
        [TestCase("Assets/Art/Models", "t:Model")]
        [TestCase("Assets/Prefabs/Models", "t:Prefab")]
        [TestCase("Assets/Prefabs/View", "t:Prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement", "t:Prefab")]
        public void ModelAssets_HaveNoEmbeddedFlashObjects(string folder, string filter)
        {
            foreach (string guid in AssetDatabase.FindAssets(filter, new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(node.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase), Is.LessThan(0), path + ": " + node.name);
                if (filter == "t:Model")
                    foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (asset is Mesh mesh)
                            Assert.That(mesh.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase), Is.LessThan(0), path + ": " + mesh.name);
            }
        }

        [Test]
        public void Strip_RemovesFlashMeshesAndPreservesMuzzleWorldPose()
        {
            var root = new GameObject("Fighter");
            try
            {
                var hull = new GameObject("Hull");
                hull.transform.SetParent(root.transform, false);
                var flash = new GameObject("MuzzleA_00_Flash", typeof(MeshFilter), typeof(MeshRenderer));
                flash.transform.SetParent(root.transform, false);
                flash.transform.localPosition = new Vector3(1f, 2f, 3f);
                var muzzle = new GameObject("MuzzleA_00");
                muzzle.transform.SetParent(flash.transform, false);
                muzzle.transform.localPosition = new Vector3(2f, 3f, 4f);
                Vector3 position = muzzle.transform.position;

                Assert.That(ModelMuzzleFlashPostprocessor.Strip(root.transform), Is.EqualTo(1));

                Assert.That(flash == null, Is.True);
                Assert.That(hull != null, Is.True);
                Assert.That(muzzle.transform.parent, Is.SameAs(root.transform));
                Assert.That(muzzle.transform.position, Is.EqualTo(position));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Strip_PreservesMuzzleWithoutAFlash()
        {
            var root = new GameObject("Fighter");
            try
            {
                var muzzle = new GameObject("MuzzleA_00");
                muzzle.transform.SetParent(root.transform, false);

                Assert.That(ModelMuzzleFlashPostprocessor.Strip(root.transform), Is.Zero);
                Assert.That(muzzle != null, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
