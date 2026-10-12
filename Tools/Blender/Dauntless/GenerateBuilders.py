"""Adapt the reviewed Corellian builders to the Dauntless asset and loadout."""
from pathlib import Path

target = Path('Tools/Blender/Dauntless')
def Read(name):
    return Path('Tools/Blender/CorellianBattlecruiser/'+name).read_text().replace('CorellianBattlecruiser', 'Dauntless').replace('Corellian', 'Dauntless')
def Save(name, source):
    (target/name).write_text(source)

art = Read('BuildArt.cs').replace('const float LENGTH = 140', 'const float LENGTH = 160')
art = art.replace('        foreach (var guid', '        var normals=JObject.Parse(File.ReadAllText("Temp/DauntlessImport/ConversionReport.json"))["materials"].Select(m=>(string)m["normal"]).Where(n=>n!=null).ToArray();\n        foreach (var guid',1)
art = art.replace('!importer.assetPath.Contains("Normal")', '!normals.Any(n=>importer.assetPath.EndsWith(n+".png"))')
art = art.replace('if (importer.assetPath.Contains("Normal"))', 'if (normals.Any(n=>importer.assetPath.EndsWith(n+".png")))')
art = art.replace('Saved Battlecruiser hull + four source turret models', 'Saved Dauntless source hull')
start = art.index('            var bones=hull.GetComponentsInChildren<Transform>(true);')
end = art.index('            var bounds=BoundsOf(root);', start)
art = art[:start] + art[end:]
art = art.replace('bool additive = ((string)source["shader"]).Contains("Additive");', 'bool additive = ((string)source["shader"]).Contains("Additive") || ((string)source["shader"]).Contains("Shield");')
art = art.replace('texture.Contains("CorBC_") && texture.Contains("diffuse")', 'texture.Contains("Neutral_Dauntless_Cruiser") && !texture.Contains("_B")')
art = art.replace('bool hidden=(bool)', 'bool hidden=(bool)')
Save('BuildArt.cs', art)

game = Read('BuildGameplay.cs')
start = game.index('            foreach (var source in audit["hardpoints"]')
end = game.index('            var opaque =', start)
game = game[:start] + '''            int[] profilesByMount={2,2,2,2,2,2,2,2,5,5,5,5,11,11,11,11,14,14};
            for(int i=0;i<18;i++)
            {
                string socket="HP_"+(i+1).ToString("D2")+"_FP_01";
                var hp=new GameObject("DauntlessWeapon"+(i+1).ToString("D2")).AddComponent<WeaponHardPoint>();
                hp.transform.SetParent(mountGroup,false);hp.transform.position=Bone(socket).position;
                Configure(hp,id++,i>=16?8:0,profilesByMount[i],90);
                targetable.Add(hp);weapons.Add(hp);
                records.Add(new JObject{["id"]=hp.Id,["name"]=hp.name,["bone"]=socket,["profile"]=profilesByMount[i],["targetable"]=true});
            }
            string[] systems={"HP_Engine1","HP_Engine2","HP_Shield","HP_Hangar","Hull"};
            int[] systemTypes={1,1,2,4,10};
            for(int i=0;i<systems.Length;i++)
            {
                var hp=new GameObject(i==4?"DauntlessSensorArray":systems[i]).AddComponent<HardPoint>();
                hp.transform.SetParent(mountGroup,false);hp.transform.position=Bone(systems[i]).position;
                if(i==4)hp.transform.localPosition=new Vector3(0,bounds.max.y,bounds.size.z*.1f);
                Configure(hp,id++,systemTypes[i],0,0);targetable.Add(hp);
                records.Add(new JObject{["id"]=hp.Id,["name"]=hp.name,["bone"]=systems[i],["derivedPosition"]=i==4,["targetable"]=true});
            }
            if(weapons.Count!=18 || targetable.Count!=23)throw new InvalidOperationException("Expected 18 weapons and 5 subsystems.");
''' + game[end:]
game = game.replace('Set(data,"Hull",14000); Set(data,"Shields",8000); Set(data,"Speed",25);', 'Set(data,"Hull",10000); Set(data,"Shields",5500); Set(data,"Speed",17);')
game = game.replace('Set(data,"Hull",10000);', 'data.FindProperty("<ShipClass>k__BackingField").intValue=8;\n        Set(data,"Hull",10000);')
game = game.replace('8000f/2000f*13.333333f','5500f/2000f*13.333333f')
game = game.replace('Set(data,"NavigationRadius",80)', 'Set(data,"NavigationRadius",95)')
game = game.replace('hpHealth.arraySize=4','hpHealth.arraySize=6').replace('int[] types={0,1,2,4};float[] values={750,1500,1000,600};', 'int[] types={0,1,2,4,8,10};float[] values={750,1500,1000,600,500,600};').replace('for(int i=0;i<4;i++){var row=hpHealth', 'for(int i=0;i<6;i++){var row=hpHealth')
game = game.replace('intValue=34;', 'intValue=39;')
game = game.replace('bays.arraySize=3','bays.arraySize=2').replace('int[] squadrons={300,1,3}', 'int[] squadrons={300,3}').replace('for(int i=0;i<3;i++){var bay=', 'for(int i=0;i<2;i++){var bay=').replace('("reserve").intValue=6','("reserve").intValue=2')
game = game.replace('loadout.arraySize=3','loadout.arraySize=4').replace('int[] profiles={64,4,8}; int[] counts={4,8,5}', 'int[] profiles={2,5,11,14}; int[] counts={8,4,4,2}').replace('for(int i=0;i<3;i++){var row=loadout','for(int i=0;i<4;i++){var row=loadout')
game = game.replace('Dauntless gameplay saved: 20 targetable hardpoints, 17 weapons, X-Wing/A-Wing/Y-Wing hangar', 'Dauntless gameplay saved: 23 targetable hardpoints, 18 weapons, X-Wing/Y-Wing hangar')
Save('BuildGameplay.cs',game)
Save('Persist.cs',Read('Persist.cs'))
render=Read('Render.cs').replace('camera.orthographicSize=78','camera.orthographicSize=92')
Save('Render.cs',render)

