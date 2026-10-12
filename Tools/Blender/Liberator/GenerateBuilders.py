"""Adapt the existing ship builders to Liberator's audited attachments and balance."""
from pathlib import Path

root = Path('Tools/Blender/Liberator')
donor = Path('Tools/Blender/CorellianBattlecruiser')

def Read(name):
    return (donor/name).read_text().replace('CorellianBattlecruiser', 'Liberator').replace('Corellian ', 'Liberator ')

game = Read('BuildGameplay.cs')
start = game.index('            foreach (var source in audit["hardpoints"]')
end = game.index('            var opaque =', start)
game = game[:start] + '''            void Mount(string name,int type,int profile,Vector3 position,string source)
            {
                var mount=new GameObject(name); mount.transform.SetParent(mountGroup,false); mount.transform.position=position;
                var hp=type==0 ? (HardPoint)mount.AddComponent<WeaponHardPoint>() : mount.AddComponent<HardPoint>();
                Configure(hp,id++,type,profile,85); targetable.Add(hp);
                if(hp is WeaponHardPoint) weapons.Add((WeaponHardPoint)hp);
                records.Add(new JObject{["id"]=hp.Id,["name"]=name,["source"]=source,["position"]=new JArray(position.x,position.y,position.z),["targetable"]=true});
            }
            foreach(var attachment in audit["attachments"])
            {
                string name=(string)attachment["hardpoint"], variant=(string)attachment["variant"];
                int profile;
                if(name.Contains("_DIC_")) profile=32;
                else if(name.Contains("_DLC_")) profile=14;
                else if(name.Contains("_DTL_"))
                {
                    int index=int.Parse(name.Substring(name.Length-2));
                    if(index>6) continue; // Keep all source turret art; the balance note specifies six turbolasers.
                    profile=index<=4 ? 2 : 5;
                }
                else continue;
                var part=bones.Single(t=>t.name==variant);
                string muzzle=name.Contains("_DTL_") ? "MuzzleA_00" : "FP_01";
                Mount(name,0,profile,part.GetComponentsInChildren<Transform>(true).Single(t=>t.name==muzzle).position,variant+"/"+muzzle);
            }
            var engine=Bone("HP_Engines").position;
            Mount("EnginePort",1,0,engine+Vector3.left*bounds.size.x*.15f,"HP_Engines + port offset");
            Mount("EngineStarboard",1,0,engine+Vector3.right*bounds.size.x*.15f,"HP_Engines + starboard offset");
            Mount("ShieldGenerator",2,0,Bone("Shield").position,"Shield");
            Mount("HangarPort",4,0,Bone("HP_Spawn_00").position,"HP_Spawn_00");
            Mount("HangarStarboard",4,0,Bone("HP_Spawn_01").position,"HP_Spawn_01");
            Mount("TractorBeam",7,0,Bone("HP_Facingboi").position,"HP_Facingboi");
            if(weapons.Count!=12 || targetable.Count!=18) throw new InvalidOperationException("Expected 12 weapons + six subsystem hardpoints.");
''' + game[end:]
start = game.index('            var hangar = Component(root,"HangarComponent");')
end = game.index('            foreach (var image', start)
game = game[:start] + '''            var bays=targetable.Where(h=>h.HardPointType==EmpireAtWar.Components.Ship.Health.HardPointType.Hangar).ToArray();
            var exits=new Transform[2];
            for(int i=0;i<2;i++)
            {
                exits[i]=new GameObject("LiberatorLaunchPoint"+i).transform; exits[i].SetParent(body,false);
                exits[i].localPosition=new Vector3(bays[i].transform.localPosition.x,bounds.min.y-8,bays[i].transform.localPosition.z);
            }
            so=new SerializedObject(hangarComponent);
            so.FindProperty("launchPoint").objectReferenceValue=exits[0];
            so.FindProperty("hangarHardPoint").objectReferenceValue=bays[0];
            so.FindProperty("isDestroyable").boolValue=true; so.ApplyModifiedPropertiesWithoutUndo();
            Assign(hangarComponent,"bayHardPoints",bays); Assign(hangarComponent,"bayLaunchPoints",exits);
''' + game[end:]
start = game.index('        Set(data,"Hull",')
end = game.index('        Save(data);AssetDatabase.SaveAssets();', start)
game = game[:start] + '''        Set(data,"Hull",4800); Set(data,"Shields",4200); Set(data,"Speed",25);
        Set(data,"ShieldRegenerateValue",28); Set(data,"ShieldRegenerateDelay",3); Set(data,"HullBottom",bottom); Set(data,"HullTop",top);
        Set(data,"NavigationRadius",95); Set(data,"Range",600); Set(data,"HangarInitialDelay",4); Set(data,"HangarLaunchInterval",8);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=Load(WRECK_DATA).targetObject;
        var hpHealth=data.FindProperty("hardPointHealth");hpHealth.arraySize=5;
        int[] types={0,1,2,4,7};float[] values={400,600,600,700,350};
        for(int i=0;i<5;i++){var row=hpHealth.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=values[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=2;abilities.GetArrayElementAtIndex(0).intValue=51;abilities.GetArrayElementAtIndex(1).intValue=22;
        var hangarBays=data.FindProperty("hangarBays");hangarBays.arraySize=2;
        for(int i=0;i<2;i++){var bay=hangarBays.GetArrayElementAtIndex(i);bay.FindPropertyRelative("squadronType").intValue=300;bay.FindPropertyRelative("reserve").intValue=i==0?4:2;bay.FindPropertyRelative("maxActive").intValue=i==0?2:1;}
        var loadout=data.FindProperty("weaponLoadout"); loadout.arraySize=4;
        int[] profiles={2,5,32,14}; int[] counts={4,2,4,2};
        for(int i=0;i<4;i++){var row=loadout.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=profiles[i];row.FindPropertyRelative("count").intValue=counts[i];}
''' + game[end:]
game = game.replace('20 targetable hardpoints, 17 weapons, X-Wing/A-Wing/Y-Wing hangar', '18 targetable hardpoints, 12 weapons, three active plus three replacement X-Wings')
(root/'BuildGameplay.cs').write_text(game)

