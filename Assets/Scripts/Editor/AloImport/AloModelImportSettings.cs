using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloModelImportSettings
    {
        public static void Copy(ModelImporter baseline, ModelImporter target, JArray remaps)
        {
            target.globalScale = baseline.globalScale;
            target.useFileScale = baseline.useFileScale;
            target.bakeAxisConversion = baseline.bakeAxisConversion;
            target.importNormals = baseline.importNormals;
            target.importTangents = baseline.importTangents;
            target.meshCompression = baseline.meshCompression;
            target.isReadable = baseline.isReadable;
            target.optimizeMeshPolygons = baseline.optimizeMeshPolygons;
            target.optimizeMeshVertices = baseline.optimizeMeshVertices;
            target.weldVertices = baseline.weldVertices;
            target.preserveHierarchy = baseline.preserveHierarchy;
            target.importBlendShapes = baseline.importBlendShapes;
            target.addCollider = baseline.addCollider;
            target.materialImportMode = baseline.materialImportMode;
            target.materialLocation = baseline.materialLocation;
            target.animationType = baseline.animationType;
            target.importAnimation = baseline.importAnimation;
            target.importCameras = baseline.importCameras;
            target.importLights = baseline.importLights;
            target.importAnimatedCustomProperties = baseline.importAnimatedCustomProperties;
            target.importConstraints = baseline.importConstraints;
            foreach (var identifier in target.GetExternalObjectMap().Keys) target.RemoveRemap(identifier);
            foreach (JToken remap in remaps)
            {
                var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), (string)remap["sourceName"]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath((string)remap["materialGuid"]));
                if (material == null) throw new InvalidOperationException("Missing remap material " + remap["materialGuid"]);
                target.AddRemap(identifier, material);
            }
        }
    }
}
