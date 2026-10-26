"""Adapt the verified asset-only Battlecruiser builders to MC30c's audited hull."""
from pathlib import Path

ROOT = Path('Tools/Blender/MC30c')
DONOR = Path('Tools/Blender/CorellianBattlecruiser')


def Read(name):
    return (DONOR/name).read_text().replace('CorellianBattlecruiser', 'MC30c')


art = Read('BuildArt.cs').replace('const float LENGTH = 140', 'const float LENGTH = 80')
start = art.index('            for (int i=0;i<4;i++)')
end = art.index('            var bounds=BoundsOf(root)', start)
art = art[:start] + art[end:]
art = art.replace('importer.assetPath.Contains("Normal")', '(importer.assetPath.EndsWith("_BC.png") || importer.assetPath.EndsWith("_B.png"))')
art = art.replace('texture.Contains("CorBC_") && texture.Contains("diffuse")', 'texture.Contains("Cruiser_AllMC30C_CL") && !texture.EndsWith("_BC")')
art = art.replace('material.SetFloat("_TeamMaskStrength",0);', 'material.SetFloat("_TeamMaskStrength",hullPaint?1:0); if(hullPaint) material.SetTexture("_TeamMaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+texture+"_TeamMask.png"));')
art = art.replace('material.SetFloat("_TeamLiveryStrength",hullPaint?1:0)', 'material.SetFloat("_TeamLiveryStrength",0)')
art = art.replace('Saved Battlecruiser hull + four source turret models', 'Saved MC30c hull')
(ROOT/'BuildArt.cs').write_text(art)

