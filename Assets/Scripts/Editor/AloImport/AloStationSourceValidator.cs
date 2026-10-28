using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloStationSourceValidator
    {
        public static void Verify(JObject recipe, JObject assembly, AloAssetObjectMap objects)
        {
            foreach (JToken mount in recipe["station"]["mounts"])
                foreach (JToken anchor in mount["anchors"])
                {
                    JToken piece = assembly["pieces"].Single(item => (string)item["id"] == (string)anchor["piece"]);
                    JToken bone = piece["bones"].Single(item => (int)item["index"] == (int)anchor["boneIndex"]);
                    var point = objects.Resolve(anchor["point"]) as Transform;
                    if (point == null) throw new InvalidOperationException("Station source point must be a Transform.");
                    Transform source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(point);
                    if (source == null) source = point;
                    JToken output = recipe["integration"]["outputs"].Single(item => (string)item["role"] == "model-" + (string)anchor["piece"]);
                    if (AssetDatabase.GetAssetPath(source) != (string)output["path"] || source.name != (string)bone["exportedName"])
                        throw new InvalidOperationException("Station anchor does not resolve its original piece/bone index: " + mount["id"]);
                    if (Vector3.Distance(point.localPosition, source.localPosition) > 0.00002f || Quaternion.Angle(point.localRotation, source.localRotation) > 0.00002f ||
                        Vector3.Distance(point.localScale, source.localScale) > 0.00002f)
                        throw new InvalidOperationException("Station imported bone has an unrecorded local transform override: " + mount["id"]);
                    int parent = (int)bone["parentIndex"];
                    if (parent >= 0 && (source.parent == null || source.parent.name != (string)piece["bones"].Single(item => (int)item["index"] == parent)["exportedName"]))
                        throw new InvalidOperationException("Station source bone parent differs: " + mount["id"]);
                }
        }
    }
}
