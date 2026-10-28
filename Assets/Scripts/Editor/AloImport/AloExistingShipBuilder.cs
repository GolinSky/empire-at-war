using System;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloExistingShipBuilder
    {
        private const float TRANSFORM_TOLERANCE = 0.00002f;

        public static void Preflight(JObject recipe)
        {
            AloRecipeSchemaValidator.Validate(recipe);
            if ((string)recipe["operation"] != "update" || (string)recipe["kind"] != "ship" || (string)recipe["integration"]["bindingProfile"] != "existing-assets-v1")
                throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: this adapter updates existing ship assets only.");
            var objects = new AloAssetObjectMap(recipe);
            AloBindingWriter.Preflight(recipe, objects);
            VerifyGameplay(recipe, objects);
        }

        public static JObject ApplyBindings(JObject recipe, string jobDirectory)
        {
            AloInvocationPolicy.RequireAssetWrite(jobDirectory, recipe["integration"]["outputs"].Select(output => (string)output["path"]));
            Preflight(recipe);
            var objects = new AloAssetObjectMap(recipe);
            string[] paths = recipe["integration"]["bindings"].Select(binding => AssetDatabase.GetAssetPath(objects.Asset((string)binding["component"]["role"]))).Distinct().ToArray();
            AloPublicationJournal.Prepare(jobDirectory, paths);
            JArray saved = AloBindingWriter.Apply(recipe, objects, jobDirectory);
            AloPublicationJournal.Checkpoint(jobDirectory, "bindingsSaved", paths);
            VerifyGameplay(recipe, new AloAssetObjectMap(recipe));
            return new JObject { ["savedBindings"] = saved, ["gameplayBaseline"] = "preserved", ["hardpoints"] = recipe["hardpoints"].Count() };
        }

        public static JObject Verify(JObject recipe)
        {
            Preflight(recipe);
            return new JObject { ["hardpointIdentityAndBindings"] = "passed", ["gameplayBaseline"] = "passed", ["hangarCatalogResolution"] = "passed", ["runtime"] = "not-run" };
        }

        private static void VerifyGameplay(JObject recipe, AloAssetObjectMap objects)
        {
            string baseline = AssetDatabase.GUIDToAssetPath((string)recipe["gameplay"]["baselineGuid"]);
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(baseline);
            if (data == null) throw new InvalidOperationException("Existing ship requires a saved ShipData baseline.");
            if (recipe["gameplay"]["values"].Any()) throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: existing-assets-v1 preserves gameplay values; rebalance requires a separate adapter.");
            EntityBindings bindings = objects.All("view").OfType<EntityBindings>().Single();
            var health = bindings.Health.Single();
            HardPoint[] declared = recipe["hardpoints"].Select(point =>
            {
                if (point["component"] == null) throw new InvalidOperationException("Existing hardpoint requires canonical component role/fileId.");
                var component = objects.Resolve(point["component"]) as HardPoint;
                if (component == null || component.Id != (int)point["id"]) throw new InvalidOperationException("Hardpoint identity differs: " + point["id"]);
                bool targetable = health.ShipUnits.Contains(component);
                if (targetable != (bool)point["targetable"] || !bindings.Injectables.Contains(component)) throw new InvalidOperationException("Hardpoint targeting/injection differs: " + component.Id);
                float expectedHealth = component.HealthOverride > 0 ? component.HealthOverride : data.HardPointHealth.Single(entry => entry.HardPointType == component.HardPointType).Health;
                if (Mathf.Abs(expectedHealth - (float)point["health"]) > TRANSFORM_TOLERANCE) throw new InvalidOperationException("Hardpoint health differs: " + component.Id);
                if (point["anchor"]["localPosition"] == null) throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: existing-assets-v1 requires authored local anchors.");
                if (Vector3.Distance(component.transform.localPosition, Vector(point["anchor"]["localPosition"])) > TRANSFORM_TOLERANCE ||
                    Quaternion.Angle(component.transform.localRotation, Quaternion.Euler(Vector(point["anchor"]["localEulerDegrees"]))) > TRANSFORM_TOLERANCE)
                    throw new InvalidOperationException("Hardpoint anchor differs: " + component.Id);
                if (component is WeaponHardPoint weapon)
                {
                    if ((string)point["kind"] != "weapon" || (int)weapon.WeaponType != (int)point["profileKey"] ||
                        Mathf.Abs(weapon.MinYaw - (float)point["yawRangeDegrees"][0]) > TRANSFORM_TOLERANCE ||
                        Mathf.Abs(weapon.MaxYaw - (float)point["yawRangeDegrees"][1]) > TRANSFORM_TOLERANCE) throw new InvalidOperationException("Weapon profile/arc differs: " + component.Id);
                    var catalog = AssetDatabase.LoadAssetAtPath<WeaponsData>(AssetDatabase.GUIDToAssetPath((string)point["profileGuid"]));
                    if (catalog == null) throw new InvalidOperationException("Missing weapon catalog");
                    catalog.GetProfile(weapon.WeaponType);
                }
                return component;
            }).ToArray();
            if (objects.All("view").OfType<HardPoint>().Except(declared).Any()) throw new InvalidOperationException("Saved view contains undeclared hardpoints.");
            foreach (var owner in bindings.Weapons)
            {
                var list = new SerializedObject(owner).FindProperty("hardPoints");
                for (int index = 0; index < list.arraySize; index++)
                    if (!declared.Contains((HardPoint)list.GetArrayElementAtIndex(index).objectReferenceValue)) throw new InvalidOperationException("Weapon list contains undeclared hardpoint.");
            }
            if (data.HangarBays.Count != recipe["hangar"].Count()) throw new InvalidOperationException("Hangar bay count differs.");
            if (recipe["hangar"].Any() && recipe["registrations"] == null)
                throw new InvalidOperationException("Existing ship hangars require the canonical AssetMappingData registration contract.");
            foreach (JToken hangar in recipe["hangar"])
            {
                if ((string)hangar["squadronGuid"] != (string)recipe["registrations"]["assetMappingGuid"])
                    throw new InvalidOperationException("Hangar catalog differs from the unit's canonical AssetMappingData registration catalog.");
                AloSquadronCatalogVerifier.Resolve(hangar);
                var bay = data.HangarBays.Single(item => (int)item.SquadronType == (int)hangar["squadronKey"]);
                if (bay.Reserve != (int)hangar["reserve"] || bay.MaxActive != (int)hangar["capacity"] ||
                    data.HangarInitialDelay != (float)hangar["initialDelaySeconds"] || data.HangarLaunchInterval != (float)hangar["intervalSeconds"])
                    throw new InvalidOperationException("Hangar values differ: " + hangar["id"]);
            }
            if (data.BodyRotationMaxAngle != (float)recipe["dimensions"]["bankingDegrees"]) throw new InvalidOperationException("Banking angle differs.");
        }

        private static Vector3 Vector(JToken values) => new Vector3((float)values[0], (float)values[1], (float)values[2]);
    }
}
