using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloSavedAssetVerifier
    {
        public static JObject Verify(JObject recipe)
        {
            AloRecipeSchemaValidator.Validate(recipe);
            var checks = new JArray();
            var objects = new Dictionary<string, Dictionary<long, Object>>();
            foreach (JToken output in recipe["integration"]["outputs"])
            {
                string path = (string)output["path"];
                string actualGuid = AssetDatabase.AssetPathToGUID(path);
                if (!File.Exists(path) || string.IsNullOrEmpty(actualGuid)) throw new InvalidOperationException("Missing persisted output " + path);
                if (output["guid"].Type != JTokenType.Null && (string)output["guid"] != actualGuid)
                    throw new InvalidOperationException("Output GUID changed " + path);
                Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null) throw new InvalidOperationException("Output failed to import " + path);
                foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                    if (AssetDatabase.LoadMainAssetAtPath(dependency) == null) throw new InvalidOperationException("Broken dependency " + dependency);
                var components = new List<Object> { asset };
                if (asset is GameObject root) Collect(root.transform, components);
                var identities = new Dictionary<long, Object>();
                foreach (Object component in components)
                {
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out string _, out long id))
                        throw new InvalidOperationException("Missing persistent object identity " + component.name);
                    identities[id] = component;
                }
                objects.Add((string)output["role"], identities);
                checks.Add(new JObject { ["path"] = path, ["guid"] = actualGuid, ["state"] = "passed" });
            }
            foreach (JToken binding in recipe["integration"]["bindings"])
            {
                Object component = objects[(string)binding["component"]["role"]][(long)binding["component"]["fileId"]];
                SerializedProperty property = new SerializedObject(component).FindProperty((string)binding["property"]);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                    throw new InvalidOperationException("Missing mandatory binding " + binding["component"] + "." + binding["property"]);
                if (binding["targetGuid"] != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(property.objectReferenceValue));
                    if (guid != (string)binding["targetGuid"]) throw new InvalidOperationException("Binding target differs " + binding["property"]);
                }
                else if (property.objectReferenceValue != objects[(string)binding["target"]["role"]][(long)binding["target"]["fileId"]])
                    throw new InvalidOperationException("Local binding target differs " + binding["property"]);
            }
            return new JObject
            {
                ["outputs"] = checks, ["bindings"] = recipe["integration"]["bindings"].Count(),
                ["persistedOutputs"] = "passed", ["explicitBindings"] = "passed", ["integration"] = "not-run",
                ["sourceFidelity"] = "not-run", ["visual"] = "needs-review", ["runtime"] = "not-run"
            };
        }

        private static void Collect(Transform node, List<Object> components)
        {
            components.Add(node.gameObject);
            SerializedProperty serializedComponents = new SerializedObject(node.gameObject).FindProperty("m_Component");
            for (int index = 0; index < serializedComponents.arraySize; index++)
            {
                Object component = serializedComponents.GetArrayElementAtIndex(index).FindPropertyRelative("component").objectReferenceValue;
                if (component == null) throw new InvalidOperationException("Missing script in " + node.name);
                components.Add(component);
            }
            for (int child = 0; child < node.childCount; child++) Collect(node.GetChild(child), components);
        }
    }
}
