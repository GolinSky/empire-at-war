using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloBindingWriter
    {
        public static void Preflight(JObject recipe, AloAssetObjectMap objects)
        {
            foreach (JToken binding in recipe["integration"]["bindings"])
            {
                Object component = objects.Resolve(binding["component"]);
                SerializedProperty property = Property(component, (string)binding["property"]);
                Object target = objects.Target(binding);
                if (target == null || property.propertyType != SerializedPropertyType.ObjectReference)
                    throw new InvalidOperationException("Binding requires an existing object-reference property.");
                if (AssetDatabase.GetAssetPath(component).EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: imported FBX components are immutable; bind the owning prefab.");
            }
        }

        public static JArray Apply(JObject recipe, AloAssetObjectMap objects, string jobDirectory)
        {
            AloInvocationPolicy.RequireAssetWrite(jobDirectory, recipe["integration"]["bindings"].Select(binding => AssetDatabase.GetAssetPath(objects.Asset((string)binding["component"]["role"]))));
            Preflight(recipe, objects);
            var changed = new HashSet<string>();
            var saved = new JArray();
            foreach (JToken binding in recipe["integration"]["bindings"])
            {
                Object component = objects.Resolve(binding["component"]);
                var serialized = new SerializedObject(component);
                SerializedProperty property = serialized.FindProperty((string)binding["property"]);
                Object target = objects.Target(binding);
                if (property.objectReferenceValue == target) continue;
                property.objectReferenceValue = target;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
                changed.Add((string)binding["component"]["role"]);
            }
            foreach (string role in changed)
            {
                Object asset = objects.Asset(role);
                if (asset is GameObject prefab)
                {
                    PrefabUtility.SavePrefabAsset(prefab, out bool success);
                    if (!success) throw new InvalidOperationException("Failed to save bound prefab " + role);
                }
                saved.Add(AssetDatabase.GetAssetPath(asset));
            }
            AssetDatabase.SaveAssets();
            return saved;
        }

        private static SerializedProperty Property(Object component, string path)
        {
            if (path.StartsWith("m_", StringComparison.Ordinal)) throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: engine-owned binding fields cannot be rewritten.");
            SerializedProperty property = new SerializedObject(component).FindProperty(path);
            if (property == null) throw new InvalidOperationException("Missing serialized binding property " + path);
            return property;
        }
    }
}
