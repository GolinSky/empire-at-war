"""Adapt existing asset builders to Nebula's audited attachment layout and reference stats."""
from pathlib import Path

ROOT = Path('Tools/Blender/Nebula')
DONOR = Path('Tools/Blender/CorellianBattlecruiser')

def Read(name):
    return (DONOR/name).read_text().replace('CorellianBattlecruiser', 'Nebula')

art = Read('BuildArt.cs').replace('const float LENGTH = 140', 'const float LENGTH = 160')
art = art.replace('importer.assetPath.Contains("Normal")', '(importer.assetPath.EndsWith("_B.png"))')
art = art.replace('((string)source["shader"]).Contains("Additive")', '(((string)source["shader"]).Contains("Additive") || ((string)source["shader"]).Contains("Shield"))')
art = art.replace('TEXTURES+texture+".png"', 'TEXTURES+texture+(additive?"_Additive":"")+".png"')
art = art.replace('texture.Contains("CorBC_") && texture.Contains("diffuse")', '(texture.Contains("Prototype_Main") || texture.Contains("Prototype_Second"))')
start = art.index('            for (int i=0;i<4;i++)')
end = art.index('            var bounds=BoundsOf(root)', start)
art = art[:start] + '''            var audit=JObject.Parse(File.ReadAllText("Temp/NebulaImport/SourceAudit.json"));
            foreach(var attachment in audit["attachments"])
            {
                string bone=(string)attachment["bone"];
                var socket=bones.Single(t=>t.name.Equals(bone,StringComparison.OrdinalIgnoreCase) && t.GetComponent<MeshFilter>()==null);
                var part=Instantiate((string)attachment["variant"],socket,report);
                part.transform.localPosition=Vector3.zero;
                part.transform.localRotation=Quaternion.Euler(90,0,0);
                part.transform.localScale=Vector3.one*.01f;
            }
''' + art[end:]
art = art.replace('string sourceName=renderer.name.Split(\'.\')[0]; renderer.name=sourceName;', 'string sourceName=renderer.name;')
art = art.replace('Saved Battlecruiser hull + four source turret models', 'Saved Nebula hull and fifteen source turret models')
(ROOT/'BuildArt.cs').write_text(art)

