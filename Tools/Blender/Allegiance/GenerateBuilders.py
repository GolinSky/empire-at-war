"""Adapt the verified Executor builders for this unit's requested balance."""
from pathlib import Path

folder = Path('Tools/Blender/Allegiance')
s = Path('Tools/Blender/Executor/BuildExecutorShip.cs').read_text().replace('Executor', 'Allegiance')
s = s.replace('using EmpireAtWar.Editor.Rendering;', 'using EmpireAtWar.Editor.Rendering;\nusing EmpireAtWar.Editor;')
s = s.replace('var shieldTransform=shield.transform;', '''var shieldTransform=shield.transform;
            int weaponLayer=root.GetComponentsInChildren<WeaponHardPoint>(true).First().gameObject.layer;
            foreach(var hangar in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c.GetType().Name=="HangarComponent").ToArray()) UnityEngine.Object.DestroyImmediate(hangar.gameObject);
            Component(root,"AudioShipComponent").transform.SetParent(root.transform,true);''')
s = s.replace('new[]{-3f,0f,3f}', 'new[]{-5f,0f,5f}')
s=s.replace('string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase) && InSourceRig', 't.name==name && InSourceRig')
start = s.index('            var armed=')
end = s.index('            Assign(health,', start)
s = s[:start] + '''            int[] profiles={3,20,5,32};
            string[][] groups={
                new[]{"TL_01","TL_02","TL_03"},
                new[]{"QTL_01","QTL_03","QTL_05","QTL_07","QTL_09","QTL_11"},
                new[]{"QL_01","QL_07","QL_15","QL_18","QL_24","QL_30"},
                new[]{"IC_01","IC_02","IC_05","IC_06","ICT_01","ICT_04"}};
            for(int group=0;group<groups.Length;group++) foreach(string socket in groups[group])
            {
                var source=audit["hardpoints"].Single(h=>(string)h["Attachment_Bone"]==socket && h["Fire_Projectile_Type"]!=null && (string)h["name"]!="HP_ASD_Facing");
                var parent=!string.IsNullOrEmpty((string)source["Model_To_Attach"])?visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==socket+"_Turret").gameObject:visual;
                var muzzle=Bone(parent,(string)source["Fire_Bone_A"]);
                var point=CreateWeaponAt(body,"Battery_"+socket,muzzle.position,profiles[group],id++,180);
                point.gameObject.layer=weaponLayer; var battery=new SerializedObject(point);battery.FindProperty("mainBattery").boolValue=group<2;battery.ApplyModifiedPropertiesWithoutUndo(); targets.Add(point); weapons.Add(point);
                mappings.Add(new {name=point.name,attachment=socket,fireBone=muzzle.name,weaponType=profiles[group],id=point.Id,position=new[]{muzzle.position.x,muzzle.position.y,muzzle.position.z}});
            }
            var engines=visual.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Engine_Exhaust").bounds;
            for(int i=0;i<3;i++)
            {
                var point=CreateSystem(body,visual,"Engine"+(i+1),"Shield_Gen",1,id++);
                point.gameObject.layer=weaponLayer; point.transform.position=engines.center+Vector3.right*engines.extents.x*(i-1)*.7f; targets.Add(point);
            }
            var generator=Bone(visual,"Shield_Gen");
            for(int i=0;i<2;i++)
            {
                var point=CreateSystem(body,visual,"ShieldGenerator"+(i+1),"Shield_Gen",2,id++);
                point.gameObject.layer=weaponLayer; point.transform.position=generator.position+Vector3.right*(i==0?-1:1)*bounds.size.x*.04f; targets.Add(point);
            }
            foreach(var spec in new[]{new{name="TractorBeam",bone="Spawn_01",type=7},new{name="CommandArray",bone="objBridge",type=11}})
            {var point=CreateSystem(body,visual,spec.name,spec.bone,spec.type,id++);point.gameObject.layer=weaponLayer;targets.Add(point);}
''' + s[end:]
start = s.index('            var shieldMesh=')
end = s.index('            healthConfig.Update()', start)
s = s[:start] + '            ShieldHullBaker.Bake(root.transform,(Shield)shield);\n' + s[end:]
s = s.replace('Set(so,"Hull",200000);Set(so,"Shields",200000);Set(so,"Speed",15)', 'Set(so,"Hull",20000);Set(so,"Shields",16000);Set(so,"Speed",12)')
s = s.replace('Set(so,"ShieldRegenerateValue",200)', 'Set(so,"ShieldRegenerateValue",80)').replace('Set(so,"Range",500)', 'Set(so,"Range",750)').replace('Set(so,"BodyRotationMaxAngle",3)', 'Set(so,"BodyRotationMaxAngle",5)')
s = s.replace('list.arraySize=4;\n        int[] types={0,2,1,4};float[] hpValues={2500,4000,4000,4000};', 'list.arraySize=5;\n        int[] types={0,2,1,7,11};float[] hpValues={600,1200,1200,800,800};')
s = s.replace('for(int i=0;i<4;i++)', 'for(int i=0;i<5;i++)').replace('GetArrayElementAtIndex(0).intValue=10', 'GetArrayElementAtIndex(0).intValue=24')
start = s.index('        list=so.FindProperty("hangarBays")')
end = s.index('        so.FindProperty("<Wreck>', start)
s = s[:start] + '''        so.FindProperty("hangarBays").arraySize=0;
        list=so.FindProperty("weaponLoadout");list.arraySize=4;
        int[] weaponTypes={3,20,5,32},counts={3,6,6,6};
        for(int i=0;i<4;i++){var row=list.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=weaponTypes[i];row.FindPropertyRelative("count").intValue=counts[i];}
''' + s[end:]
s = s.replace('Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);', 'Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);\n            var spawn=new SerializedObject(Component(root,"UnitSpawnView"));spawn.FindProperty("hologramMaterial").objectReferenceValue=hologram;spawn.ApplyModifiedPropertiesWithoutUndo();')
s = s.replace('return "Allegiance: 174 automatic weapons; 59 targets; 200000/200000/15; two engines and two hangars; Laser Beam + Tractor Beam; own preview and wreck.";', 'return "Allegiance: 21 weapons, 28 targets, 20000/16000/12, no hangar, Power to Main Batteries + Tractor Beam.";')
start=s.index('    static WeaponHardPoint CreateSourceWeapon')
end=s.index('    static WeaponHardPoint CreateWeaponAt',start)
s=s[:start]+s[end:]
# Bow of the imported ALO points -Y, which exports to +Z. Broadside batteries use outward-facing arcs.
s=s.replace('point.transform.rotation=body.rotation;', 'point.transform.rotation=Quaternion.Euler(0,point.transform.localPosition.x<0?-90:90,0);')
s=s.replace('Mathf.Max(-180,side-width*.5f)', '-width*.5f').replace('Mathf.Min(180,side+width*.5f)', 'width*.5f')
s='\n'.join(line for line in s.splitlines() if 'Assign(Component(root,"FogVisibilityComponent"),"hardPoints"' not in line)+'\n'
s=s.replace('        float side=point.transform.localPosition.x<0?-90:90;','')
folder.joinpath('BuildShip.cs').write_text(s)

