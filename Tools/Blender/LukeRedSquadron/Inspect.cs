using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.ViewComponents.Squadrons;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LukeInspect
{
    private const string OUTPUT = "Temp/LukeRedSquadron/Previews/";
    public static object Run()
    {
        Directory.CreateDirectory(OUTPUT);
        var data=AssetDatabase.LoadAssetAtPath<SquadronData>("Assets/Settings/Data/Squadron/LukeRedSquadronSquadronData.asset");
        var weapons=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var view=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Squadrons/LukeRedSquadronSquadronView.prefab");
        var components=BalancePrefabBindings.Components(view).OfType<Component>().ToArray();
        var fighters=components.OfType<FighterHardPoint>().OrderBy(f=>f.Id).ToArray();
        var bindings=components.OfType<EmpireAtWar.Entities.BaseEntity.EntityBindings>().Single();
        var ability=AssetDatabase.LoadAssetAtPath<EmpireAtWar.Services.ShipAbilities.ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get(EmpireAtWar.Services.ShipAbilities.ShipAbilityId.LuckyShot);
        var settings=(EmpireAtWar.Services.ShipAbilities.Abilities.LuckyShotSettings)ability.Settings;
        var faction=AssetDatabase.LoadAssetAtPath<EmpireAtWar.Models.Factions.FactionCatalog>("Assets/Settings/Data/Factions/Shared/FactionCatalog.asset").GetSquadronFactionData(EmpireAtWar.Entities.Squadrons.SquadronType.LukeRedSquadron);
        var others=AssetDatabase.FindAssets("t:SquadronData",new[]{"Assets/Settings/Data/Squadron"}).Select(g=>AssetDatabase.LoadAssetAtPath<SquadronData>(AssetDatabase.GUIDToAssetPath(g))).Where(d=>d!=data).ToArray();
        Render(view,"Formation");
        Render(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Squadrons/LukeRedSquadron.prefab"),"Leader");
        Render(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/LukeRedSquadronReinforcementView.prefab"),"Placement");
        var result=new {data.MemberCount,data.TotalHull,data.TotalShields,data.LeaderHull,data.LeaderShields,data.CruiseSpeed,data.CombatSpeed,
            dps=data.WeaponLoadout.Sum(w=>weapons.GetProfile(w.WeaponType).DamagePerSecond*w.Count),
            leaderDamageType=weapons.GetProfile(WeaponType.LukeSkywalkerLaser).DamageType.ToString(),
            otherMaxDps=others.Max(d=>d.WeaponLoadout.Sum(w=>weapons.GetProfile(w.WeaponType).DamagePerSecond*w.Count)),
            otherMaxHull=others.Max(d=>d.TotalHull),otherMaxShields=others.Max(d=>d.TotalShields),
            bindings=new {health=BalancePrefabBindings.References(bindings.SquadronHealth,"fighters").Count,flight=BalancePrefabBindings.References(bindings.SquadronFlight,"fighters").Count,guns=BalancePrefabBindings.References(bindings.Weapons.Single(),"hardPoints").Count,injectables=bindings.Injectables.Length},
            ability=new {ability.DisplayName,ability.Range,ability.RecoveryDelay,settings.Damage,settings.Windup,projectile=settings.Projectile.ShotPrefab.name,icon=ability.Icon.name},
            roster=new {faction.Price,faction.BuildTime,faction.UnitCapacity,faction.MaxCount,faction.AvailableLevel,faction.IsHero},
            addresses=new[]{"Assets/Prefabs/Models/Squadrons/LukeRedSquadronSquadronView.prefab","Assets/Prefabs/Ui/Reinforcement/LukeRedSquadronReinforcementView.prefab","Assets/Settings/Data/Squadron/LukeRedSquadronSquadronData.asset"}.Select(path=>{var entry=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));return new{path,entry.address,group=entry.parentGroup.Name};}).ToArray(),
            fighters=fighters.Select(f=>new {f.Id,f.name,guns=BalancePrefabBindings.Components(f.gameObject).OfType<WeaponHardPoint>().Select(w=>new{w.Id,type=w.WeaponType.ToString()}).ToArray()}).ToArray(),
            meshes=components.OfType<MeshRenderer>().Count(),missingMaterials=components.OfType<MeshRenderer>().Sum(r=>r.sharedMaterials.Count(m=>m==null)),
            output=Path.GetFullPath(OUTPUT)};
        File.WriteAllText(OUTPUT+"Readback.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    private static void Render(GameObject source,string name)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;var oldActive=RenderTexture.active;
        var root=new GameObject("Preview");SceneManager.MoveGameObjectToScene(root,scene);
        var cameraObject=new GameObject("Camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.065f,1);camera.nearClipPlane=.01f;camera.farClipPlane=2000;camera.cullingMask=1<<30;
        var target=RenderTexture.GetTemporary(1200,900,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
        var texture=new Texture2D(1200,900,TextureFormat.RGBA32,false);
        try
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.42f,.47f);
            foreach(var direction in new[]{new Vector3(35,30,0),new Vector3(-20,-130,0)})
            {var go=new GameObject("Light");SceneManager.MoveGameObjectToScene(go,scene);var light=go.AddComponent<Light>();light.type=LightType.Directional;light.intensity=direction.x>0?1.4f:.7f;light.cullingMask=1<<30;go.transform.rotation=Quaternion.Euler(direction);}
            var components=BalancePrefabBindings.Components(source).OfType<Component>().ToArray();
            var renders=new List<MeshRenderer>();
            foreach(var original in components.OfType<MeshRenderer>().Where(r=>r.enabled))
            {
                var go=new GameObject(original.name);go.layer=30;go.transform.SetParent(root.transform,false);go.transform.position=original.transform.position;go.transform.rotation=original.transform.rotation;go.transform.localScale=original.transform.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh=components.OfType<MeshFilter>().Single(f=>f.transform==original.transform).sharedMesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=original.sharedMaterials;renders.Add(renderer);
            }
            var bounds=renders[0].bounds;foreach(var renderer in renders.Skip(1))bounds.Encapsulate(renderer.bounds);
            camera.orthographicSize=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))*.65f;
            camera.transform.position=bounds.center+new Vector3(1,1.2f,1.4f)*100;camera.transform.LookAt(bounds.center);
            camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1200,900),0,0);texture.Apply();File.WriteAllBytes(OUTPUT+name+".png",texture.EncodeToPNG());
        }
        finally
        {RenderTexture.active=oldActive;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);EditorSceneManager.ClosePreviewScene(scene);RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;}
    }
}
