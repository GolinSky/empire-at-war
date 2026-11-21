using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Models.Factions;
using UnityEditor;

namespace EmpireAtWar.Editor
{
    /// <summary>Refits a ship's reinforcement preview whenever its ship view prefab is saved.</summary>
    public sealed class ShipReinforcementPreviewPostprocessor : AssetPostprocessor
    {
        private const string PREFAB_EXTENSION = ".prefab";

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<ShipType> changed = new List<ShipType>();
            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(ShipReinforcementPreviewSync.SHIP_VIEW_SUFFIX + PREFAB_EXTENSION, StringComparison.Ordinal))
                    continue;

                string name = Path.GetFileNameWithoutExtension(path);
                string shipName = name.Substring(0, name.Length - ShipReinforcementPreviewSync.SHIP_VIEW_SUFFIX.Length);
                if (Enum.TryParse(shipName, out ShipType shipType))
                    changed.Add(shipType);
            }

            if (changed.Count == 0) return;

            // Saving another prefab during this import callback would nest imports.
            EditorApplication.delayCall += () =>
            {
                foreach (ShipType shipType in changed)
                {
                    string result = ShipReinforcementPreviewSync.Sync(shipType, ShipReinforcementPreviewSync.LoadShipView(shipType));
                    if (result != ShipReinforcementPreviewSync.IN_SYNC)
                        UnityEngine.Debug.Log($"{shipType} reinforcement preview refit to its ship view: {result}");
                }
            };
        }
    }
}
