using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EmpireAtWar.Editor
{
    // Adapter for the pinned com.unity.memoryprofiler 1.1.12 snapshot reader.
    // Its cached snapshot and crawler are internal; a package upgrade must revalidate these names.
    internal static class BattleMemorySnapshotExporter
    {
        private const BindingFlags MEMBERS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        internal static IEnumerator Export(string snapshotPath, string outputPath)
        {
            var assembly = Assembly.Load("Unity.MemoryProfiler.Editor");
            var readerType = assembly.GetType("Unity.MemoryProfiler.Editor.Format.QueriedSnapshot.FileReader", true);
            using var reader = (IDisposable)Activator.CreateInstance(readerType, true);
            object error = readerType.GetMethod("Open", MEMBERS).Invoke(reader, new object[] { snapshotPath });
            if (error.ToString() != "Success") throw new IOException("Memory snapshot read failed: " + error);
            var snapshotType = assembly.GetType("Unity.MemoryProfiler.Editor.CachedSnapshot", true);
            using var snapshot = (IDisposable)Activator.CreateInstance(snapshotType, new object[] { reader });
            var processing = (IEnumerator)snapshotType.GetMethod("PostProcess", MEMBERS).Invoke(snapshot, new object[] { true });
            try { while (processing.MoveNext()) yield return null; }
            finally { if (processing is IDisposable disposable) disposable.Dispose(); }

            var totals = new Dictionary<(string Type, string Owner), (long Count, ulong Bytes)>();
            object native = Get(snapshot, "NativeObjects");
            string[] nativeNames = (string[])Get(native, "ObjectName");
            string[] nativeTypes = (string[])Get(Get(snapshot, "NativeTypes"), "TypeName");
            int[] nativeTypeIndices = Values<int>(Get(native, "NativeTypeArrayIndex"));
            ulong[] nativeSizes = Values<ulong>(Get(native, "Size"));
            long[] rootIds = Values<long>(Get(native, "RootReferenceId"));
            object roots = Get(snapshot, "NativeRootReferences");
            long[] ids = Values<long>(Get(roots, "Id"));
            string[] areas = (string[])Get(roots, "AreaName");
            var rootOwners = new Dictionary<long, string>();
            for (int i = 0; i < ids.Length; i++) rootOwners[ids[i]] = areas[i];
            string NativeOwner(long index) => rootOwners.TryGetValue(rootIds[index], out string area)
                ? area + "/" + nativeNames[index] : nativeNames[index];
            for (int i = 0; i < nativeNames.Length; i++)
                Add(totals, "native:" + nativeTypes[nativeTypeIndices[i]], NativeOwner(i), nativeSizes[i]);
            yield return null;

            object managed = Get(snapshot, "CrawledData");
            // Read the backing field because ManagedObjects is a ref-return property.
            object[] objects = ((IEnumerable)Get(managed, "m_ManagedObjects")).Cast<object>().ToArray();
            string[] types = (string[])Get(Get(snapshot, "TypeDescriptions"), "TypeDescriptionName");
            var objectTypes = new string[objects.Length];
            var nativeOwners = new long[objects.Length];
            var sizes = new ulong[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                int type = Convert.ToInt32(Get(objects[i], "ITypeDescription"));
                objectTypes[i] = type >= 0 ? types[type] : "<unknown managed type>";
                nativeOwners[i] = Convert.ToInt64(Get(objects[i], "NativeObjectIndex"));
                sizes[i] = checked((ulong)Convert.ToInt64(Get(objects[i], "Size")));
            }
            var owners = new Dictionary<long, SortedSet<string>>();
            object connections = Get(managed, "Connections");
            var connectionAt = connections.GetType().GetProperty("Item", MEMBERS);
            long connectionCount = Convert.ToInt64(Get(connections, "Count"));
            for (long index = 0; index < connectionCount; index++)
            {
                object connection = connectionAt.GetValue(connections, new object[] { index });
                int target = Convert.ToInt32(Get(connection, "ToManagedObjectIndex"));
                if (target < 0) continue;
                int source = Convert.ToInt32(Get(connection, "FromManagedObjectIndex"));
                int staticType = Convert.ToInt32(Get(connection, "FromManagedType"));
                string owner = source >= 0 ? "referrer:" + objectTypes[source] : staticType >= 0 ? "static:" + types[staticType] : null;
                if (owner == null) continue;
                if (!owners.TryGetValue(target, out var set)) owners.Add(target, set = new SortedSet<string>(StringComparer.Ordinal));
                set.Add(owner);
            }
            for (int i = 0; i < objects.Length; i++)
            {
                string owner = nativeOwners[i] >= 0 ? "native-counterpart:" + NativeOwner(nativeOwners[i]) :
                    owners.TryGetValue(i, out var set) ? string.Join(";", set) : "<no resolved direct owner>";
                Add(totals, "managed:" + objectTypes[i], owner, sizes[i]);
                if (i % 50000 == 0) yield return null;
            }
            BattleProfilerDecoder.WriteJson(outputPath, new { snapshot = snapshotPath, package = "com.unity.memoryprofiler@1.1.12",
                scope = "Shallow native object sizes + crawled managed object sizes. Owner is native counterpart or a sorted set of direct referrer types, not an exclusive retained-size root. Excludes reserved, untracked, graphics and non-object native allocations.",
                types = totals.OrderByDescending(p => p.Value.Bytes).Select(p => new
                { type = p.Key.Type, owner = p.Key.Owner, count = p.Value.Count, bytes = p.Value.Bytes }) });
        }

        private static void Add(Dictionary<(string Type, string Owner), (long Count, ulong Bytes)> totals, string type, string owner, ulong bytes)
        {
            var key = (type, owner);
            totals.TryGetValue(key, out var total);
            totals[key] = (total.Count + 1, total.Bytes + bytes);
        }

        private static T[] Values<T>(object container) => ((IEnumerable)container).Cast<T>().ToArray();

        private static object Get(object target, string name)
        {
            Type type = target.GetType();
            FieldInfo field = type.GetField(name, MEMBERS);
            if (field != null) return field.GetValue(target);
            PropertyInfo property = type.GetProperty(name, MEMBERS);
            if (property == null) throw new MissingMemberException(type.FullName, name);
            return property.GetValue(target);
        }
    }
}
