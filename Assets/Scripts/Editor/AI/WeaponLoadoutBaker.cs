using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.AI
{
    /// <summary>Counts each unit view's WeaponComponent hardpoints per weapon type and caches the result in its
    /// ShipData / SquadronData, so the AI can rate units it has not spawned without loading their prefabs.</summary>
    public static class WeaponLoadoutBaker
    {
        public const string SHIP_DATA_FOLDER = "Assets/Settings/Data/Ship";
        public const string SQUADRON_DATA_FOLDER = "Assets/Settings/Data/Squadron";
        private const string SHIP_VIEW_FOLDER = "Assets/Prefabs/Models/Ships";
        private const string SQUADRON_VIEW_FOLDER = "Assets/Prefabs/Models/Squadrons";
        private const string SHIP_DATA_SUFFIX = "ShipData";
        private const string SHIP_VIEW_SUFFIX = "ShipView";
        private const string SQUADRON_DATA_SUFFIX = "SquadronData";
        private const string SQUADRON_VIEW_SUFFIX = "SquadronView";
        private const string HARD_POINTS_PROPERTY = "hardPoints";
        private const string FIGHTERS_PROPERTY = "fighters";
        private const string LOADOUT_PROPERTY = "weaponLoadout";
        private const string MEMBER_COUNT_PROPERTY = "<MemberCount>k__BackingField";

        [MenuItem("Tools/AI/Bake Weapon Loadouts")]
        public static void BakeAll()
        {
            StringBuilder report = new StringBuilder("Weapon loadouts baked:\n");
            foreach (ShipData data in LoadAll<ShipData>(SHIP_DATA_FOLDER))
            {
                List<WeaponLoadoutEntry> loadout = DeriveLoadout(LoadShipView(data));
                SerializedObject serializedData = new SerializedObject(data);
                WriteLoadout(serializedData.FindProperty(LOADOUT_PROPERTY), loadout);
                serializedData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                report.AppendLine($"{data.name}: {Describe(loadout)}");
            }

            foreach (SquadronData data in LoadAll<SquadronData>(SQUADRON_DATA_FOLDER))
            {
                GameObject view = LoadSquadronView(data);
                List<WeaponLoadoutEntry> loadout = DeriveLoadout(view);
                SerializedObject serializedData = new SerializedObject(data);
                WriteLoadout(serializedData.FindProperty(LOADOUT_PROPERTY), loadout);
                serializedData.FindProperty(MEMBER_COUNT_PROPERTY).intValue = DeriveMemberCount(view);
                serializedData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                report.AppendLine($"{data.name}: {Describe(loadout)}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        public static IEnumerable<T> LoadAll<T>(string folder) where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static GameObject LoadShipView(ShipData data) =>
            LoadView(data.name, SHIP_DATA_SUFFIX, SHIP_VIEW_FOLDER, SHIP_VIEW_SUFFIX);

        public static GameObject LoadSquadronView(SquadronData data) =>
            LoadView(data.name, SQUADRON_DATA_SUFFIX, SQUADRON_VIEW_FOLDER, SQUADRON_VIEW_SUFFIX);

        /// <summary>Weapon counts per type, ordered by type, from the hardpoints the WeaponComponent fires.</summary>
        public static List<WeaponLoadoutEntry> DeriveLoadout(GameObject view)
        {
            // Editor-only inspection of a prefab asset, so the lookup is required.
            WeaponComponent weapons = view.GetComponentInChildren<WeaponComponent>(true);
            if (weapons == null)
                return new List<WeaponLoadoutEntry>();

            SerializedProperty hardPoints = new SerializedObject(weapons).FindProperty(HARD_POINTS_PROPERTY);
            Dictionary<WeaponType, int> counts = new Dictionary<WeaponType, int>();
            for (int i = 0; i < hardPoints.arraySize; i++)
            {
                WeaponHardPoint hardPoint = (WeaponHardPoint)hardPoints.GetArrayElementAtIndex(i).objectReferenceValue;
                if (hardPoint == null)
                    throw new MissingReferenceException($"{view.name} has an empty weapon hardpoint slot {i}.");
                counts.TryGetValue(hardPoint.WeaponType, out int count);
                counts[hardPoint.WeaponType] = count + 1;
            }

            return counts
                .OrderBy(pair => pair.Key)
                .Select(pair => new WeaponLoadoutEntry(pair.Key, pair.Value))
                .ToList();
        }

        public static int DeriveMemberCount(GameObject view)
        {
            SquadronHealthComponent health = view.GetComponentInChildren<SquadronHealthComponent>(true);
            return new SerializedObject(health).FindProperty(FIGHTERS_PROPERTY).arraySize;
        }

        private static GameObject LoadView(string dataName, string dataSuffix, string viewFolder, string viewSuffix)
        {
            string viewName = dataName.Substring(0, dataName.Length - dataSuffix.Length) + viewSuffix;
            string viewPath = $"{viewFolder}/{viewName}.prefab";
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(viewPath);
            if (view == null)
                throw new MissingReferenceException($"{dataName} has no view at {viewPath}.");
            return view;
        }

        private static void WriteLoadout(SerializedProperty property, IReadOnlyList<WeaponLoadoutEntry> loadout)
        {
            property.arraySize = loadout.Count;
            for (int i = 0; i < loadout.Count; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("weaponType").intValue = (int)loadout[i].WeaponType;
                element.FindPropertyRelative("count").intValue = loadout[i].Count;
            }
        }

        private static string Describe(IEnumerable<WeaponLoadoutEntry> loadout) =>
            string.Join(", ", loadout.Select(entry => $"{entry.WeaponType}×{entry.Count}"));
    }
}
