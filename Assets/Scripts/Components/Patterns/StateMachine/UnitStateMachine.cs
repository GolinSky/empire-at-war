using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class UnitStateMachine : StateMachine 
    {
        public UnitStateMachine(
            IAttackComponent attackComponent,
            IRadarModelObserver radarModel,
            IHealthModelObserver healthModel)
        {
            AttackComponent = attackComponent;
            RadarModel = radarModel;
            HealthModel = healthModel;
        }

        public IAttackComponent AttackComponent { get; }
        public IRadarModelObserver RadarModel { get; }
        public IHealthModelObserver HealthModel { get; }
    }
}
