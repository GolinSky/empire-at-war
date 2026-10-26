"""Adapt the verified Nebula asset workflow to the Titan's audited layout."""
from pathlib import Path

ROOT = Path('Tools/Blender/Titan')
def Read(name):
    return (Path('Tools/Blender/Nebula')/name).read_text().replace('Nebula', 'Titan').replace('RebellionShips', 'EmpireShips').replace('Ships/Rebellion/Titan', 'Ships/Empire/Titan').replace('Ship/Rebellion/Titan', 'Ship/Empire/Titan').replace('Wrecks/Rebellion/Titan', 'Wrecks/Empire/Titan').replace('ShipIcon/Rebellion/Titan', 'ShipIcon/Empire/Titan')

art = Read('BuildArt.cs').replace('const float LENGTH = 160;', '')
art = art.replace('var bounds=BoundsOf(root); float scale=LENGTH/bounds.size.z;', '''var reference=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Republic/VenatorShipView.prefab");
            var referencePoints=reference.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterial.shader.name=="EmpireAtWar/Ship Lit").SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>reference.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            float length=(referencePoints.Max(v=>v.z)-referencePoints.Min(v=>v.z))*4500f/1137f;
            var bounds=BoundsOf(root); float scale=length/bounds.size.z;''')
art = art.replace('(texture.Contains("Prototype_Main") || texture.Contains("Prototype_Second"))', 'texture.Contains("Empire_Titan_Star_Destroyer")')
art = art.replace('material.SetFloat("_TeamMaskStrength",0);', 'material.SetTexture("_TeamMaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES+texture+"_TeamMask.png")); material.SetFloat("_TeamMaskStrength",0);')
art = art.replace('material.SetFloat("_TeamLiveryStrength",hullPaint?1:0)', 'material.SetFloat("_TeamLiveryStrength",0)')
art = art.replace('Saved Titan hull and fifteen source turret models', 'Saved Titan hull and 106 source turret attachments')
art = art.replace('if(hidden) {', 'if(sourceName=="Stealth")renderer.enabled=false;\n            if(hidden) {')
(ROOT/'BuildArt.cs').write_text(art)

