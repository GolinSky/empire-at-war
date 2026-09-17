using System.Linq;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Editor.Ship
{
    public static class ShipUnitViewEditor
    {
        [MenuItem("Custom/Ships/SetUpShipUnits")]
        public static void SetUpShipUnits()
        {
            GameObject selectionObject = (GameObject)Selection.objects.FirstOrDefault();

            if (!selectionObject.name.Contains("ShipView"))
            {
                Debug.LogError("This is not ship view root object");
                return;
            }
            
            IHardPointProvider[] shipUnits = selectionObject.GetComponentsInChildren<IHardPointProvider>();

            int counter = 0;
            foreach (IHardPointProvider unitProvider in shipUnits)
            {
                unitProvider.SetId(counter);
                counter++;
                EditorUtility.SetDirty(unitProvider.GameObject);
            }
            EditorUtility.SetDirty(selectionObject);
            
        }
        
    }
}