game = Read('BuildGameplay.cs')
start = game.index('            foreach (var source in audit["hardpoints"]')
end = game.index('            var opaque =', start)
game = game[:start] + '''            HardPoint Mount(string name,int type,int profile,Vector3 position,string source)
            {
                var mount=new GameObject(name); mount.transform.SetParent(mountGroup,false); mount.transform.position=position;
                var hp=type==0 ? (HardPoint)mount.AddComponent<WeaponHardPoint>() : mount.AddComponent<HardPoint>();
                Configure(hp,id++,type,profile,85);targetable.Add(hp);
                mount.AddComponent<SphereCollider>().radius=type==0?1.5f:3f;
                if(hp is WeaponHardPoint)weapons.Add((WeaponHardPoint)hp);
                records.Add(new JObject{["id"]=hp.Id,["name"]=name,["source"]=source,["position"]=new JArray(position.x,position.y,position.z),["profile"]=profile,["targetable"]=true});
                return hp;
            }
            Vector3 PartMuzzle(int index,string name="FP_01")=>bones.Single(t=>t.name=="NebulaPart"+index.ToString("D2")).GetComponentsInChildren<Transform>(true).Single(t=>t.name==name).position;
            for(int i=1;i<=4;i++)for(int side=0;side<2;side++)
                Mount("DualHeavyTurbolaser_"+i+"_"+side,0,3,PartMuzzle(i)+Vector3.right*(side==0?-0.65f:.65f),"NebulaPart"+i.ToString("D2")+"/FP_01 with derived barrel offset");
            for(int i=9;i<=14;i++)Mount("MediumTurbolaser_"+i,0,4,PartMuzzle(i),"NebulaPart"+i.ToString("D2")+"/FP_01");
            for(int i=5;i<=8;i++)Mount("MediumIon_"+i,0,32,PartMuzzle(i),"NebulaPart"+i.ToString("D2")+"/FP_01");
            foreach(string n in new[]{"MUZZLEA_01","MUZZLEA_02"})Mount("AssaultConcussion_"+n,0,65,Bone(n).position,n);
            foreach(string n in new[]{"MUZZLEA_03","MUZZLEA_04","MUZZLEA_05","MUZZLEA_06"})Mount("PointDefense_"+n,0,14,Bone(n).position,n);
            for(int side=0;side<2;side++)
            {
                float x=(side==0?-1:1)*bounds.size.x*.07f;
                Mount("Engine_"+side,1,0,new Vector3(x,bounds.center.y,bounds.min.z+bounds.size.z*.04f),"Derived from stern hull bounds");
                Mount("ShieldGenerator_"+side,2,0,Bone("Shield_Gen").position+Vector3.right*x,"Shield_Gen with paired offset");
                Mount("Hangar_"+side,4,0,Bone("Hanger").position+Vector3.right*x,"Hanger with paired offset");
            }
            Mount("TractorBeam",7,0,Bone("MUZZLEA_00").position,"MUZZLEA_00");
            if(weapons.Count!=24 || targetable.Count!=31)throw new InvalidOperationException("Expected 24 weapons + 7 subsystem hardpoints.");
''' + game[end:]
start = game.index('            var hangar = Component(root,"HangarComponent");')
end = game.index('            foreach (var image', start)
game = game[:start] + '''            var hangars=targetable.Where(h=>h.HardPointType==EmpireAtWar.Components.Ship.Health.HardPointType.Hangar).ToArray();
            var launches=hangars.Select((h,i)=>
            {
                var launch=new GameObject("NebulaLaunchPoint_"+i).transform;launch.SetParent(body,false);
                launch.localPosition=new Vector3(h.transform.localPosition.x,bounds.min.y-8,h.transform.localPosition.z);
                return launch;
            }).ToArray();
            so=new SerializedObject(hangarComponent);
            so.FindProperty("launchPoint").objectReferenceValue=launches[0];
            so.FindProperty("hangarHardPoint").objectReferenceValue=hangars[0];
            so.FindProperty("isDestroyable").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
            Assign(hangarComponent,"bayHardPoints",hangars);Assign(hangarComponent,"bayLaunchPoints",launches);
''' + game[end:]
game = game.replace('Set(data,"Hull",14000); Set(data,"Shields",8000); Set(data,"Speed",25);', 'Set(data,"Hull",16000); Set(data,"Shields",15000); Set(data,"Speed",20);')
game = game.replace('8000f/2000f*13.333333f', '25f').replace('Set(data,"NavigationRadius",80)', 'Set(data,"NavigationRadius",Mathf.Sqrt(bounds.extents.x*bounds.extents.x+bounds.extents.z*bounds.extents.z)+8)')
start = game.index('        var hpHealth=')
end = game.index('        Save(data);AssetDatabase.SaveAssets();',start)
game = game[:start] + '''        var hpHealth=data.FindProperty("hardPointHealth");hpHealth.arraySize=5;
        int[] types={0,1,2,4,7};float[] values={750,1500,1250,1200,1000};
        for(int i=0;i<5;i++){var row=hpHealth.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=values[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=1;abilities.GetArrayElementAtIndex(0).intValue=38;
        var bays=data.FindProperty("hangarBays");bays.arraySize=2;
        for(int i=0;i<2;i++){var bay=bays.GetArrayElementAtIndex(i);bay.FindPropertyRelative("squadronType").intValue=300;bay.FindPropertyRelative("reserve").intValue=i==0?3:2;bay.FindPropertyRelative("maxActive").intValue=i==0?2:1;}
        var loadout=data.FindProperty("weaponLoadout");loadout.arraySize=5;
        int[] profiles={3,4,32,65,14};int[] counts={8,6,4,2,4};
        for(int i=0;i<5;i++){var row=loadout.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=profiles[i];row.FindPropertyRelative("count").intValue=counts[i];}
''' + game[end:]
game = game.replace('Corellian gameplay saved: 20 targetable hardpoints, 17 weapons, X-Wing/A-Wing/Y-Wing hangar;', 'Nebula gameplay saved: 31 targetable hardpoints, 24 weapons, two X-Wing hangars;')
(ROOT/'BuildGameplay.cs').write_text(game)
for name in ('Persist.cs','Render.cs'):
    text=Read(name).replace('Battlecruiser','Nebula')
    if name=='Render.cs':text=text.replace('orthographicSize=78','orthographicSize=90')
    (ROOT/name).write_text(text)

