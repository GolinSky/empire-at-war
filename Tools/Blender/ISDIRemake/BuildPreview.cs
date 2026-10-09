using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildISDIRemakePreview
{
    public static string Main()
    {
        foreach(string name in new[]{"ISDI"})
        {
            bool ship=name=="ISDI";string kind=ship?"Ships":"Squadrons",icons=ship?"ShipIcon":"SquadronIcon";
            var visual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/"+kind+"/"+name+".prefab");
            string path="Assets/Prefabs/Ui/Reinforcement/"+name+"ReinforcementView.prefab";
            if(!File.Exists(path))AssetDatabase.CopyAsset("Assets/Prefabs/Ui/Reinforcement/"+(ship?"Imperator":"TIEFighter")+"ReinforcementView.prefab",path);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name=name+"ReinforcementView";foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                int count=ship?1:name=="TIEInterceptor"?8:name=="TIEBrute"?6:4;
                for(int member=0;member<count;member++)
                {
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(visual,root.transform);
                    if(!ship){var slot=EmpireAtWar.Components.Squadrons.Flight.SquadronFormation.GetSlot(member,5);model.transform.localPosition=new Vector3(slot.X,slot.Y,slot.Z);}
                }
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterials.Any(m=>m.shader.name!="EmpireAtWar/Ship Lit")))renderer.enabled=false;
                var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
                foreach(var renderer in renderers)renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                var spawn=root.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");var so=new SerializedObject(spawn);
                var array=so.FindProperty("meshRenderers");array.arraySize=renderers.Length;for(int i=0;i<renderers.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=renderers[i];
                var shipData=ship?AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Ship/ISDIShipData.asset"):null;
                so.FindProperty("hologramMaterial").objectReferenceValue=material;so.FindProperty("height").floatValue=ship?(float)shipData.GetType().GetProperty("Height").GetValue(shipData):11;so.ApplyModifiedPropertiesWithoutUndo();
                var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                var box=root.GetComponents<BoxCollider>().Single();box.center=bounds.center;box.size=bounds.size;box.isTrigger=true;
                var rb=root.GetComponents<Rigidbody>().Single();rb.useGravity=false;rb.isKinematic=true;PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}

        }
        AssetDatabase.SaveAssets();return "Saved the ISD I hologram preview with current geometry and explicit material bindings.";
    }
}
