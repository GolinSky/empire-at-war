using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmpireAtWar.Entities.Saves;

public static class VerifySaveUi
{
    public static string Run()
    {
        string[] paths = {"Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab", "Assets/Prefabs/Ui/MainMenu/PlayUi.prefab", "Assets/Prefabs/Ui/SkirmishMenu/PauseMenuUi.prefab"};
        int references=0;
        foreach(var path in paths)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var script in asset.GetComponentsInChildren<MonoBehaviour>(true).Where(s => s!=null && (s.GetType().Namespace=="EmpireAtWar.Entities.Saves" || s.GetType().Name=="PlayUi" || s.GetType().Name=="PauseMenuUi")))
            {
                var so=new SerializedObject(script); var p=so.GetIterator();
                while(p.NextVisible(true)) if(p.propertyType==SerializedPropertyType.ObjectReference && p.name!="m_Script")
                {
                    if(p.objectReferenceValue==null) throw new Exception(path+": missing "+p.propertyPath);
                    references++;
                }
            }
        }
        var browser=AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);
        var input=browser.GetComponentInChildren<TMP_InputField>(true);
        if(input.textComponent.enableAutoSizing || input.textComponent.transform.parent!=input.textViewport || input.textComponent.rectTransform.localScale!=Vector3.one) throw new Exception("Input geometry mismatch");
        if(input.textViewport.GetComponent<RectMask2D>()==null) throw new Exception("Input mask missing");
        var scroll=browser.GetComponentInChildren<ScrollRect>(true);
        if(scroll.horizontal || !scroll.vertical || scroll.content.GetComponent<VerticalLayoutGroup>()==null) throw new Exception("Invalid save list layout");
        var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(paths[0]));
        if(entry.address!="SaveBrowserUi") throw new Exception("Addressable key mismatch");
        return "Verified "+references+" serialized references, vertical scrolling, fixed-size masked input geometry, and SaveBrowserUi Addressable registration.";
    }
}
