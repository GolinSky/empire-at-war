using System;
using System.Reflection;
using EmpireAtWar.Repository;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AddressableHandleLoaderTests
    {
        [Test]
        public void EveryMappedAssetExists()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(
                "Assets/Settings/AssetMappingData.asset");
            var entries = new SerializedObject(asset).FindProperty("assetMappings.keyValue");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                string guid = entry.FindPropertyRelative("value.m_AssetGUID").stringValue;
                Assert.That(AssetDatabase.GUIDToAssetPath(guid), Is.Not.Empty,
                    entry.FindPropertyRelative("key").stringValue);
            }
        }

        [Test]
        public void FailedMappingLoadRejectsEverySubsequentRequest()
        {
            var loader = new AddressableHandleLoader();
            typeof(AddressableHandleLoader).GetField("_mappingLoadAttempted",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(loader, true);

            for (int attempt = 0; attempt < 2; attempt++)
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => loader.Load<UnityEngine.Object>("MissingAsset"));
                Assert.That(exception.Message, Does.Contain("AssetMappingData"));
            }
        }
    }
}
