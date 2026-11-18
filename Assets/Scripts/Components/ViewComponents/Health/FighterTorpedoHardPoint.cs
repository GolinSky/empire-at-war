using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Models.Health;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class FighterTorpedoHardPoint : WeaponHardPoint
    {
        [SerializeField] private int fighterIndex;
        private IFighterAttackRunObserver _attackRuns;
        private int _firedRun;
        internal override bool ShipsOnly => true;
        // One torpedo per attack run: outside a fresh run the mount would refuse, so it skips target selection.
        internal override bool CanStartSalvo
        {
            get
            {
                int run = _attackRuns.GetAttackRun(fighterIndex);
                return run != 0 && run != _firedRun;
            }
        }

        [Inject]
        private void Construct(IFighterAttackRunObserver attackRuns)
        {
            _attackRuns = attackRuns;
        }

        public override void Attack(AttackData attackData, IHardPointModel hardPointModel)
        {
            if (!CanStartSalvo) return;
            if (TryAttack(attackData, hardPointModel)) _firedRun = _attackRuns.GetAttackRun(fighterIndex);
        }

        public override void RestartAttackSequence()
        {
            base.RestartAttackSequence();
            _firedRun = 0;
        }
    }
}
