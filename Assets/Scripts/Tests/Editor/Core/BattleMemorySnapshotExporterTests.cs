using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Timing
{
    [Category("Core")]
    public sealed class BattleMemorySnapshotExporterTests
    {
        private const BindingFlags MEMBERS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        [Test]
        public void PinnedMemoryProfiler_ProvidesSnapshotReaderAndCrawlerContracts()
        {
            Assembly package = Assembly.Load("Unity.MemoryProfiler.Editor");
            Type reader = package.GetType("Unity.MemoryProfiler.Editor.Format.QueriedSnapshot.FileReader", true);
            Assert.That(reader.GetMethod("Open", MEMBERS, null, new[] { typeof(string) }, null), Is.Not.Null);
            Assert.That(typeof(IDisposable).IsAssignableFrom(reader), Is.True);
            Type snapshot = package.GetType("Unity.MemoryProfiler.Editor.CachedSnapshot", true);
            Assert.That(snapshot.GetConstructors(MEMBERS).Any(constructor =>
                constructor.GetParameters().Length == 1 && constructor.GetParameters()[0].ParameterType.IsAssignableFrom(reader)), Is.True);
            MethodInfo process = snapshot.GetMethod("PostProcess", MEMBERS);
            Assert.That(process, Is.Not.Null);
            Assert.That(process.GetParameters()[0].ParameterType, Is.EqualTo(typeof(bool)));
            Assert.That(typeof(IEnumerator).IsAssignableFrom(process.ReturnType), Is.True);
            foreach (string field in new[] { "NativeObjects", "NativeTypes", "NativeRootReferences", "TypeDescriptions" })
                Assert.That(snapshot.GetField(field, MEMBERS), Is.Not.Null, field);
            Assert.That(snapshot.GetProperty("CrawledData", MEMBERS), Is.Not.Null);
        }

        [Test]
        public void PinnedMemoryProfiler_ExposesManagedObjectsAndDirectReferrersWithoutRefReturnReflection()
        {
            Assembly package = Assembly.Load("Unity.MemoryProfiler.Editor");
            Type data = package.GetType("Unity.MemoryProfiler.Editor.ManagedData", true);
            FieldInfo objects = data.GetField("m_ManagedObjects", MEMBERS);
            Assert.That(objects, Is.Not.Null);
            Assert.That(typeof(IEnumerable).IsAssignableFrom(objects.FieldType), Is.True);
            Type connections = data.GetProperty("Connections", MEMBERS).PropertyType;
            Assert.That(connections.GetProperty("Count", MEMBERS), Is.Not.Null);
            PropertyInfo item = connections.GetProperty("Item", MEMBERS);
            Assert.That(item.GetIndexParameters()[0].ParameterType, Is.EqualTo(typeof(long)));
            Assert.That(item.PropertyType.IsByRef, Is.False);
            foreach (string property in new[] { "ToManagedObjectIndex", "FromManagedObjectIndex", "FromManagedType" })
                Assert.That(item.PropertyType.GetProperty(property, MEMBERS), Is.Not.Null, property);
            Type info = package.GetType("Unity.MemoryProfiler.Editor.ManagedObjectInfo", true);
            foreach (string field in new[] { "ITypeDescription", "NativeObjectIndex", "Size" })
                Assert.That(info.GetField(field, MEMBERS), Is.Not.Null, field);
        }
    }
}
