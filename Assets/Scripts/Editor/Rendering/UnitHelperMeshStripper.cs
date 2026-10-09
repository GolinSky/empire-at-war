using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// Removes the source-model helper meshes (collision, shadow volume, shield, LOD copies) that unit prefabs keep
    /// behind disabled renderers. The FBX and .blend files keep them; only the game prefabs drop them.
    /// A helper goes only when its renderer is disabled in the saved prefab, its GameObject holds nothing but
    /// Transform, MeshFilter and MeshRenderer, it has no children, and nothing but a renderer list references it.
    /// </summary>
    [EmpireAtWar.Editor.EditorToolInfo("Report helper meshes first; Strip removes eligible helper meshes from unit prefabs using the existing tool.")]
    public static class UnitHelperMeshStripper
    {
        private const string REPORT_MENU_PATH = "Tools/Empire At War/Rendering/Materials/Report Helper Meshes In Unit Prefabs";
        private const string STRIP_MENU_PATH = "Tools/Empire At War/Rendering/Materials/Strip Helper Meshes From Unit Prefabs";
        private const string PREFAB_EXTENSION = ".prefab";

        public static readonly string[] UNIT_PREFAB_FOLDERS = { "Assets/Prefabs/Models", "Assets/Prefabs/Ui/Reinforcement" };

        // Only disabled renderers are candidates, so visible geometry with a helper-like name (Hull_LOD_1) is safe.
        private static readonly Regex HELPER_NAME =
            new Regex(@"collis+ion|shadow|shield|lod_?\d|(^|[^a-z])col([^a-z]|$)", RegexOptions.IgnoreCase);

        // Hardpoint blast meshes (HP_Shield_Blast) are effects, not helpers, even when their name matches.
        private static readonly Regex EFFECT_NAME = new Regex("blast", RegexOptions.IgnoreCase);

        // Lists that only enumerate meshes; stripped helpers are removed from them. Any other reference keeps the helper.
        private static readonly HashSet<string> RENDERER_LISTS = new HashSet<string>
        {
            "TeamColorView.meshRenderers", "FogVisibilityComponent.renderers", "UnitWreckView.meshRenderers",
            "UnitWreckView.meshFilters", "UnitSpawnView.meshRenderers", "ExplosionVfx.renderers",
            "Ship.explosionHullRenderers", "SpaceStation.explosionHullRenderers",
            "DefendPlatform.explosionHullRenderers", "MiningFacility.explosionHullRenderers"
        };

        private static readonly HashSet<string> IGNORED_PROPERTIES = new HashSet<string>
        {
            "m_GameObject", "m_Script", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset"
        };

        public sealed class Helper
        {
            public string OwnerPath;
            public string HierarchyPath;
            public string Name;
            public int Triangles;
            /// <summary>Why the helper must stay; null when it can be stripped.</summary>
            public string BlockedBy;
        }

        [MenuItem(REPORT_MENU_PATH)]
        public static void ReportUnitPrefabs() =>
            Debug.Log(Report(FindHelpers(FindUnitPrefabPaths(), new string[0])));

        [MenuItem(STRIP_MENU_PATH)]
        public static void StripUnitPrefabs() => Debug.Log(Strip(FindUnitPrefabPaths(), new string[0]));

        public static string[] FindUnitPrefabPaths() =>
            AssetDatabase.FindAssets("t:Prefab", UNIT_PREFAB_FOLDERS).Select(AssetDatabase.GUIDToAssetPath).ToArray();

        /// <summary>
        /// Lists every disabled helper renderer owned by the given prefabs. Locked prefabs are scanned for references
        /// but never edited, so helpers they own or reference stay.
        /// </summary>
        public static List<Helper> FindHelpers(IReadOnlyCollection<string> prefabPaths, IReadOnlyCollection<string> lockedPaths)
        {
            var locked = new HashSet<string>(lockedPaths);
            Dictionary<Object, List<(string Field, string Path)>> references = CollectReferences(prefabPaths);
            var helpers = new List<Helper>();
            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
                {
                    GameObject helperObject = renderer.gameObject;
                    if (renderer.enabled || !HELPER_NAME.IsMatch(helperObject.name) ||
                        EFFECT_NAME.IsMatch(helperObject.name) || IsOwnedByNestedPrefab(helperObject))
                    {
                        continue;
                    }

                    Component[] components = helperObject.GetComponents<Component>();
                    MeshFilter filter = components.OfType<MeshFilter>().FirstOrDefault();
                    helpers.Add(new Helper
                    {
                        OwnerPath = path,
                        HierarchyPath = GetHierarchyPath(helperObject.transform, prefab.transform),
                        Name = helperObject.name,
                        Triangles = CountTriangles(filter),
                        BlockedBy = FindBlocker(path, locked, helperObject, components, references)
                    });
                }
            }

            return helpers;
        }

        /// <summary>
        /// Removes every strippable helper and its renderer-list entries, then returns a report. Repeats until
        /// nothing is left, because removing a helper's last child can turn its parent helper into a leaf.
        /// </summary>
        public static string Strip(IReadOnlyCollection<string> prefabPaths, IReadOnlyCollection<string> lockedPaths)
        {
            var report = new StringBuilder();
            while (StripPass(prefabPaths, lockedPaths, report) > 0) { }
            return report.ToString();
        }

        private static int StripPass(IReadOnlyCollection<string> prefabPaths, IReadOnlyCollection<string> lockedPaths,
            StringBuilder report)
        {
            List<Helper> helpers = FindHelpers(prefabPaths, lockedPaths);
            List<Helper> strippable = helpers.Where(helper => helper.BlockedBy == null).ToList();
            var keys = new HashSet<string>(strippable.Select(helper => GetKey(helper.OwnerPath, helper.HierarchyPath)));

            // List entries go first: once an owner drops a helper, outer prefabs can no longer resolve their entries.
            int listEntries = 0;
            foreach (string path in prefabPaths.Except(lockedPaths))
            {
                listEntries += EditPrefab(path, root => RemoveListEntries(root, path, keys));
            }

            int removed = 0;
            foreach (IGrouping<string, Helper> owner in strippable.GroupBy(helper => helper.OwnerPath))
            {
                removed += EditPrefab(owner.Key, root => DestroyHelpers(root, owner.ToList()));
            }

            AssetDatabase.SaveAssets();
            report.AppendLine($"[HelperMeshes] removed {removed} helpers and {listEntries} renderer-list entries")
                .Append(Report(helpers));
            return removed;
        }

        public static string Report(IReadOnlyCollection<Helper> helpers)
        {
            var report = new StringBuilder();
            report.AppendLine($"[HelperMeshes] {helpers.Count(h => h.BlockedBy == null)} strippable " +
                              $"({helpers.Where(h => h.BlockedBy == null).Sum(h => h.Triangles)} triangles), " +
                              $"{helpers.Count(h => h.BlockedBy != null)} blocked");
            foreach (Helper helper in helpers.OrderBy(h => h.BlockedBy == null).ThenBy(h => h.OwnerPath))
            {
                report.AppendLine($"  {(helper.BlockedBy == null ? "strip" : "KEEP ")} {helper.OwnerPath} :: " +
                                  $"{helper.HierarchyPath} ({helper.Triangles} tris){(helper.BlockedBy == null ? "" : " — " + helper.BlockedBy)}");
            }

            return report.ToString();
        }

        private static string FindBlocker(string path, HashSet<string> locked, GameObject helperObject,
            Component[] components, Dictionary<Object, List<(string Field, string Path)>> references)
        {
            if (locked.Contains(path)) return "owner prefab is locked";
            if (helperObject.transform.childCount > 0) return "has children";
            string extra = string.Join(", ", components
                .Where(component => !(component is Transform || component is MeshFilter || component is MeshRenderer))
                .Select(component => component == null ? "missing script" : component.GetType().Name));
            if (extra.Length > 0) return "has " + extra;

            foreach (Object target in components.Cast<Object>().Append(helperObject))
            {
                if (!references.TryGetValue(target, out List<(string Field, string Path)> users)) continue;
                foreach ((string field, string userPath) in users)
                {
                    if (!RENDERER_LISTS.Contains(field)) return $"referenced by {field} in {userPath}";
                    if (locked.Contains(userPath)) return $"listed by {field} in locked {userPath}";
                }
            }

            return null;
        }

        // Every object reference in the prefabs, resolved through nested prefab sources to the owning asset object.
        private static Dictionary<Object, List<(string Field, string Path)>> CollectReferences(IEnumerable<string> prefabPaths)
        {
            var references = new Dictionary<Object, List<(string Field, string Path)>>();
            foreach (string path in prefabPaths)
            {
                foreach (Component component in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component is Transform) continue;
                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference ||
                            property.objectReferenceValue == null || IGNORED_PROPERTIES.Contains(property.name))
                        {
                            continue;
                        }

                        string field = component.GetType().Name + "." + property.propertyPath.Split('.')[0];
                        for (Object target = property.objectReferenceValue; target != null;
                             target = PrefabUtility.GetCorrespondingObjectFromSource(target))
                        {
                            if (!references.TryGetValue(target, out List<(string Field, string Path)> users))
                            {
                                users = new List<(string Field, string Path)>();
                                references.Add(target, users);
                            }

                            users.Add((field, path));
                        }
                    }
                }
            }

            return references;
        }

        private static int RemoveListEntries(GameObject root, string path, HashSet<string> keys)
        {
            int removed = 0;
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                foreach (string list in RENDERER_LISTS.Where(list => list.StartsWith(component.GetType().Name + ".")))
                {
                    SerializedProperty entries = serialized.FindProperty(list.Substring(list.IndexOf('.') + 1));
                    if (entries == null || !entries.isArray) continue;
                    for (int i = entries.arraySize - 1; i >= 0; i--)
                    {
                        Object target = entries.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (target == null || !GetKeys(target, path, root.transform).Overlaps(keys)) continue;
                        entries.GetArrayElementAtIndex(i).objectReferenceValue = null;
                        entries.DeleteArrayElementAtIndex(i);
                        removed++;
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return removed;
        }

        private static int DestroyHelpers(GameObject root, List<Helper> helpers)
        {
            var transforms = new Dictionary<string, Transform>();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                transforms[GetHierarchyPath(child, root.transform)] = child;
            }

            // Collect before destroying: removing a sibling shifts the indices the paths are built from.
            List<GameObject> targets = helpers.Select(helper => transforms[helper.HierarchyPath].gameObject).ToList();
            foreach (GameObject target in targets)
            {
                Object.DestroyImmediate(target);
            }

            return targets.Count;
        }

        private static int EditPrefab(string path, System.Func<GameObject, int> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int changes = edit(root);
                if (changes > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                return changes;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The key of the object in the edited prefab itself plus the keys of every nested prefab object it comes from.
        private static HashSet<string> GetKeys(Object target, string path, Transform root)
        {
            GameObject targetObject = target as GameObject ?? ((Component)target).gameObject;
            var keys = new HashSet<string> { GetKey(path, GetHierarchyPath(targetObject.transform, root)) };
            for (GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(targetObject); source != null;
                 source = PrefabUtility.GetCorrespondingObjectFromSource(source))
            {
                string sourcePath = AssetDatabase.GetAssetPath(source);
                if (sourcePath.EndsWith(PREFAB_EXTENSION))
                {
                    keys.Add(GetKey(sourcePath, GetHierarchyPath(source.transform, source.transform.root)));
                }
            }

            return keys;
        }

        private static bool IsOwnedByNestedPrefab(GameObject gameObject)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            return source != null && AssetDatabase.GetAssetPath(source).EndsWith(PREFAB_EXTENSION);
        }

        // Sibling indices keep repeated names (Bshadow.002 under several turrets) apart.
        private static string GetHierarchyPath(Transform transform, Transform root)
        {
            var parts = new List<string>();
            for (Transform current = transform; current != root; current = current.parent)
            {
                parts.Add(current.name + "#" + current.GetSiblingIndex());
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string GetKey(string path, string hierarchyPath) => path + "::" + hierarchyPath;

        private static int CountTriangles(MeshFilter filter)
        {
            if (filter == null || filter.sharedMesh == null) return 0;
            Mesh mesh = filter.sharedMesh;
            long indices = 0;
            for (int i = 0; i < mesh.subMeshCount; i++) indices += mesh.GetIndexCount(i);
            return (int)(indices / 3);
        }
    }
}