game = Read('BuildGameplay.cs')
game = game.replace('            var hangarComponent = Component(root,"HangarComponent");\n            hangarComponent.transform.SetParent(root.transform,true);', '            foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c.GetType().Name=="HangarComponent").ToArray())\n                UnityEngine.Object.DestroyImmediate(component.gameObject);')
start = game.index('            foreach (var source in audit["hardpoints"]')
end = game.index('            var opaque =', start)
game = game[:start] + '''            void Mount(string name,int type,int profile,Vector3 position,string boneName)
            {
                var mount=new GameObject(name); mount.transform.SetParent(mountGroup,false); mount.transform.position=position;
                var hp=type==0 ? (HardPoint)mount.AddComponent<WeaponHardPoint>() : mount.AddComponent<HardPoint>();
                Configure(hp,id++,type,profile,85); targetable.Add(hp);
                if(hp is WeaponHardPoint) weapons.Add((WeaponHardPoint)hp);
                records.Add(new JObject{["id"]=hp.Id,["name"]=name,["bone"]=boneName,["position"]=new JArray(position.x,position.y,position.z),["targetable"]=true});
            }
            foreach(string n in new[]{"HP_TORP01_FP01","HP_TORP02_FP01"}) Mount("HeavyProtonTorpedo_"+n,0,66,Bone(n).position,n);
            foreach(string n in new[]{"HP_TBL_01_FP01","HP_TBL_01_FP02","HP_TBL_02_FP01","HP_TBL_02_FP02"}) Mount("MediumTurbolaser_"+n,0,4,Bone(n).position,n);
            foreach(string n in new[]{"HP_L_01_FP1","HP_L_01_FP2","HP_L_02_FP1","HP_L_02_FP2"}) Mount("DualLaser_"+n,0,7,Bone(n).position,n);
            foreach(string n in new[]{"HP_TORP03_FP01","HP_TORP04_FP01"}) Mount("ClusterBomb_"+n,0,81,Bone(n).position,n);
            foreach(string n in new[]{"HP_TORP03_FP02","HP_TORP04_FP02"}) Mount("PointDefense_"+n,0,14,Bone(n).position,n);
            Mount("Engine",1,0,visual.transform.TransformPoint(new Vector3(0,bounds.center.y,bounds.min.z+bounds.size.z*.06f)),null);
            Mount("ShieldGenerator",2,0,Bone("Shield").position,"Shield");
            if(weapons.Count!=14 || targetable.Count!=16) throw new InvalidOperationException("Expected 14 weapons + shield/engine.");
''' + game[end:]
start = game.index('            var hangar = Component(root,"HangarComponent");')
end = game.index('            foreach (var image', start)
game = game[:start] + game[end:]
game = game.replace('Vector2.one*160', 'Vector2.one*90')
game = game.replace('Configure(hp,id++,type,profile,85)', 'Configure(hp,id++,type,profile,profile==14?180:100)')
game = game.replace('hp.transform.localRotation=Quaternion.Euler(0,hp.transform.localPosition.x>0?90:-90,0);', 'hp.transform.localRotation=Quaternion.Euler(0,weapon==66?0:(hp.transform.localPosition.x>0?90:-90),0);')
game = game.replace('Set(data,"Hull",14000); Set(data,"Shields",8000); Set(data,"Speed",25);', 'Set(data,"Hull",1800); Set(data,"Shields",2400); Set(data,"Speed",42);')
game = game.replace('8000f/2000f*13.333333f', '16f')
game = game.replace('Set(data,"NavigationRadius",80)', 'Set(data,"NavigationRadius",46)')
game = game.replace('data=Load(DATA); data.targetObject.name="MC30cShipData";', 'data=Load(DATA); data.targetObject.name="MC30cShipData"; data.FindProperty("<ShipClass>k__BackingField").intValue=3; data.FindProperty("<HeightTier>k__BackingField").intValue=7;')
start = game.index('        var hpHealth=')
end = game.index('        Save(data);AssetDatabase.SaveAssets();',start)
game = game[:start] + '''        var hpHealth=data.FindProperty("hardPointHealth");hpHealth.arraySize=3;
        int[] types={0,1,2};float[] values={180,360,360};
        for(int i=0;i<3;i++){var row=hpHealth.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=values[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=1;abilities.GetArrayElementAtIndex(0).intValue=36;
        data.FindProperty("hangarBays").arraySize=0;
        var loadout=data.FindProperty("weaponLoadout"); loadout.arraySize=5;
        int[] profiles={66,4,7,14,81}; int[] counts={2,4,4,2,2};
        for(int i=0;i<5;i++){var row=loadout.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=profiles[i];row.FindPropertyRelative("count").intValue=counts[i];}
''' + game[end:]
game = game.replace('Corellian gameplay saved: 20 targetable hardpoints, 17 weapons, X-Wing/A-Wing/Y-Wing hangar;', 'MC30c gameplay saved: 16 targetable hardpoints, 14 weapons, no hangar;')
(ROOT/'BuildGameplay.cs').write_text(game)

for name in ('Persist.cs', 'Render.cs'):
    text=Read(name).replace('Battlecruiser', 'MC30c')
    if name=='Render.cs':
        text=text.replace('orthographicSize=78', 'orthographicSize=45')
        text=text.replace('var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Wrecks/Rebellion/MC30c/"+renderer.sharedMaterial.name+"_Wreck.mat"));', 'var replacements=renderer.sharedMaterials.Select(source=>new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Wrecks/Rebellion/MC30c/"+source.name+"_Wreck.mat"))).ToArray();\n                foreach(var material in replacements)\n                {')
        text=text.replace('renderer.sharedMaterial=material;', '}\n                renderer.sharedMaterials=replacements;')
        text=text.replace('Corellian icon', 'MC30c icon')
    (ROOT/name).write_text(text)

