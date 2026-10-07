using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PersistMC80Independence
{
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Independence persistence requires Edit Mode.");
        var ship=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/MC80IndependenceShipView.prefab");
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Ship/MC80IndependenceShipData.asset"));
        float angle=data.FindProperty("<BodyRotationMaxAngle>k__BackingField").floatValue;
        float bottom=float.PositiveInfinity, top=float.NegativeInfinity;
        var rotations=new[]{Quaternion.Euler(0,0,-angle),Quaternion.identity,Quaternion.Euler(0,0,angle)};
        foreach(var renderer in ship.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))
        foreach(var vertex in renderer.GetComponent<MeshFilter>().sharedMesh.vertices)
        {
            var point=ship.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
            foreach(var rotation in rotations)
            {
                float height=(rotation*point).y;
                bottom=Mathf.Min(bottom,height);top=Mathf.Max(top,height);
            }
            float peak=Mathf.Atan2(point.x,point.y)*Mathf.Rad2Deg;
            float trough=Mathf.Atan2(-point.x,-point.y)*Mathf.Rad2Deg;
            float radius=new Vector2(point.x,point.y).magnitude;
            if(Mathf.Abs(peak)<=angle) top=Mathf.Max(top,radius);
            if(Mathf.Abs(trough)<=angle) bottom=Mathf.Min(bottom,-radius);
        }
        data.FindProperty("<HullBottom>k__BackingField").floatValue=bottom;
        data.FindProperty("<HullTop>k__BackingField").floatValue=top;
        data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data.targetObject);
        AssetDatabase.SaveAssets();
        return "Saved complete bank envelope "+bottom+" .. "+top+" at +/-"+angle+" degrees.";
    }
}
