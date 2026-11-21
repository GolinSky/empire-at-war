using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Wing re-orders (W4), kept attack targets (W11) and stall by result (W10); relations only.</summary>
    [Category("EnemyAi")]
    public sealed class EnemyOrderRulesTests
    {
        private const float LEASH = 100f;

        [Test]
        public void EngagedWingNearItsPoint_KeepsItsFight()
        {
            Vector3 point = new Vector3(10f, 0f, 10f);

            Assert.That(EnemyWingOrderRules.KeepsFight(true, point + Vector3.right * (LEASH * 0.5f), point, LEASH),
                Is.True);
            Assert.That(EnemyWingOrderRules.KeepsFight(false, point, point, LEASH), Is.False,
                "An idle wing takes its order again.");
            Assert.That(EnemyWingOrderRules.KeepsFight(true, point + Vector3.right * (LEASH * 2f), point, LEASH),
                Is.False, "A wing chased past its leash is called back.");
        }

        [Test]
        public void WingPulledFromAReachedHold_ReturnsByMove()
        {
            Assert.That(EnemyWingOrderRules.ReturnsByMove(reachedHold: true, insideEntry: false), Is.True);
            Assert.That(EnemyWingOrderRules.ReturnsByMove(reachedHold: false, insideEntry: true), Is.True);
            Assert.That(EnemyWingOrderRules.ReturnsByMove(reachedHold: false, insideEntry: false), Is.False,
                "A wing still on its way attack-moves to the hold.");
        }

        [Test]
        public void ShipKeepsItsTarget_UntilItIsDestroyedCloakedOrOutOfReach()
        {
            const float reach = 50f;
            Vector3 ship = Vector3.zero;
            Vector3 near = Vector3.right * reach;
            Vector3 far = Vector3.right * (reach * EnemyAttackTargetRules.REACH_FACTOR * 2f);

            Assert.That(EnemyAttackTargetRules.KeepsTarget(false, false, ship, near, reach), Is.True);
            Assert.That(EnemyAttackTargetRules.KeepsTarget(true, false, ship, near, reach), Is.False);
            Assert.That(EnemyAttackTargetRules.KeepsTarget(false, true, ship, near, reach), Is.False);
            Assert.That(EnemyAttackTargetRules.KeepsTarget(false, false, ship, far, reach), Is.False);
            Assert.That(EnemyAttackTargetRules.KeepsTarget(false, false, ship, near, 0f), Is.False,
                "A ship without guns has nothing to keep.");
        }

        [Test]
        public void OnlyASiteTurningToTheTeam_IsStallProgress()
        {
            Assert.That(EnemyShipTaskGroupPlanner.IsStallProgress(3, false, 2, false), Is.True, "A site was taken.");
            Assert.That(EnemyShipTaskGroupPlanner.IsStallProgress(3, false, 3, true), Is.True, "The zone is now held.");
            Assert.That(EnemyShipTaskGroupPlanner.IsStallProgress(3, true, 3, true), Is.False,
                "Destroyed or rebuilt structures change no site.");
            Assert.That(EnemyShipTaskGroupPlanner.IsStallProgress(2, true, 3, true), Is.False, "Losing a site is no progress.");
        }
    }
}