game = Read('BuildGameplay.cs')
start = game.index('            Vector3 PartMuzzle(')
end = game.index('            var opaque =', start)
game = game[:start] + '''            Vector3 Muzzle(string hardpoint)
            {
                var hp=audit["hardpoints"].Single(h=>(string)h["name"]==hardpoint);
                var attachment=audit["attachments"].Single(a=>(string)a["hardpoint"]==hardpoint);
                var part=bones.Single(t=>t.name==(string)attachment["variant"]);
                string fire=(string)hp["values"]["Fire_Bone_A"][0];
                return part.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(fire,StringComparison.OrdinalIgnoreCase) && t.GetComponent<MeshFilter>()==null).position;
            }
            for(int i=1;i<=12;i++)Mount("QuadHeavyTurbolaser_"+i,0,20,Muzzle("HP_Allegiance_QTLT_"+i.ToString("D2")),"HP_Allegiance_QTLT_"+i.ToString("D2"));
            for(int i=1;i<=6;i++)Mount("MediumDualTurbolaser_"+i,0,5,Muzzle("HP_Allegiance_Small_Quad_Turret_"+i.ToString("D2")),"HP_Allegiance_Small_Quad_Turret_"+i.ToString("D2"));
            for(int i=1;i<=4;i++)Mount("HeavyIon_"+i,0,11,Muzzle("HP_Titan_TIC_"+i.ToString("D2")),"HP_Titan_TIC_"+i.ToString("D2"));
            for(int i=7;i<=8;i++)Mount("PointDefense_"+i,0,14,Muzzle("HP_Allegiance_Small_Quad_Turret_"+i.ToString("D2")),"HP_Allegiance_Small_Quad_Turret_"+i.ToString("D2"));
            for(int i=0;i<4;i++)Mount("Engine_"+i,1,0,new Vector3(bounds.size.x*(i-1.5f)*.075f,bounds.center.y,bounds.min.z+bounds.size.z*.04f),"Derived from stern engine geometry bounds");
            for(int i=0;i<3;i++)Mount("ShieldGenerator_"+i,2,0,Bone("Shield_Generator").position+Vector3.right*bounds.size.x*(i-1)*.035f,"Shield_Generator with derived lateral offsets");
            Mount("TractorBeam",7,0,Bone("HP_Trac_Bone").position,"HP_Trac_Bone");
            Mount("SensorArray",10,0,Bone("Superstructure_Top").position,"Superstructure_Top");
            if(weapons.Count!=24 || targetable.Count!=33)throw new InvalidOperationException("Expected 24 weapons + 9 subsystem hardpoints.");
''' + game[end:]
start = game.index('            var hangars=')
end = game.index('            foreach (var image', start)
game = game[:start] + '''            var launch=new GameObject("TitanDisabledLaunchPoint").transform;launch.SetParent(body,false);
            launch.localPosition=new Vector3(0,bounds.min.y-8,0);
            so=new SerializedObject(hangarComponent);
            so.FindProperty("launchPoint").objectReferenceValue=launch;
            so.FindProperty("hangarHardPoint").objectReferenceValue=null;
            so.FindProperty("isDestroyable").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            Assign(hangarComponent,"bayHardPoints",Array.Empty<UnityEngine.Object>());Assign(hangarComponent,"bayLaunchPoints",Array.Empty<UnityEngine.Object>());
''' + game[end:]
game = game.replace('Vector2.one*160', 'Vector2.one*bounds.size.z')
game = game.replace('Set(data,"Hull",16000); Set(data,"Shields",15000); Set(data,"Speed",20);','Set(data,"Hull",28000); Set(data,"Shields",18000); Set(data,"Speed",16);')
game = game.replace('hpHealth.arraySize=5','hpHealth.arraySize=5').replace('int[] types={0,1,2,4,7};float[] values={750,1500,1250,1200,1000};','int[] types={0,1,2,7,10};float[] values={1000,2000,1500,1500,1000};')
start = game.index('        var abilities=')
end = game.index('        Save(data);', start)
game = game[:start] + '''        var abilities=data.FindProperty("abilities");abilities.arraySize=2;abilities.GetArrayElementAtIndex(0).intValue=5;abilities.GetArrayElementAtIndex(1).intValue=22;
        data.FindProperty("hangarBays").arraySize=0;
        var loadout=data.FindProperty("weaponLoadout");loadout.arraySize=4;
        int[] profiles={20,5,11,14};int[] counts={12,6,4,2};
        for(int i=0;i<4;i++){var row=loadout.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=profiles[i];row.FindPropertyRelative("count").intValue=counts[i];}
''' + game[end:]
game = game.replace('31 targetable hardpoints, 24 weapons, two X-Wing hangars', '33 targetable hardpoints, 24 weapons, squadrons disabled')
game='\n'.join(line for line in game.split('\n') if 'FogVisibilityComponent' not in line or '"hardPoints"' not in line)
(ROOT/'BuildGameplay.cs').write_text(game)
for name in ('Persist.cs', 'Render.cs'):
    text = Read(name)
    if name == 'Render.cs':
        text=text.replace('orthographicSize=90', 'orthographicSize=270').replace('new Vector3(190,150,190)', 'new Vector3(570,450,570)').replace('farClipPlane=1000','farClipPlane=3000')
    (ROOT/name).write_text(text)

register = Read('Register.cs').replace('SHIP_ID=330','SHIP_ID=214').replace('const int ABILITY_ID=38;', '')
register=register.replace('Factions/Rebellion/RebellionFaction','Factions/Empire/EmpireFaction').replace('Titan Star Destroyer','Titan-class Battlecruiser').replace('intValue=7200','intValue=13000').replace('intValue=72;','intValue=100;').replace('"<MaxCount>k__BackingField").intValue=3','"<MaxCount>k__BackingField").intValue=1').replace('"<UnitCapacity>k__BackingField").intValue=8','"<UnitCapacity>k__BackingField").intValue=12')
start=register.index('        value.FindPropertyRelative("description").stringValue=')
end=register.index('\n',start)
register=register[:start]+'''        value.FindPropertyRelative("description").stringValue="Heavy Imperial battlecruiser with twelve quad heavy turbolasers, six medium dual turbolasers, four heavy ion cannons and two point defense lasers. No squadrons. Uses existing Power to Weapons and Tractor Beam abilities; requires escorts against bomber swarms.";'''+register[end:]
start=register.index('        var catalog=')
end=register.index('\n    }',start)
register=register[:start]+'''        AssetDatabase.SaveAssets();return "Titan=214 registered with Empire, data/view/placement/icons/matchups and existing abilities 5/22.";'''+register[end:]
(ROOT/'Register.cs').write_text(register)

