using System;
using System.Reflection;
using System.Threading;
using EmpireAtWar.Repository;
using EmpireAtWar.Repository.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

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

        [Test]
        public void Initialize_KeepsTheMappingHandleUntilLoaderDisposal()
        {
            using AddressableHandleLoader loader = new AddressableHandleLoader();
            AssetMappingData mapping = AssetDatabase.LoadAssetAtPath<AssetMappingData>(
                "Assets/Settings/AssetMappingData.asset");
            using ResourceManager resources = new ResourceManager();
            AsyncOperationHandle<AssetMappingData> handle = resources.CreateCompletedOperation(mapping, string.Empty);
            SetMappingHandle(loader, handle);
            int releaseCount = 0;
            handle.Destroyed += releasedHandle => releaseCount++;

            loader.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            loader.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(handle.IsValid(), Is.True);
            Assert.That(typeof(AddressableHandleLoader).GetField("_mappingData",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(loader), Is.SameAs(mapping));

            loader.Dispose();
            loader.Dispose();

            Assert.That(handle.IsValid(), Is.False);
            Assert.That(releaseCount, Is.EqualTo(1));
        }

        [Test]
        public void Initialize_NullMapping_ReleasesTheHandleAndRejectsLaterLoads()
        {
            using AddressableHandleLoader loader = new AddressableHandleLoader();
            using ResourceManager resources = new ResourceManager();
            AsyncOperationHandle<AssetMappingData> handle =
                resources.CreateCompletedOperation<AssetMappingData>(null, string.Empty);
            SetMappingHandle(loader, handle);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                loader.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult());

            Assert.That(exception.Message, Does.Contain(nameof(AssetMappingData)));
            Assert.That(handle.IsValid(), Is.False);
            Assert.Throws<InvalidOperationException>(() => loader.Load<UnityEngine.Object>("MissingAsset"));
        }

        [Test]
        public void Initialize_AfterDispose_RejectsTheRequest()
        {
            using AddressableHandleLoader loader = new AddressableHandleLoader();
            loader.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                loader.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult());
            Assert.Throws<ObjectDisposedException>(() => loader.Load<UnityEngine.Object>("MissingAsset"));
        }

        private static void SetMappingHandle(AddressableHandleLoader loader, AsyncOperationHandle<AssetMappingData> handle)
        {
            typeof(AddressableHandleLoader).GetField("_mappingLoadAttempted",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(loader, true);
            typeof(AddressableHandleLoader).GetField("_mappingHandle",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(loader, handle);
        }
    }
}