s=Path('Tools/Blender/Executor/RegisterExecutor.cs').read_text().replace('Executor','Allegiance').replace('207','213')
start=s.index('        var weapons=');end=s.index('        var icon=',start)
s=s[:start]+s[end:]
s=s.replace('Allegiance Star Dreadnought','Allegiance-class Battlecruiser').replace('Dreadnought / Fleet Flagship','Heavy Battlecruiser / Anti-capital')
start=s.index('        unit.FindPropertyRelative("description")');end=s.index('        unit.FindPropertyRelative("iconKey")',start)
s=s[:start]+'''        unit.FindPropertyRelative("description").stringValue="Heavy anti-capital battlecruiser: 20,000 hull, 16,000 shields; three heavy dual turbolasers, six heavy quad turbolasers, six medium dual turbolasers and six medium ion cannons. Three engines, two shield generators, tractor beam and command array. Power to Main Batteries and Tractor Beam. No fighter complement.";
'''+s[end:]
s=s.replace('key="MaxCount",value=1','key="MaxCount",value=3').replace('key="AvailableLevel",value=5','key="AvailableLevel",value=4').replace('key="Price",value=125000','key="Price",value=9200').replace('key="BuildTime",value=2500','key="BuildTime",value=85').replace('key="UnitCapacity",value=40','key="UnitCapacity",value=12')
start=s.index('    static void Profile');end=s.index('    static SerializedProperty Entry',start)
s=s[:start]+s[end:]
s=s.replace('six dedicated weapon profiles and firing audio','existing weapon profiles and abilities')
s=s.replace('Save(matchups);', '\n        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;\n        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=2;\n        string[] labels={"Cruisers","Capital ships","Bombers","Fighter swarms"};\n        string[] iconPaths={"Assets/Art/Textures/Ui/Icons/ShipIcon/MonCalCruiserIcon.png","Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIIcon.png","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEFighterIcon.png"};\n        for(int i=0;i<4;i++){var row=(i<2?strong:weak).GetArrayElementAtIndex(i%2);var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long fileId);row.FindPropertyRelative("label").stringValue=labels[i];row.FindPropertyRelative("iconKey").stringValue=guid+":"+fileId;}Save(matchups);')
folder.joinpath('Register.cs').write_text(s)
