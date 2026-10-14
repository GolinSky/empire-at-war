using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
public static class RenderMiningFacilities {
 const string OUTPUT = "Temp/MiningFacilitiesImport/Previews/";
 public static string Main() {
 Directory.CreateDirectory(OUTPUT);
 var scene=EditorSceneManager.NewPreviewScene();
 var oldAmbient=RenderSettings.ambientLight; var oldMode=RenderSettings.ambientMode; var oldActive=RenderTexture.active;
 var oldPalette=Shader.GetGlobalVectorArray("_TeamColors");
 var root=new GameObject("Preview"); SceneManager.MoveGameObjectToScene(root,scene);
 var camGo=new GameObject("Camera"); SceneManager.MoveGameObjectToScene(camGo,scene); var camera=camGo.AddComponent<Camera>(); camera.scene=scene; camera.enabled=false; camera.orthographic=true; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.035f,.045f,.065f,1); camera.nearClipPlane=.01f; camera.farClipPlane=2000; camera.cullingMask=1<<30;
 foreach(var direction in new[]{new Vector3(35,30,0),new Vector3(-20,-130,0)}) {var go=new GameObject("Light");SceneManager.MoveGameObjectToScene(go,scene);var light=go.AddComponent<Light>();light.type=LightType.Directional;light.intensity=direction.x>0?1.4f:.7f;light.cullingMask=1<<30;go.transform.rotation=Quaternion.Euler(direction);}
 var target=RenderTexture.GetTemporary(768,768,24,RenderTextureFormat.ARGB32); camera.targetTexture=target; var texture=new Texture2D(768,768,TextureFormat.RGBA32,false);
 try {RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.42f,.47f);
 foreach(string name in new[]{"MiningFacility","BattleMiningFacility","MiningConstruction","AsteroidMiningFacilityWreckView","AsteroidDefendPlatformWreckView"}) {
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>((name.Contains("Wreck")?"Assets/Prefabs/Models/Wrecks/":"Assets/Prefabs/Models/MiningFacilities/Vanilla/")+name+".prefab");
 Clone(asset,root.transform,Vector3.zero);
 var renderers=root.GetComponentsInChildren<MeshRenderer>();var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);
 var wreckMaterials=new System.Collections.Generic.List<Material>();
 if(name.Contains("Wreck"))foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(source=>{var material=new Material(source);wreckMaterials.Add(material);material.SetFloat("_WreckLifetime",1000000);material.SetFloat("_WreckSeparationSpeed",0);material.SetFloat("_WreckTiltSpeed",0);material.SetFloat("_WreckSinkSpeed",0);material.SetVector("_WreckCuts",Vector4.one*1000000);return material;}).ToArray();
 camera.orthographicSize=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z))*.8f; camera.transform.position=b.center+new Vector3(1,1.2f,-1.4f)*200;camera.transform.LookAt(b.center);Render(camera,target,texture,OUTPUT+name+".png");
 Shader.SetGlobalVectorArray("_TeamColors",new[]{new Vector4(.2f,.55f,1,1),new Vector4(.3f,.9f,.4f,1),new Vector4(1,.25f,.2f,1),new Vector4(1,.75f,.2f,1),new Vector4(.7f,.35f,1,1),new Vector4(.2f,.9f,.9f,1),new Vector4(1,.45f,.75f,1),new Vector4(.9f,.9f,.9f,1)});
 for(int team=0;team<8;team++){foreach(var renderer in renderers)renderer.SetShaderUserValue((uint)(team+1));Render(camera,target,texture,OUTPUT+name+"Team"+team+".png");}
 foreach(var renderer in renderers)renderer.SetShaderUserValue(0);
 camera.transform.position=b.center+Vector3.up*300;camera.transform.rotation=Quaternion.Euler(90,0,0);Render(camera,target,texture,OUTPUT+name+"Top.png");
 foreach(Transform child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
 foreach(var material in wreckMaterials)UnityEngine.Object.DestroyImmediate(material);
 }
 foreach(string name in new[]{"CaptureSite","BattleAsteroidCaptureSite"}) {
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/CaptureSites/"+name+".prefab");
 foreach(var child in asset.transform.Cast<Transform>().Where(t=>t.name=="AsteroidRocks"||t.name=="MiningFramework"||t.name=="CaptureRing")) Clone(child.gameObject,root.transform,Vector3.zero);
 var b=new Bounds(new Vector3(0,name=="CaptureSite"?-40:20,0),Vector3.one*160);camera.orthographicSize=110;camera.transform.position=b.center+new Vector3(1,1.2f,-1.4f)*200;camera.transform.LookAt(b.center);Render(camera,target,texture,OUTPUT+name+"Construction.png");
 foreach(Transform child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
 Clone(asset.transform.Cast<Transform>().Single(t=>t.name=="CaptureRing").gameObject,root.transform,Vector3.zero);
 var facility=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/MiningFacilities/Vanilla/"+(name=="CaptureSite"?"MiningFacility":"BattleMiningFacility")+".prefab");
 Clone(facility,root.transform,new Vector3(0,name=="CaptureSite"?-60:0,0));Render(camera,target,texture,OUTPUT+name+"Operational.png");
 foreach(Transform child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
 }
 }finally {RenderTexture.active=oldActive;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);EditorSceneManager.ClosePreviewScene(scene);RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;if(oldPalette.Length>0)Shader.SetGlobalVectorArray("_TeamColors",oldPalette);}
 return "Rendered three saved visuals, top views and both construction sites.";
 }
 static void Clone(GameObject source,Transform root,Vector3 offset) {foreach(var renderer in source.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled)) {var go=new GameObject(renderer.name);go.layer=30;go.transform.SetParent(root,false);go.transform.position=renderer.transform.position+offset;go.transform.rotation=renderer.transform.rotation;go.transform.localScale=renderer.transform.lossyScale;go.AddComponent<MeshFilter>().sharedMesh=renderer.GetComponents<MeshFilter>().Single().sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;}}
 static void Render(Camera camera,RenderTexture target,Texture2D texture,string path){camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,768,768),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
}
