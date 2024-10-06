using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.ViewComponents.Health;
using Unity.VisualScripting;
using UnityEditor;

namespace EmpireAtWar.Editor.Ship
{
    public static class ShipUnitViewEditor
    {
        [MenuItem("Custom/Ships/SetUpShipUnits")]
        public static void SetUpShipUnits()
        {
            IEnumerable<IShipUnitProvider> shipUnits = Selection.objects.Select(x => x.GetComponent<IShipUnitProvider>());

            int counter = 0;
            foreach (IShipUnitProvider unitProvider in shipUnits)
            {
                unitProvider.SetId(counter);
                counter++;
            }
        }
    }
}