stripes=Path('Tools/Blender/ISDIRemake/BuildStripes.cs').read_text().replace('BuildISDIRemakeStripes','BuildTitanStripes').replace('/ISDI/','/Titan/').replace('ISDI_TeamStripes','Titan_TeamStripes').replace('/ISDI.prefab','/Titan.prefab')
stripes=stripes.replace('new[] { "Assets/Prefabs/Models/Ships/Empire/Titan.prefab", "Assets/Prefabs/Models/Wrecks/Source/ISDIShipView.prefab" }','new[] { "Assets/Prefabs/Models/Ships/Empire/Titan.prefab" }')
stripes=stripes.replace('STRIPE_WIDTH = 2.6f','STRIPE_WIDTH = 7.3f').replace('new[] { -5f, 5f, 15f, 25f }','new[] { -14f, 14f, 42f, 70f }').replace('v.Position.x - 4f','v.Position.x - 11f').replace('22f - side','62f - side')
stripes=stripes.replace('r.enabled && !r.name.StartsWith("TeamStripes")','r.enabled && new[]{"Hull","Hull_Front","Superstructure","Superstructure_2","Superstructure_Top"}.Contains(r.name)')
(ROOT/'BuildStripes.cs').write_text(stripes)

verify=Read('Verify.cs').replace('SHIP_ID=330','SHIP_ID=214')
verify=verify.replace('targets.Length==31','targets.Length==33').replace('Range(0,31)','Range(0,33)').replace('31 unique','33 unique')
verify=verify.replace('new{type=3,count=8},new{type=4,count=6},new{type=32,count=4},new{type=65,count=2},new{type=14,count=4}', 'new{type=88,count=12},new{type=5,count=6},new{type=11,count=4},new{type=14,count=2}')
verify=verify.replace('new{type=1,count=2},new{type=2,count=2},new{type=4,count=2},new{type=7,count=1}', 'new{type=1,count=4},new{type=2,count=3},new{type=7,count=1},new{type=10,count=1}')
verify=verify.replace('floatValue==16000','floatValue==28000').replace('floatValue==15000','floatValue==18000').replace('floatValue==20','floatValue==16')
start=verify.index('        Check(bays.arraySize==2')
end=verify.index('        var source=',start)
verify=verify[:start]+'''        Check(bays.arraySize==0 && bayTargets.Length==0 && launches.Length==0 && !hangar.FindProperty("isDestroyable").boolValue,"Squadrons disabled without stale hangar bindings");
        Check(!targets.Any(t=>(int)t.HardPointType==4),"No gameplay hangar targets");
        var collider=root.GetComponents<BoxCollider>().Single();
        Check(Mathf.Abs(collider.size.z-472.4936f)<.02f,"Length matches measured Venator ratio");
        Check(guns.All(g=>new Bounds(collider.center,collider.size).Contains(root.transform.InverseTransformPoint(g.transform.position))),"Projectile origins within ship volume");
''' + verify[end:]
verify=verify.replace('Factions/Rebellion/RebellionFaction','Factions/Empire/EmpireFaction').replace('Titan Star Destroyer','Titan-class Battlecruiser').replace('intValue==7200','intValue==13000').replace('intValue==72','intValue==100')
start=verify.index('        var definition=')
end=verify.index('        var result=', start)
verify=verify[:start]+'''        var abilities=data.FindProperty("abilities");
        Check(abilities.arraySize==2 && abilities.GetArrayElementAtIndex(0).intValue==5 && abilities.GetArrayElementAtIndex(1).intValue==22,"Approved existing Power to Weapons and Tractor Beam");
        foreach(int id in new[]{5,22})Check(Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),id).FindPropertyRelative("value.settings").managedReferenceValue!=null,"Existing ability settings "+id);
        var paint=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("TeamStripes")).ToArray();
        Check(paint.Length>0 && paint.All(r=>r.sharedMaterial.GetFloat("_TeamLiveryStrength")==1 && r.sharedMaterial.GetFloat("_TeamMaskStrength")==0),"Derived worn team stripes");
        var gamePaint=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("TeamStripes")).ToArray();
        Check(gamePaint.Length==paint.Length && gamePaint.All(r=>References(Config(root,"TeamColorView").FindProperty("meshRenderers")).Contains(r)),"Gameplay team stripe ownership");
        Check(!EditorUtility.scriptCompilationFailed,"Unity compilation succeeded");
''' + verify[end:]
(ROOT/'Verify.cs').write_text(verify)
