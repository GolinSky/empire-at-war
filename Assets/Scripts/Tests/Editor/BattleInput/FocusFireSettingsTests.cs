using System;
using System.Linq;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Shipped focus fire data: wiring and design intent only, never the tuned numbers.</summary>
    [Category("BattleInput")]
    public sealed class FocusFireSettingsTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/UnitOrderSettings.asset";

        [Test]
        public void ShippedCaps_CoverEveryClass_AndNeverShrinkWithHullTier()
        {
            UnitOrderSettings settings = AssetDatabase.LoadAssetAtPath<UnitOrderSettings>(SETTINGS_PATH);
            Assert.That(settings, Is.Not.Null, SETTINGS_PATH);

            foreach (ShipClass shipClass in Enum.GetValues(typeof(ShipClass)))
            foreach (bool isSquadron in new[] { false, true })
                Assert.That(settings.GetFocusFireCap(shipClass, isSquadron), Is.GreaterThanOrEqualTo(0),
                    $"{shipClass} squadron={isSquadron}");

            ShipClass[] hulls = Enum.GetValues(typeof(ShipClass)).Cast<ShipClass>()
                .Where(shipClass => shipClass.HullTier() > 0).OrderBy(shipClass => shipClass.HullTier()).ToArray();
            for (int i = 1; i < hulls.Length; i++)
            foreach (bool isSquadron in new[] { false, true })
                Assert.That(settings.GetFocusFireCap(hulls[i], isSquadron),
                    Is.GreaterThanOrEqualTo(settings.GetFocusFireCap(hulls[i - 1], isSquadron)),
                    $"{hulls[i]} offers fewer slots than {hulls[i - 1]} (squadron={isSquadron}).");
            Assert.That(settings.GetFocusFireCap(ShipClass.Structure, isSquadron: false), Is.EqualTo(FocusFireCaps.UNLIMITED),
                "Structures are exempt from the cap.");
        }

        [Test]
        [Category("EnemyAi")]
        public void DifficultyScale_IsAShareAndNeverFallsWithDifficulty()
        {
            float previous = 0f;
            foreach (EnemyAiDifficulty difficulty in Enum.GetValues(typeof(EnemyAiDifficulty)))
            {
                float scale = EnemyAiDifficultyProfile.Get(difficulty).FocusFireScale;
                Assert.That(scale, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), difficulty.ToString());
                Assert.That(scale, Is.GreaterThanOrEqualTo(previous), difficulty.ToString());
                previous = scale;
            }
        }
    }
}
