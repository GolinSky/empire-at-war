using EmpireAtWar.Components.AttackComponent;
using LightWeightFramework.Model;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class UnitStateMachine : StateMachine 
    {
        public UnitStateMachine(IAttackComponent attackComponent, IModel model)
        {
            AttackComponent = attackComponent;
            Model = model;
        }

        public IModel Model { get; }
        public IAttackComponent AttackComponent { get; }
    }
}