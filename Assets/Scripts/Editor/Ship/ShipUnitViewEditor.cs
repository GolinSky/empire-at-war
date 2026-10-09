using System.Linq;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Editor.Ship
{
    [EmpireAtWar.Editor.EditorToolInfo("Select a ship-view root GameObject first. Assigns sequential hardpoint IDs using the existing setup command; save the edited scene or prefab afterwards.")]
    public static class ShipUnitViewEditor
    {
        [MenuItem("Tools/Empire At War/Units/Ships/Set Up Ship Hardpoint IDs")]
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
