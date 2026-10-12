"""Build Endurance using existing carrier, preview and wreck construction workflows."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/BuildShip.cs').read_text()
source = source.replace('ISDIRemake', 'Endurance').replace('ISDI', 'Endurance').replace('ImperatorShipView', 'HomeOneShipView')
start = source.index('            AddWeapon("HP_ISD_1_New_HICT_01"')
end = source.index('            Assign(Component(root,"WeaponComponent")', start)
source = source[:start] + '''            for(int i=0;i<4;i++) AddWeapon("MediumTurbolaser"+(i+1),"EndurancePart"+(i+2).ToString("00")+"/FP_01",4,true);
            AddWeapon("MediumIonCannon1","EndurancePart06/FP_01",32,true);
            AddWeapon("MediumIonCannon2","EndurancePart09/FP_01",32,true);
            for(int i=0;i<4;i++) AddWeapon("PointDefense"+(i+1),"EndurancePart"+(i+10).ToString("00")+"/FP_01",14,true);
            AddWeapon("PointDefense5","BL_02",14,true);
            AddWeapon("PointDefense6","BL_11",14,true);
            var opaque=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
            var opaqueBounds=BoundsOf(opaque);
            AddSystem("ShieldGenerator","Shield_Gen",2);
            AddSystemAt("EngineLeft",new Vector3(-opaqueBounds.size.x*.2f,0,opaqueBounds.min.z+5),1);
            AddSystemAt("EngineRight",new Vector3(opaqueBounds.size.x*.2f,0,opaqueBounds.min.z+5),1);
            AddSystem("TractorBeamLeft","BL_05",7);
            AddSystem("TractorBeamRight","BL_08",7);
            AddSystem("CommandArray","EndurancePart00/Core",11);
            string[] bayBones={"Hanger","HB_02","HB_03","HB_04"};
            foreach(string bone in bayBones) AddSystem("Hangar_"+bone,bone,4);
''' + source[end:]
start = source.index('            var hangar=new SerializedObject')
end = source.index('            var points=solidHull', start)
source = source[:start] + '''            var solidHull=hull.Where(r=>r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
            bounds=BoundsOf(solidHull);
            var box=root.GetComponents<BoxCollider>().Single();box.center=bounds.center;box.size=bounds.size;
            var hangarComponent=Component(root,"HangarComponent");
            var hangar=new SerializedObject(hangarComponent);
            var bays=targets.Where(h=>h.HardPointType==EmpireAtWar.Components.Ship.Health.HardPointType.Hangar).ToArray();
            var launches=new List<Transform>();
            foreach(var bay in bays)
            {
                var launch=new GameObject(bay.name+"LaunchPoint").transform;
                launch.SetParent(body,false);
                launch.position=new Vector3(bay.transform.position.x,bounds.min.y-8,bay.transform.position.z);
                launch.rotation=root.transform.rotation;
                launches.Add(launch);
            }
            hangar.FindProperty("launchPoint").objectReferenceValue=launches[0];
            hangar.FindProperty("hangarHardPoint").objectReferenceValue=bays[0];
            hangar.ApplyModifiedPropertiesWithoutUndo();
            Assign(hangarComponent,"bayHardPoints",bays);
            Assign(hangarComponent,"bayLaunchPoints",launches.ToArray());
''' + source[end:]
# Restrict hull-bone queries to the main rig so attached bridges cannot shadow them.
source = source.replace('var search=pieces.Length==1?visual.transform:visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==pieces[0]);', 'var search=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(pieces.Length==1?"EnduranceRig":pieces[0]));')
source = source.replace('return search.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(pieces.Last(),StringComparison.OrdinalIgnoreCase));', 'return search.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(pieces.Last(),StringComparison.OrdinalIgnoreCase) && t.GetComponents<Component>().Length==1 && (pieces.Length>1 || !t.GetComponentsInParent<Transform>(true).Any(p=>p.name.StartsWith("EndurancePart"))));')
start = source.index('        var data=new SerializedObject')
end = source.index('        var saved=AssetDatabase', start)
source = source[:start] + '''        const string WRECK="Assets/Prefabs/Models/Wrecks/EnduranceWreckView.prefab";
        const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/EnduranceReinforcementView.prefab";
        const string WRECK_DATA="Assets/Settings/Data/Ship/Wreck/EnduranceWreckData.asset";
        Directory.CreateDirectory("Assets/Art/Materials/Wrecks/Endurance");AssetDatabase.Refresh();
        BuildVariant(PREVIEW,"Assets/Prefabs/Ui/Reinforcement/HomeOneReinforcementView.prefab",false,bounds);
        BuildVariant(WRECK,"Assets/Prefabs/Models/Wrecks/HomeOneWreckView.prefab",true,bounds);
        Copy("Assets/Settings/Data/Ship/Wreck/HomeOneWreckData.asset",WRECK_DATA);
        var wd=Load(WRECK_DATA);wd.targetObject.name="EnduranceWreckData";
        wd.FindProperty("<Prefab>k__BackingField").objectReferenceValue=Component(AssetDatabase.LoadAssetAtPath<GameObject>(WRECK),"UnitWreckView");Save(wd);
        Copy("Assets/Settings/Data/Ship/HomeOneShipData.asset",DATA);
        var data=Load(DATA);data.targetObject.name="EnduranceShipData";
        SetFloat(data,"Hull",9000);SetFloat(data,"Shields",7000);SetFloat(data,"Speed",18);
        SetFloat(data,"ShieldRegenerateValue",7000f/600f);SetFloat(data,"NavigationRadius",105);
        SetFloat(data,"HullBottom",hullBottom);SetFloat(data,"HullTop",hullTop);
        SetFloat(data,"HangarInitialDelay",4);SetFloat(data,"HangarLaunchInterval",8);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=wd.targetObject;
        var healthRows=data.FindProperty("hardPointHealth");healthRows.arraySize=6;
        int[] types={0,1,2,4,7,11};float[] healthValues={400,600,600,600,400,500};
        for(int i=0;i<types.Length;i++){var row=healthRows.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=healthValues[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=2;abilities.GetArrayElementAtIndex(0).intValue=52;abilities.GetArrayElementAtIndex(1).intValue=22;
        var hangarRows=data.FindProperty("hangarBays");hangarRows.arraySize=4;
        int[] active={2,2,1,1};
        for(int i=0;i<4;i++){var row=hangarRows.GetArrayElementAtIndex(i);row.FindPropertyRelative("squadronType").intValue=300;row.FindPropertyRelative("maxActive").intValue=active[i];row.FindPropertyRelative("reserve").intValue=active[i]*2;}
''' + source[end:]
start = source.index('        return "Remake ISD I:')
end = source.index('\n    }', start)
source = source[:start] + '        return "Endurance saved: 12 targetable weapons, 10 systems, 4 independent X-Wing hangars; 6 active + 6 replacement squadrons. Bounds "+bounds;' + source[end:]

variant = Path('Tools/Blender/MC75Profundity/BuildGameplay.cs').read_text()
start = variant.index('    static void BuildVariant')
end = variant.index('    static void Configure', start)
variant = variant[start:end].replace('MC75Profundity','Endurance')
variant = variant.replace('AssetDatabase.CopyAsset(donor,path);', 'Copy(donor,path);')
variant = variant.replace('VISUAL', '"Assets/Prefabs/Models/Ships/Endurance.prefab"')
start = variant.index('                foreach(var source in renderers.Select')
end = variant.index('                var view=Component', start)
variant = variant[:start] + '''                foreach(var source in renderers.SelectMany(r=>r.sharedMaterials).Distinct())
                {
                    string materialPath="Assets/Art/Materials/Wrecks/Endurance/"+source.name+"_Wreck.mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(material==null){material=new Material(source){shader=Shader.Find("EmpireAtWar/Ship Wreck")};AssetDatabase.CreateAsset(material,materialPath);}
                    foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m==source?material:m).ToArray();
                }
''' + variant[end:]
variant = variant.replace('Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);', 'Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);var spawn=new SerializedObject(Component(root,"UnitSpawnView"));spawn.FindProperty("hologramMaterial").objectReferenceValue=hologram;spawn.ApplyModifiedPropertiesWithoutUndo();')
source = source[:-2] + '\n' + variant + '    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));\n}\n'
Path('Tools/Blender/Endurance/BuildShip.cs').write_text(source)
