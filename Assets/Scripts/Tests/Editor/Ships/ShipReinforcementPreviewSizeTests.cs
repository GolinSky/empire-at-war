using System;
using System.Collections.Generic;
using EmpireAtWar.Editor;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ShipReinforcementPreviewSizeTests
    {
        private const string RESYNC_HINT = "Run Tools/Empire At War/Units/Ships/Sync Reinforcement Previews.";

        [Test]
        public void ShipPreviews_MatchShipViewHull()
        {
            List<string> mismatches = new List<string>();
            int checkedShips = 0;
            foreach (ShipType shipType in Enum.GetValues(typeof(ShipType)))
            {
                GameObject shipView = ShipReinforcementPreviewSync.LoadShipView(shipType);
                if (shipView == null) continue;

                GameObject preview = AssetDatabase.LoadAssetAtPath<GameObject>(
                    ShipReinforcementPreviewSync.GetPreviewPath(shipType));
                Bounds ship = ShipReinforcementPreviewSync.MeasureModel(shipView.transform);
                Bounds hologram = ShipReinforcementPreviewSync.MeasureModel(preview.transform);
                if (!ShipReinforcementPreviewSync.IsMatch(hologram, ship))
                    mismatches.Add($"{shipType}: preview {hologram} vs ship {ship}");
                checkedShips++;
            }

            Assert.That(checkedShips, Is.GreaterThan(0));
            Assert.That(mismatches, Is.Empty, RESYNC_HINT);
        }

        [Test]
        public void Fit_MovesAndScalesPreviewOntoShipHull()
        {
            GameObject root = new GameObject("Preview");
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = new Vector3(1f, 2f, 3f);
                visual.transform.localScale = new Vector3(2f, 1f, 4f);
                Bounds before = ShipReinforcementPreviewSync.MeasureModel(root.transform);
                Bounds target = new Bounds(new Vector3(0f, -5f, 0f), before.size * 1.5f);

                Assert.That(ShipReinforcementPreviewSync.IsMatch(before, target), Is.False);
                ShipReinforcementPreviewSync.Fit(root.transform, before, target);

                Assert.That(ShipReinforcementPreviewSync.IsMatch(
                    ShipReinforcementPreviewSync.MeasureModel(root.transform), target), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MeasureModel_IncludesRootScale()
        {
            GameObject root = new GameObject("Preview");
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = new Vector3(0f, 4f, 0f);
                Bounds unscaled = ShipReinforcementPreviewSync.MeasureModel(root.transform);

                root.transform.localScale = Vector3.one * 0.25f;
                Bounds scaled = ShipReinforcementPreviewSync.MeasureModel(root.transform);

                Assert.That(ShipReinforcementPreviewSync.IsMatch(scaled, unscaled), Is.False);
                Assert.That(scaled.size.x, Is.EqualTo(unscaled.size.x * 0.25f).Within(0.0001f));
                Assert.That(scaled.center.y, Is.EqualTo(unscaled.center.y * 0.25f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Fit_MovesAndScalesPreviewOntoShipHull_UnderScaledRoot()
        {
            GameObject root = new GameObject("Preview");
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                root.transform.localScale = Vector3.one * 0.3f;
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = new Vector3(1f, 2f, 3f);
                visual.transform.localScale = new Vector3(2f, 1f, 4f);
                Bounds before = ShipReinforcementPreviewSync.MeasureModel(root.transform);
                Bounds target = new Bounds(new Vector3(0f, -5f, 0f), before.size * 1.5f);

                ShipReinforcementPreviewSync.Fit(root.transform, before, target);

                Assert.That(ShipReinforcementPreviewSync.IsMatch(
                    ShipReinforcementPreviewSync.MeasureModel(root.transform), target), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