register = Read('Register.cs').replace('SHIP_ID=306','SHIP_ID=321').replace('ABILITY_ID=34','ABILITY_ID=51')
register = register.replace('"Liberator Battlecruiser"','"Liberator Cruiser"').replace('"Fleet reinforcement / Squadron support"','"Battlecarrier / Fleet support"')
register = register.replace('intValue=2000;', 'intValue=4200;').replace('intValue=81;', 'intValue=48;').replace('"<AvailableLevel>k__BackingField").intValue=2', '"<AvailableLevel>k__BackingField").intValue=3')
start=register.index('        value.FindPropertyRelative("description").stringValue=')
end=register.index('\n', start)
register=register[:start]+'''        value.FindPropertyRelative("description").stringValue="Battlecarrier with 4,800 hull and 4,200 shields. Four heavy turbolasers, two medium dual turbolasers, four medium ion cannons and two point defense lasers. Deploys three active X-Wing squadrons with three replacements. Rapid Deployment launches up to two available reserve squadrons; 45 second cooldown.";'''+register[end:]
start=register.index('        var catalog=')
end=register.index('        Save(audio);AssetDatabase.SaveAssets();', start)
register=register[:start]+'''        var catalog=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var definitions=catalog.FindProperty("definitions.keyValue");
        row=Upsert(definitions,ABILITY_ID); value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("icon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/SquadronIcon/XWingIcon.png");
        value.FindPropertyRelative("displayName").stringValue="Rapid Deployment";
        value.FindPropertyRelative("description").stringValue="Launch up to two available reserve squadrons immediately, within the active capacity. 45 second cooldown.";
        value.FindPropertyRelative("duration").floatValue=0; value.FindPropertyRelative("recoveryDelay").floatValue=45;
        value.FindPropertyRelative("canCancel").boolValue=false; value.FindPropertyRelative("isToggle").boolValue=false;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue=false; value.FindPropertyRelative("range").floatValue=0;
        value.FindPropertyRelative("aiUse").intValue=2;
        value.FindPropertyRelative("settings").managedReferenceValue=new RapidDeploymentSettings(); Save(catalog);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {
            int index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==19);
            abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;
        }
''' + register[end:]
start=register.index('    static void Profile(')
end=register.index('    static SerializedObject Load(',start)
register=register[:start]+register[end:]
register=register.replace('Battlecruiser=306 registered: Rebellion level 2/population 3, views/data/placement/icons/matchups, Power to Shields=34 and two-burst rocket profile=64 with audio.', 'Liberator=321 registered with Rapid Deployment=51.')
(root/'Register.cs').write_text(register)
