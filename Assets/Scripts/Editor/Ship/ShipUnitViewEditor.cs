﻿using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Repository;
using EmpireAtWar.Ship;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Weapon;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.Ship
{
    public static class ShipUnitViewEditor
    {
        private const string HARD_POINT_PATH = "WeaponHardPoint";

        [MenuItem("Custom/Ships/SetCorrectPosition")]
        public static void SetCorrectPosition()
        {
            AddressableRepository addressableRepository = new AddressableRepository();

            string[] shipNames = Enum.GetNames(typeof(ShipType));
            
            foreach (string shipName in shipNames)
            {
                ShipView view = addressableRepository.LoadComponent<ShipView>($"{shipName}{nameof(ShipView)}");
                ShipModel model = addressableRepository.Load<ShipModel>($"{shipName}{nameof(ShipModel)}");
                
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
            AddressableRepository addressableRepository = new AddressableRepository();

            ShipModel shipModel = addressableRepository.Load<ShipModel>($"{shipType}ShipModel");

            
            int counter = 0;
            foreach (IHardPointProvider unitProvider in shipUnits)
            {
                unitProvider.SetId(counter);
                counter++;
            }
            shipModel.HealthModel.SetHardPoints(shipUnits);
            EditorUtility.SetDirty(shipModel);
            AssetDatabase.SaveAssetIfDirty(shipModel);
            
        }
        
        [MenuItem("Custom/Ships/Refactor Hard Point")]
        public static void RefactorHardPoints()
        {
            Object selectionObject = Selection.objects.FirstOrDefault();
            AttackViewComponent attackViewComponent = selectionObject.GetComponent<AttackViewComponent>();

            List<TurretView> turretViews = attackViewComponent.GetComponentsInChildren<TurretView>().ToList();

            AddressableRepository addressableRepository = new AddressableRepository();

            GameObject weaponHardPointViewPrefab = addressableRepository.LoadPrefab(HARD_POINT_PATH);
            
            foreach (TurretView turretView in turretViews)
            {
                Object hardPointInstanceGO =
                    PrefabUtility.InstantiatePrefab(weaponHardPointViewPrefab, attackViewComponent.transform);
                WeaponHardPointView hardPointInstance = hardPointInstanceGO.GetComponent<WeaponHardPointView>();

                hardPointInstance.transform.localPosition = turretView.transform.localPosition;
                
                hardPointInstance.SetData(turretView.YAxisRange);
                hardPointInstanceGO.name = turretView.gameObject.name;
                
            }
        }
    }
}