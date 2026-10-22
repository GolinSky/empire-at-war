using EmpireAtWar.Components.AttackComponent;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SquadronShieldBalanceTests
    {
        [Test]
        public void FighterLasers_ReduceShipShieldDamageWithoutReducingDogfightDamage()
        {
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(
                "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");

            Assert.That(matrix.GetShipShieldMultiplier(DamageType.FighterLaser), Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(matrix.GetShieldMultiplier(DamageType.FighterLaser), Is.EqualTo(0.5f));
            Assert.That(matrix.GetShipShieldMultiplier(DamageType.Laser), Is.EqualTo(0.5f));
        }
    }
}
