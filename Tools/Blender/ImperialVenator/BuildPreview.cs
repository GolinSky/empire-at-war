using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildImperialVenatorPreview
{
    public static string Main()
    {
        const string PATH = "Assets/Prefabs/Ui/Reinforcement/ImperialVenatorReinforcementView.prefab";
        if (!File.Exists(PATH)) AssetDatabase.CopyAsset("Assets/Prefabs/Ui/Reinforcement/VenatorReinforcementView.prefab", PATH);
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/Reinforcement/VenatorReinforcementView.prefab");
        try
        {
            root.name = "ImperialVenatorReinforcementView"; root.transform.localPosition = Vector3.zero; root.transform.localRotation = Quaternion.identity; root.transform.localScale = Vector3.one;
            foreach (var child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ImperialVenator.prefab"), root.transform);
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.enabled = renderers.Contains(r);
            foreach (var r in renderers) r.sharedMaterials = Enumerable.Repeat(hologram, r.sharedMaterials.Length).ToArray();
            var so = new SerializedObject(root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "UnitSpawnView"));
            so.FindProperty("hologramMaterial").objectReferenceValue = hologram;
            var array = so.FindProperty("meshRenderers"); array.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            var data = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Ship/ImperialVenatorShipData.asset");
            so.FindProperty("height").floatValue = (float)data.GetType().GetProperty("Height").GetValue(data); so.ApplyModifiedPropertiesWithoutUndo();
            var bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            var box = root.AddComponent<BoxCollider>(); box.center = bounds.center; box.size = bounds.size; box.isTrigger = true;
            var rb = root.GetComponents<Rigidbody>().Single(); rb.useGravity = false; rb.isKinematic = true;
            PrefabUtility.SaveAsPrefabAsset(root, PATH);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets(); return "Imperial Venator placement geometry, hologram, explicit renderer list and fitted trigger saved.";
    }
}