register=Read('Register.cs').replace('SHIP_ID=306','SHIP_ID=310').replace('ABILITY_ID=34','ABILITY_ID=39')
register=register.replace('Dauntless Battlecruiser','Dauntless Heavy Cruiser')
register=register.replace('"Fleet reinforcement / Squadron support"','"Frontline capital combat"').replace('intValue=2000','intValue=5600').replace('intValue=81','intValue=60')
register=register.replace('intValue=2;\n        value.FindPropertyRelative("<Price>', 'intValue=3;\n        value.FindPropertyRelative("<Price>')
start=register.index('        value.FindPropertyRelative("description").stringValue=')
end=register.index('\n',start)
register=register[:start]+'''        value.FindPropertyRelative("description").stringValue="Heavy frontline cruiser with eight heavy turbolasers, four medium dual turbolasers, four heavy ion cannons and two point-defense lasers. Deploys X-Wing and Y-Wing squadrons. Sensor Jamming reduces nearby enemy accuracy by 20% for 8 seconds.";'''+register[end:]
start=register.index('        value.FindPropertyRelative("displayName").stringValue="Power to Shields";')
end=register.index('        var abilitySounds=',start)
register=register[:start]+'''        value.FindPropertyRelative("displayName").stringValue="Sensor Jamming";
        value.FindPropertyRelative("description").stringValue="Reduce enemy accuracy by 20% within 500 game units for 8 seconds. Sensor destruction ends the effect. Recovers in 40 seconds; overlapping jammers do not stack.";
        value.FindPropertyRelative("duration").floatValue=8;
        value.FindPropertyRelative("recoveryDelay").floatValue=40;
        value.FindPropertyRelative("range").floatValue=500;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;
        value.FindPropertyRelative("aiUse").intValue=0;
        value.FindPropertyRelative("settings").managedReferenceValue=new SensorJammingSettings();
        Save(catalog);
        var overlay=Load("Assets/Settings/Data/Ship/HardPointOverlayData.asset");
        var entries=overlay.FindProperty("entries");
        var sensor=Enumerable.Range(0,entries.arraySize).Select(i=>entries.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("hardPointType").intValue==10)??Append(entries);
        sensor.FindPropertyRelative("hardPointType").intValue=10;
        sensor.FindPropertyRelative("displayName").stringValue="Sensor Array";
        sensor.FindPropertyRelative("description").stringValue="Destroying it disables Sensor Jamming.";
        sensor.FindPropertyRelative("icon").objectReferenceValue=value.FindPropertyRelative("icon").objectReferenceValue;
        Save(overlay);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
'''+register[end:]
start=register.index('    static void Profile(')
end=register.index('    static SerializedObject Load(',start)
register=register[:start]+register[end:]
register=register.replace('Battlecruiser=306 registered: Rebellion level 2/population 3, views/data/placement/icons/matchups, Power to Shields=34 and two-burst rocket profile=64 with audio.', 'Dauntless=310 registered: Rebellion, views/data/placement/icons/matchups, Sensor Jamming=39 with audio and sensor overlay.')
Save('Register.cs',register)
