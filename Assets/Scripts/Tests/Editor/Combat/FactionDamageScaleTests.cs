using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class FactionDamageScaleTests
    {
        private const string MATRIX_PATH = "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset";

        [Test]
        public void GetFactionDamageMultiplier_AppliesOnlyToItsFactionAndDamageType()
        {
            DamageMatrixData matrix = ScriptableObject.CreateInstance<DamageMatrixData>();
            try
            {
                JsonUtility.FromJsonOverwrite(
                    @"{""factionDamageScales"":[{""faction"":2,""damageType"":4,""multiplier"":0.6}]}", matrix);

                Assert.That(matrix.GetFactionDamageMultiplier(FactionType.Rebellion, DamageType.Ion), Is.EqualTo(0.6f));
                Assert.That(matrix.GetFactionDamageMultiplier(FactionType.Empire, DamageType.Ion), Is.EqualTo(1f));
                Assert.That(matrix.GetFactionDamageMultiplier(FactionType.Rebellion, DamageType.Turbolaser), Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(matrix);
            }
        }

        [Test]
        public void ShippedMatrix_RebelIonIsAPenalty()
        {
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(MATRIX_PATH);

            Assert.That(matrix.GetFactionDamageMultiplier(FactionType.Rebellion, DamageType.Ion), Is.InRange(0f, 1f).And.LessThan(1f));
            Assert.That(matrix.GetFactionDamageMultiplier(FactionType.Empire, DamageType.Ion), Is.EqualTo(1f));
        }
    }
}
