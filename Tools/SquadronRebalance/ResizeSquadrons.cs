using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// One-time authoring operation, invoked through unity command run_script outside Assets.
public static class ResizeSquadrons
{
    private const string PREFAB_FOLDER = "Assets/Prefabs/Models/Squadrons/";
    private const string DATA_FOLDER = "Assets/Settings/Data/Squadron/";

    public static object Apply()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            throw new InvalidOperationException("Resize requires an idle Editor.");
        string[] names = { "XWing", "ARC170", "TIEFighter", "Vulture", "AWing", "VWing",
            "TIEInterceptor", "Belbullab22", "YWingBomber", "YWing", "TIEBomber", "DroidBomber",
            "V19Torrent", "NTB630", "TIEBrute", "TIEAvenger", "TIEPunisher" };
        var report = new List<object>();
        foreach (string name in names) report.Add(Resize(name));
        AssetDatabase.SaveAssets();
        return report;
    }

    private static object Resize(string name)
    {
        string path = PREFAB_FOLDER + name + "SquadronView.prefab";
        SquadronData data = AssetDatabase.LoadAssetAtPath<SquadronData>(DATA_FOLDER + name + "SquadronData.asset");
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var components = BalancePrefabBindings.Components(root).OfType<Component>().ToList();
            EntityBindings bindings = components.OfType<EntityBindings>().Single();
            var fighters = BalancePrefabBindings.References(bindings.SquadronHealth, "fighters").Cast<FighterView>().ToList();
            int previousCount = fighters.Count;
            var originalFighters = fighters.ToArray();
            FighterView template = fighters[0];
            var removed = new HashSet<Object>(fighters.Skip(data.MemberCount)
                .SelectMany(f => Objects(f.gameObject)));
            var maps = new List<Dictionary<Object, Object>>();
            for (int i = previousCount; i < data.MemberCount; i++)
            {
                FighterView clone = Object.Instantiate(template, template.transform.parent);
                clone.name = name + i;
                Object[] sourceObjects = Objects(template.gameObject).ToArray();
                Object[] cloneObjects = Objects(clone.gameObject).ToArray();
                if (sourceObjects.Length != cloneObjects.Length)
                    throw new InvalidOperationException("Clone topology changed for " + name);
                var map = new Dictionary<Object, Object>();
                for (int j = 0; j < sourceObjects.Length; j++) map.Add(sourceObjects[j], cloneObjects[j]);
                maps.Add(map);
                fighters.Add(clone);
            }
            fighters = fighters.Take(data.MemberCount).ToList();
            // Root-side lists include health, flight, all weapon components, fog renderers and DI.
            // Clone-local references are remapped by Instantiate; preserve every non-member binding.
            foreach (Component component in components.Where(c => c is MonoBehaviour && !originalFighters.Any(f => c.transform.IsChildOf(f.transform))))
                RewriteReferences(component, removed, maps);
            foreach (FighterView fighter in originalFighters.Skip(data.MemberCount))
                Object.DestroyImmediate(fighter.gameObject);

            for (int i = 0; i < fighters.Count; i++)
            {
                FighterView fighter = fighters[i];
                using (var serialized = new SerializedObject(fighter))
                {
                    serialized.FindProperty("id").intValue = i;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var slot = SquadronFormation.GetSlot(i, data.FormationSpacing);
                fighter.transform.localPosition = new Vector3(slot.X, slot.Y, slot.Z);
                foreach (FighterTorpedoHardPoint torpedo in BalancePrefabBindings.Components(fighter.gameObject).OfType<FighterTorpedoHardPoint>())
                {
                    using var serialized = new SerializedObject(torpedo);
                    serialized.FindProperty("fighterIndex").intValue = i;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            var mounts = bindings.Weapons.SelectMany(w => BalancePrefabBindings.References(w, "hardPoints"))
                .Cast<WeaponHardPoint>().ToList();
            for (int i = 0; i < mounts.Count; i++)
            {
                using var serialized = new SerializedObject(mounts[i]);
                serialized.FindProperty("<Id>k__BackingField").intValue = i;
                if (name == "YWingBomber" && (int)mounts[i].WeaponType == 72)
                    serialized.FindProperty("<WeaponType>k__BackingField").intValue = 157;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            ValidateBindings(root, fighters, bindings, mounts);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        List<WeaponLoadoutEntry> loadout = WeaponLoadoutBaker.DeriveLoadout(saved);
        using (var serialized = new SerializedObject(data))
        {
            SerializedProperty entries = serialized.FindProperty("weaponLoadout");
            entries.arraySize = loadout.Count;
            for (int i = 0; i < loadout.Count; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("weaponType").intValue = (int)loadout[i].WeaponType;
                entry.FindPropertyRelative("count").intValue = loadout[i].Count;
            }
            serialized.FindProperty("<MemberCount>k__BackingField").intValue = WeaponLoadoutBaker.DeriveMemberCount(saved);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        return new { unit = name, members = data.MemberCount, mounts = loadout.Sum(e => e.Count) };
    }

    private static IEnumerable<Object> Objects(GameObject root)
    {
        yield return root;
        foreach (Component component in BalancePrefabBindings.Components(root).OfType<Component>())
        {
            yield return component;
            if (component is Transform transform && transform != root.transform) yield return transform.gameObject;
        }
    }

    private static void RewriteReferences(Component component, HashSet<Object> removed, List<Dictionary<Object, Object>> maps)
    {
        using var serialized = new SerializedObject(component);
        SerializedProperty property = serialized.GetIterator();
        bool enter = true;
        while (property.Next(enter))
        {
            enter = true;
            if (property.isArray && property.propertyType != SerializedPropertyType.String && property.arraySize > 0
                && property.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.ObjectReference)
            {
                var original = new List<Object>();
                for (int i = 0; i < property.arraySize; i++) original.Add(property.GetArrayElementAtIndex(i).objectReferenceValue);
                var replacement = original.Where(o => !removed.Contains(o)).ToList();
                foreach (var map in maps)
                    foreach (Object reference in original)
                        if (reference != null && map.TryGetValue(reference, out Object clone)) replacement.Add(clone);
                property.arraySize = replacement.Count;
                for (int i = 0; i < replacement.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = replacement[i];
                enter = false;
            }
            else if (property.propertyType == SerializedPropertyType.ObjectReference && removed.Contains(property.objectReferenceValue))
                throw new InvalidOperationException($"Unsupported removed scalar binding: {component.name}/{property.propertyPath}");
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateBindings(GameObject root, List<FighterView> fighters, EntityBindings bindings, List<WeaponHardPoint> mounts)
    {
        if (!BalancePrefabBindings.References(bindings.SquadronHealth, "fighters").SequenceEqual(fighters.Cast<Object>())
            || !BalancePrefabBindings.References(bindings.SquadronFlight, "fighters").SequenceEqual(fighters.Cast<Object>()))
            throw new InvalidOperationException("Health/flight member order mismatch: " + root.name);
        var allMounts = fighters.SelectMany(f => BalancePrefabBindings.Components(f.gameObject)).OfType<WeaponHardPoint>().ToArray();
        if (mounts.Count != allMounts.Length || mounts.Distinct().Count() != mounts.Count || allMounts.Except(mounts).Any())
            throw new InvalidOperationException("Weapon registration mismatch: " + root.name);
        foreach (FighterView fighter in fighters)
        {
            using var serialized = new SerializedObject(fighter);
            var guns = new[] { fighter.Gun }.Concat(BalancePrefabBindings.References(fighter, "additionalGuns").Cast<WeaponHardPoint>());
            if (!guns.ToHashSet().SetEquals(BalancePrefabBindings.Components(fighter.gameObject).OfType<WeaponHardPoint>()))
                throw new InvalidOperationException("Member death weapon bindings incomplete: " + fighter.name);
            if (!bindings.Injectables.Contains(fighter)) throw new InvalidOperationException("Missing fighter injection: " + fighter.name);
        }
        foreach (FighterTorpedoHardPoint mount in mounts.OfType<FighterTorpedoHardPoint>())
            if (!bindings.Injectables.Contains(mount)) throw new InvalidOperationException("Missing weapon injection: " + mount.name);
        foreach (Component component in BalancePrefabBindings.Components(root).OfType<Component>())
        {
            using var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null
                    && property.objectReferenceInstanceIDValue != 0)
                    throw new InvalidOperationException($"Dangling binding: {component.name}/{property.propertyPath}");
        }
    }
}
