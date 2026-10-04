using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Models.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class FighterTorpedoHardPoint : WeaponHardPoint
    {
        [SerializeField] private int fighterIndex;
        private IFighterAttackRunObserver _attackRuns;
        private int _firedRun;

        [Inject]
        private void Construct(IFighterAttackRunObserver attackRuns)
        {
            _attackRuns = attackRuns;
        }

        public override void Attack(AttackData attackData, IHardPointModel hardPointModel)
        {
            int run = _attackRuns.GetAttackRun(fighterIndex);
            if (run == 0 || run == _firedRun) return;
            _firedRun = run;
            base.Attack(attackData, hardPointModel);
        }
    }
}
