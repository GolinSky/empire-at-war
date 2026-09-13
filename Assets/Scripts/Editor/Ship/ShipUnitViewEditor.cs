using System;
using System.Linq;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Repository;
using EmpireAtWar.Ship;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Editor.Ship
{
    public static class ShipUnitViewEditor
    {
        [MenuItem("Custom/Ships/SetCorrectPosition")]
        public static void SetCorrectPosition()
        {
            AddressableAssetService addressableRepository = new AddressableAssetService();

            string[] shipNames = Enum.GetNames(typeof(ShipType));
            
            foreach (string shipName in shipNames)
            {
                ShipEntity view = addressableRepository.LoadComponent<ShipEntity>($"{shipName}ShipView");
                ShipComponentsData model = addressableRepository.Load<ShipComponentsData>($"{shipName}{nameof(ShipComponentsData)}");
                
            }
        }
        [MenuItem("Custom/Ships/SetUpShipUnits")]
        public static void SetUpShipUnits()
        {
            GameObject selectionObject = (GameObject)Selection.objects.FirstOrDefault();

            if (!selectionObject.name.Contains("ShipView"))
            {
                Debug.LogError("This is not ship view root object");
                return;
            }
            
            string shipType = selectionObject.name.Replace("ShipView", "");

            IHardPointProvider[] shipUnits = selectionObject.GetComponentsInChildren<IHardPointProvider>();
            AddressableAssetService addressableRepository = new AddressableAssetService();

            // ShipComponentsData shipModel = addressableRepository.Load<ShipComponentsData>($"{shipType}ShipComponentsData");

            
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
