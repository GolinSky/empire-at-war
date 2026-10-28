using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloExistingStationBuilder
    {
        private const float ALIGNMENT_TOLERANCE = 0.01f;

        public static JObject Verify(JObject recipe, JObject assembly)
        {
            Process(recipe, assembly, null, true);
            return new JObject { ["levelRouting"] = "passed", ["sourceAnchors"] = "passed", ["commonAlignment"] = "passed", ["sharedGameplayData"] = "preserved", ["runtime"] = "not-run" };
        }

        public static JObject Apply(JObject recipe, JObject assembly, string jobDirectory)
        {
            AloInvocationPolicy.RequireAssetWrite(jobDirectory, recipe["integration"]["outputs"].Select(output => (string)output["path"]));
            Process(recipe, assembly, null, false);
            JObject result = Process(recipe, assembly, jobDirectory, false);
            Verify(recipe, assembly);
            return result;
        }

        public static void Preflight(JObject recipe, JObject assembly) => Process(recipe, assembly, null, false);

        private static JObject Process(JObject recipe, JObject assembly, string jobDirectory, bool requireSaved)
        {
            AloRecipeSchemaValidator.Validate(recipe);
            if ((string)recipe["kind"] != "station" || (string)recipe["operation"] != "update" || (string)recipe["integration"]["bindingProfile"] != "existing-station-assets-v1")
                throw new InvalidOperationException("Existing station adapter requires an explicit existing-station-assets-v1 update recipe.");
            var objects = new AloAssetObjectMap(recipe);
            JToken station = recipe["station"];
            var view = objects.Resolve(station["viewComponent"]) as StationLevelView;
            if (view == null) throw new InvalidOperationException("Station viewComponent must identify StationLevelView.");
            string baselineGuid = (string)recipe["gameplay"]["baselineGuid"];
            var data = AssetDatabase.LoadAssetAtPath<SpaceStationData>(AssetDatabase.GUIDToAssetPath(baselineGuid));
            if (data == null || recipe["gameplay"]["values"].Any()) throw new InvalidOperationException("Station updates preserve an existing shared SpaceStationData baseline.");
            AloStationSourceValidator.Verify(recipe, assembly, objects);
            AloBindingWriter.Preflight(recipe, objects);
            var serializedView = new SerializedObject(view);
            SerializedProperty routed = serializedView.FindProperty("levelModels");
            JToken[] levels = station["levels"].ToArray();
            if (routed.arraySize != levels.Length) throw new InvalidOperationException("Station level route count differs.");
            var hardpoints = recipe["hardpoints"].ToDictionary(point => (int)point["id"], point => (HardPoint)objects.Resolve(point["component"]));
            if (!station["mounts"].Select(mount => (int)mount["hardpoint"]).OrderBy(id => id).SequenceEqual(hardpoints.Keys.OrderBy(id => id)))
                throw new InvalidOperationException("Station mounts must list each gameplay hardpoint exactly once.");
            var written = new HashSet<string>();
            Vector3 alignment = Vector3.zero;
            var boundsByLevel = new Dictionary<int, Bounds>();
            foreach (JToken level in levels)
            {
                int number = (int)level["level"];
                if ((string)level["gameplayDataGuid"] != baselineGuid) throw new InvalidOperationException("Station level data differs from its shared baseline.");
                data.GetLevelStats(number);
                var model = objects.Resolve(level["component"]) as StationLevelModel;
                var runtime = routed.GetArrayElementAtIndex(number - 1).objectReferenceValue as StationLevelModel;
                if (model == null || runtime == null || (runtime != model && PrefabUtility.GetCorrespondingObjectFromSource(runtime) != model))
                    throw new InvalidOperationException("Station level routing does not identify its declared saved model.");
                Matrix4x4 toView = view.transform.worldToLocalMatrix * runtime.transform.localToWorldMatrix;
                for (int element = 0; element < 16; element++)
                    if (Mathf.Abs(toView[element] - Matrix4x4.identity[element]) > 0.00002f) throw new InvalidOperationException("Station level metadata requires a shared identity root in view space.");
                var point = (Transform)objects.Resolve(level["alignmentPoint"]);
                Vector3 localAlignment = model.transform.InverseTransformPoint(point.position);
                if (number == 1) alignment = localAlignment;
                else if (Vector3.Distance(alignment, localAlignment) > ALIGNMENT_TOLERANCE) throw new InvalidOperationException("Station levels do not share the declared alignment anchor.");
                Renderer[] renderers = level["hullRenderers"].Select(reference => (Renderer)objects.Resolve(reference)).ToArray();
                Bounds bounds = Measure(model.transform, renderers, objects, (string)level["component"]["role"]);
                boundsByLevel.Add(number, bounds);
                if (model.ShieldMesh == null || model.ShieldPlanes == null || model.ShieldPlanes.Length == 0) throw new InvalidOperationException("Station level lacks persisted fitted shield data.");
                Transform launch = (Transform)objects.Resolve(level["launchExit"]);
                var mounts = new List<(int Id, Transform Point, GameObject Art)>();
                foreach (JToken mount in station["mounts"].Where(mount => (int)mount["unlockLevel"] <= number))
                {
                    int id = (int)mount["hardpoint"];
                    HardPoint hardpoint = hardpoints[id];
                    if (hardpoint.Id != id || hardpoint.UnlockLevel != (int)mount["unlockLevel"] || mount["anchors"].Min(anchor => (int)anchor["level"]) != hardpoint.UnlockLevel)
                        throw new InvalidOperationException("Station hardpoint identity/unlock differs: " + id);
                    JToken declared = recipe["hardpoints"].Single(entry => (int)entry["id"] == id);
                    if (hardpoint is WeaponHardPoint weapon && (int)weapon.WeaponType != (int)declared["profileKey"]) throw new InvalidOperationException("Station weapon type differs: " + id);
                    JToken anchor = mount["anchors"].Where(item => (int)item["level"] <= number).OrderBy(item => (int)item["level"]).Last();
                    Transform target = (Transform)objects.Resolve(anchor["point"]);
                    GameObject art = anchor["art"].Type == JTokenType.Null ? null : (GameObject)objects.Resolve(anchor["art"]);
                    mounts.Add((id, target, art));
                }
                if (requireSaved)
                {
                    if (Vector3.Distance(model.HullBounds.center, bounds.center) > ALIGNMENT_TOLERANCE || Vector3.Distance(model.HullBounds.size, bounds.size) > ALIGNMENT_TOLERANCE ||
                        !model.HullRenderers.SequenceEqual(renderers) || model.LaunchExit != launch || model.Mounts.Length != mounts.Count)
                        throw new InvalidOperationException("Persisted station level metadata differs: " + number);
                    foreach (var mount in mounts)
                    {
                        StationMount saved = model.Mounts.Single(item => item.HardPointId == mount.Id);
                        if (saved.Point != mount.Point || saved.Art != mount.Art) throw new InvalidOperationException("Persisted station mount differs: " + mount.Id);
                    }
                }
                if (jobDirectory != null)
                {
                    string role = (string)level["component"]["role"];
                    string path = AssetDatabase.GetAssetPath(objects.Asset(role));
                    AloPublicationJournal.Prepare(jobDirectory, new[] { path });
                    WriteModel(model, bounds, renderers, launch, mounts);
                    if (runtime != model)
                    {
                        string viewRole = (string)station["viewComponent"]["role"];
                        AloPublicationJournal.Prepare(jobDirectory, new[] { AssetDatabase.GetAssetPath(objects.Asset(viewRole)) });
                        WriteModel(runtime, bounds, renderers.Select(renderer => Counterpart(renderer, runtime, objects, viewRole)).ToArray(),
                            Counterpart(launch, runtime, objects, viewRole), mounts.Select(mount => (mount.Id, Counterpart(mount.Point, runtime, objects, viewRole),
                                mount.Art == null ? null : Counterpart(mount.Art, runtime, objects, viewRole))).ToList());
                        written.Add(viewRole);
                    }
                    written.Add(role);
                }
            }
            if ((string)recipe["dimensions"]["sizeRule"] != "reference-level-diameter") throw new InvalidOperationException("Station update requires an explicit reference-level-diameter size rule.");
            Bounds referenceBounds = boundsByLevel[(int)recipe["dimensions"]["referenceLevel"]];
            if (Mathf.Abs(Mathf.Max(referenceBounds.size.x, referenceBounds.size.z) - (float)recipe["dimensions"]["targetDiameter"]) > ALIGNMENT_TOLERANCE)
                throw new InvalidOperationException("Station reference-level diameter differs from its current geometry.");
            if (jobDirectory != null)
            {
                AloBindingWriter.Apply(recipe, objects, jobDirectory);
                foreach (string role in written)
                {
                    PrefabUtility.SavePrefabAsset((GameObject)objects.Asset(role), out bool success);
                    if (!success) throw new InvalidOperationException("Failed to save station level role " + role);
                }
                AssetDatabase.SaveAssets();
                AloPublicationJournal.Checkpoint(jobDirectory, "stationLevelsSaved", written.Select(role => AssetDatabase.GetAssetPath(objects.Asset(role))));
            }
            return new JObject { ["levels"] = levels.Length, ["savedRoles"] = new JArray(written), ["fittedShields"] = "retained-under-candidate-geometry-parity" };
        }

        private static T Counterpart<T>(T source, StationLevelModel runtime, AloAssetObjectMap objects, string role) where T : Object =>
            objects.All(role).OfType<T>().Single(item => PrefabUtility.GetCorrespondingObjectFromSource(item) == source &&
                (item is Component component ? component.transform.IsChildOf(runtime.transform) : ((GameObject)(Object)item).transform.IsChildOf(runtime.transform)));

        private static Bounds Measure(Transform root, Renderer[] renderers, AloAssetObjectMap objects, string role)
        {
            var points = new List<Vector3>();
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled) throw new InvalidOperationException("Station hull renderer list contains disabled helper geometry.");
                MeshFilter filter = objects.All(role).OfType<MeshFilter>().Single(item => item.gameObject == renderer.gameObject);
                foreach (Vector3 vertex in filter.sharedMesh.vertices) points.Add(root.InverseTransformPoint(renderer.transform.TransformPoint(vertex)));
            }
            if (points.Count == 0) throw new InvalidOperationException("Station level has no declared hull geometry.");
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 point in points) bounds.Encapsulate(point);
            return bounds;
        }

        private static void WriteModel(StationLevelModel model, Bounds bounds, Renderer[] renderers, Transform launch, List<(int Id, Transform Point, GameObject Art)> mounts)
        {
            var serialized = new SerializedObject(model);
            serialized.FindProperty("<HullBounds>k__BackingField").boundsValue = bounds;
            SerializedProperty hull = serialized.FindProperty("<HullRenderers>k__BackingField");
            hull.arraySize = renderers.Length;
            for (int index = 0; index < renderers.Length; index++) hull.GetArrayElementAtIndex(index).objectReferenceValue = renderers[index];
            serialized.FindProperty("<LaunchExit>k__BackingField").objectReferenceValue = launch;
            SerializedProperty entries = serialized.FindProperty("<Mounts>k__BackingField");
            entries.arraySize = mounts.Count;
            for (int index = 0; index < mounts.Count; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("<HardPointId>k__BackingField").intValue = mounts[index].Id;
                entry.FindPropertyRelative("<Point>k__BackingField").objectReferenceValue = mounts[index].Point;
                entry.FindPropertyRelative("<Art>k__BackingField").objectReferenceValue = mounts[index].Art;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(model);
        }
    }
}