register=Read('Register.cs').replace('SHIP_ID=306','SHIP_ID=330').replace('ABILITY_ID=34','ABILITY_ID=38')
register=register.replace('"Small ships"','"Heavy cruisers"').replace('"Frigates"','"Star destroyers"').replace('"Fleet reinforcement / Squadron support"','"Heavy frontline combat"')
register=register.replace('"Corellian Battlecruiser"','"Nebula Star Destroyer"')
start=register.index('        value.FindPropertyRelative("description").stringValue=')
end=register.index('\n',start)
register=register[:start]+'''        value.FindPropertyRelative("description").stringValue="Heavy frontline star destroyer. Eight dual heavy turbolasers, six medium turbolasers, four medium ion cannons, two assault concussion launchers and four point defense lasers. Supports three active X-Wing squadrons and two replacements. Overcharge Weapons increases firing rate by 25% and disables shield regeneration for 10 seconds; 50 second cooldown.";'''+register[end:]
register=register.replace('intValue=2000;','intValue=7200;').replace('intValue=81;','intValue=72;').replace('"<AvailableLevel>k__BackingField").intValue=2','"<AvailableLevel>k__BackingField").intValue=5').replace('"<UnitCapacity>k__BackingField").intValue=3','"<UnitCapacity>k__BackingField").intValue=8')
start=register.index('        var catalog=')
end=register.index('        Save(audio);AssetDatabase.SaveAssets();',start)
register=register[:start]+'''        var catalog=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var definitions=catalog.FindProperty("definitions.keyValue");
        int index=Enumerable.Range(0,definitions.arraySize).Single(i=>definitions.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue==5);
        row=Enumerable.Range(0,definitions.arraySize).Select(i=>definitions.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==ABILITY_ID);
        if(row==null){definitions.InsertArrayElementAtIndex(index);row=definitions.GetArrayElementAtIndex(index);row.FindPropertyRelative("key").intValue=ABILITY_ID;}
        value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("displayName").stringValue="Overcharge Weapons";
        value.FindPropertyRelative("description").stringValue="Weapons fire 25% faster; shield regeneration stops for 10 seconds. 50 second cooldown.";
        value.FindPropertyRelative("duration").floatValue=10;value.FindPropertyRelative("recoveryDelay").floatValue=50;
        value.FindPropertyRelative("canCancel").boolValue=false;value.FindPropertyRelative("isToggle").boolValue=false;value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;
        value.FindPropertyRelative("settings").managedReferenceValue=new BoostWeaponPowerSettings();
        var modifier=value.FindPropertyRelative("settings.statModifier");
        modifier.FindPropertyRelative("damageMultiplier").floatValue=1;modifier.FindPropertyRelative("fireDelayMultiplier").floatValue=.8f;
        modifier.FindPropertyRelative("speedMultiplier").floatValue=1;modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue=0;modifier.FindPropertyRelative("damageTakenMultiplier").floatValue=1;Save(catalog);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==5);abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;}
''' + register[end:]
start=register.index('    static void Profile(')
end=register.index('    static SerializedObject Load(',start)
register=register[:start]+register[end:]
register=register.replace('Battlecruiser=306 registered: Rebellion level 2/population 3, views/data/placement/icons/matchups, Power to Shields=34 and two-burst rocket profile=64 with audio.', 'Nebula=330 registered with Rebellion, data/view/placement/icons/matchups and Overcharge Weapons=38.')
(ROOT/'Register.cs').write_text(register)
print('Generated Nebula asset builders')
