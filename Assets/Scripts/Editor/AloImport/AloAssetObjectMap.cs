using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.AloImport
{
    public sealed class AloAssetObjectMap
    {
        private readonly Dictionary<string, Dictionary<long, Object>> _objects = new Dictionary<string, Dictionary<long, Object>>();
        private readonly Dictionary<string, Object> _assets = new Dictionary<string, Object>();

        public AloAssetObjectMap(JObject recipe, IEnumerable<string> roles = null)
        {
            foreach (JToken output in recipe["integration"]["outputs"])
            {
                string role = (string)output["role"];
                if (roles != null && !roles.Contains(role)) continue;
                string path = (string)output["path"];
                bool plannedCreate = output["guid"].Type == JTokenType.Null && (string)recipe["operation"] == "create";
                if (!plannedCreate && (output["guid"].Type == JTokenType.Null || AssetDatabase.AssetPathToGUID(path) != (string)output["guid"]))
                    throw new InvalidOperationException("INPUT_CHANGED: existing output GUID differs: " + path);
                Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null) throw new InvalidOperationException("Missing imported output " + path);
                var objects = new Dictionary<long, Object>();
                foreach (Object child in AssetDatabase.LoadAllAssetsAtPath(path)) Add(child, objects);
                if (asset is GameObject root) Collect(root.transform, objects);
                _assets.Add(role, asset);
                _objects.Add(role, objects);
            }
        }

        public Object Resolve(JToken reference) => _objects[(string)reference["role"]][(long)reference["fileId"]];

        public Object Asset(string role) => _assets[role];

        public IEnumerable<Object> All(string role) => _objects[role].Values;

        public Object Target(JToken binding)
        {
            if (binding["target"] != null) return Resolve(binding["target"]);
            string guid = (string)binding["targetGuid"];
            Object target = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (target == null) throw new InvalidOperationException("Missing binding target " + guid);
            return target;
        }

        private static void Collect(Transform node, Dictionary<long, Object> objects)
        {
            Add(node.gameObject, objects);
            SerializedProperty components = new SerializedObject(node.gameObject).FindProperty("m_Component");
            for (int index = 0; index < components.arraySize; index++)
            {
                Object component = components.GetArrayElementAtIndex(index).FindPropertyRelative("component").objectReferenceValue;
                if (component == null) throw new InvalidOperationException("Missing script in " + node.name);
                Add(component, objects);
            }
            for (int child = 0; child < node.childCount; child++) Collect(node.GetChild(child), objects);
        }

        private static void Add(Object asset, Dictionary<long, Object> objects)
        {
            if (asset == null) throw new InvalidOperationException("Missing persistent subasset");
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long id))
                throw new InvalidOperationException("Missing persistent object identity " + asset.name);
            if (objects.TryGetValue(id, out Object existing) && existing != asset)
                throw new InvalidOperationException("Ambiguous persistent object identity " + id);
            objects[id] = asset;
        }
    }
}
