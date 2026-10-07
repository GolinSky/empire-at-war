using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildImperialPreviews
{
    public static string Main()
    {
        foreach(string name in new[]{"ISDI","TIEInterceptor","TIEBrute","TIEPunisher"})
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
                so.FindProperty("height").floatValue=ship?(float)shipData.GetType().GetProperty("Height").GetValue(shipData):11;so.ApplyModifiedPropertiesWithoutUndo();
                var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                var box=root.GetComponents<BoxCollider>().Single();box.center=bounds.center;box.size=bounds.size;box.isTrigger=true;
                var rb=root.GetComponents<Rigidbody>().Single();rb.useGravity=false;rb.isKinematic=true;PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            Render(name,visual,icons);
        }
        AssetDatabase.SaveAssets();return "Saved four hologram previews, transparent icons, fighter silhouettes and eight team palettes for every imported model.";
    }
    static void Render(string name,GameObject visual,string folder)
    {
        var scene=EditorSceneManager.NewPreviewScene();var oldPalette=Shader.GetGlobalVectorArray("_TeamColors");var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;var oldActive=RenderTexture.active;
        var root=(GameObject)PrefabUtility.InstantiatePrefab(visual,scene);foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);float length=bounds.size.magnitude;
        var cameraObject=new GameObject("ImperialPreviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<30;camera.orthographic=true;camera.orthographicSize=length*.45f;camera.nearClipPlane=.01f;camera.farClipPlane=length*5;camera.transform.position=new Vector3(.65f,.75f,1)*length;camera.transform.LookAt(bounds.center);
        foreach(var spec in new[]{new Vector3(35,30,1.6f),new Vector3(-20,-130,.7f)}){var obj=new GameObject("ImperialPreviewLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,scene);var light=obj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=spec.z;light.cullingMask=1<<30;obj.transform.rotation=Quaternion.Euler(spec.x,spec.y,0);}
        var points=root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.GetComponents<MeshRenderer>().Single().enabled).SelectMany(m=>m.sharedMesh.vertices.Select(v=>camera.transform.InverseTransformPoint(m.transform.TransformPoint(v)))).ToArray();
        camera.orthographicSize=points.Max(p=>Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.y)))*1.1f;
        var target=RenderTexture.GetTemporary(512,512,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var texture=new Texture2D(512,512,TextureFormat.RGBA32,false);
        var temporaryMaterials=new System.Collections.Generic.List<Material>();
        string iconPath="Assets/Art/Textures/Ui/Icons/"+folder+"/"+name+"Icon.png",silhouettePath="Assets/Art/Textures/Ui/Icons/"+folder+"/"+name+"Silhouette.png";
        try
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.42f,.47f);
            Capture(iconPath);var pixels=texture.GetPixels32();for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(255,255,255,pixels[i].a);texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(silhouettePath,texture.EncodeToPNG());
            string previews="Temp/ISDIImport/Previews/"+name;Directory.CreateDirectory(previews);
            Color[] colors={new Color(.2f,.55f,1),new Color(1,.25f,.2f),new Color(.3f,.9f,.4f),new Color(1,.75f,.2f),new Color(.7f,.35f,1),new Color(.2f,.9f,.9f),new Color(1,.45f,.75f),new Color(.9f,.9f,.9f)};Shader.SetGlobalVectorArray("_TeamColors",colors.Select(c=>(Vector4)c).ToArray());
            for(int i=0;i<8;i++){foreach(var r in renderers)r.SetShaderUserValue((uint)(i+1));Capture(previews+"/Team"+i+".png");}
            var originals=renderers.Select(r=>r.sharedMaterials).ToArray();
            foreach(var r in renderers)
            {
                if(r.sharedMaterials.Any(m=>m.shader.name!="EmpireAtWar/Ship Lit")){r.enabled=false;continue;}
                r.sharedMaterials=Enumerable.Repeat(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat"),r.sharedMaterials.Length).ToArray();
            }
            Capture(previews+"/Placement.png");
            for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=originals[i];
            if(name=="ISDI")
            {
                foreach(var r in renderers.Where(r=>r.enabled))
                {
                    var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Wrecks/ISDI/"+r.sharedMaterial.name+"_Wreck.mat"));temporaryMaterials.Add(material);
                    material.SetVector("_WreckAxis",Vector3.forward);material.SetVector("_WreckCenter",Vector3.zero);material.SetVector("_WreckCuts",new Vector4(1000000,1000000,1000000,1000000));material.SetVector("_WreckAxisRange",new Vector4(-90,90,0,0));
                    material.SetFloat("_WreckStartTime",0);material.SetFloat("_WreckLifetime",1000000000);material.SetFloat("_WreckGlowDuration",1000000000);material.SetFloat("_WreckSeparationSpeed",0);material.SetFloat("_WreckTiltSpeed",0);material.SetFloat("_WreckSinkSpeed",0);r.sharedMaterial=material;r.SetShaderUserValue(1);
                }
                Capture(previews+"/Wreck.png");
            }
            void Capture(string path){camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,512,512),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
        }
        finally{RenderTexture.active=oldActive;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);EditorSceneManager.ClosePreviewScene(scene);foreach(var material in temporaryMaterials)UnityEngine.Object.DestroyImmediate(material);RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;Shader.SetGlobalVectorArray("_TeamColors",oldPalette.Length>0?oldPalette:new Vector4[8]);}
        AssetDatabase.Refresh();foreach(string path in new[]{iconPath,silhouettePath}){var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
    }
}