register=Read('Register.cs').replace('SHIP_ID=306', 'SHIP_ID=307').replace('ABILITY_ID=34', 'ABILITY_ID=36')
register=register.replace('"Small ships"', '"Cruisers"').replace('"Frigates"', '"Capital ships"').replace('"Bomber strikes"', '"Anti-frigate fire"')
register=register.replace('"Fleet reinforcement / Squadron support"', '"Anti-capital strike ship"')
register=register.replace('"Corellian Battlecruiser"', '"MC30c Frigate"')
start=register.index('        value.FindPropertyRelative("description").stringValue=')
end=register.index('\n',start)
register=register[:start]+'''        value.FindPropertyRelative("description").stringValue="Fast torpedo frigate: 1,800 hull, 2,400 shields. Two heavy proton torpedoes, four medium turbolasers, four dual lasers, two point defense lasers and two burst cluster-bomb launchers. Torpedo Salvo increases torpedo firing rate by 50% for 6 seconds.";'''+register[end:]
register=register.replace('intValue=2000;', 'intValue=2500;').replace('intValue=81;', 'intValue=32;').replace('"<UnitCapacity>k__BackingField").intValue=3', '"<UnitCapacity>k__BackingField").intValue=2')
start=register.index('        var catalog=')
end=register.index('        Save(audio);AssetDatabase.SaveAssets();',start)
register=register[:start]+'''        var catalog=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var definitions=catalog.FindProperty("definitions.keyValue");
        row=Upsert(definitions,ABILITY_ID); value=row.FindPropertyRelative("value");
        var donor=Enumerable.Range(0,definitions.arraySize).Select(i=>definitions.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==19).FindPropertyRelative("value");
        value.FindPropertyRelative("icon").objectReferenceValue=donor.FindPropertyRelative("icon").objectReferenceValue;
        value.FindPropertyRelative("displayName").stringValue="Torpedo Salvo";
        value.FindPropertyRelative("description").stringValue="Torpedoes fire 50% faster for 6 seconds. 30 second recovery. Other weapons remain unchanged.";
        value.FindPropertyRelative("duration").floatValue=6; value.FindPropertyRelative("recoveryDelay").floatValue=30;
        value.FindPropertyRelative("canCancel").boolValue=false; value.FindPropertyRelative("isToggle").boolValue=false;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;
        value.FindPropertyRelative("aiUse").intValue=donor.FindPropertyRelative("aiUse").intValue;
        value.FindPropertyRelative("settings").managedReferenceValue=new TorpedoSalvoSettings(); Save(catalog);
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset"); var profiles=weapons.FindProperty("weapons");
        if(!Enumerable.Range(0,profiles.arraySize).Any(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==81))
        {
            int index=Enumerable.Range(0,profiles.arraySize).Single(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==35);
            profiles.InsertArrayElementAtIndex(index); row=profiles.GetArrayElementAtIndex(index); row.FindPropertyRelative("weaponType").intValue=81;
        }
        row=Enumerable.Range(0,profiles.arraySize).Select(i=>profiles.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("weaponType").intValue==81);
        row.FindPropertyRelative("displayName").stringValue="Cluster Bomb Launcher";
        row.FindPropertyRelative("pattern").intValue=1; row.FindPropertyRelative("burstShots").intValue=6;
        row.FindPropertyRelative("scope").intValue=2; row.FindPropertyRelative("unit").stringValue="MC30c";
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var sounds=audio.FindProperty("weapons");
        if(!Enumerable.Range(0,sounds.arraySize).Any(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==81))
        {int index=Enumerable.Range(0,sounds.arraySize).Single(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==35);sounds.InsertArrayElementAtIndex(index);sounds.GetArrayElementAtIndex(index).FindPropertyRelative("weaponType").intValue=81;}
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {int index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==19);abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;}
''' + register[end:]
start=register.index('    static void Profile(')
end=register.index('    static SerializedObject Load(',start)
register=register[:start]+register[end:]
register=register.replace('Battlecruiser=306 registered: Rebellion level 2/population 3, views/data/placement/icons/matchups, Power to Shields=34 and two-burst rocket profile=64 with audio.', 'MC30c=307 registered: Rebellion, views/data/placement/icons/matchups, Torpedo Salvo=36 and burst cluster-bomb profile=81 with audio.')
start=register.index('        var sounds=audio.FindProperty("weapons");')
end=register.index('        var abilitySounds=',start)
register=register[:start]+register[end:]
(ROOT/'Register.cs').write_text(register)
print('Generated MC30c asset builders')
