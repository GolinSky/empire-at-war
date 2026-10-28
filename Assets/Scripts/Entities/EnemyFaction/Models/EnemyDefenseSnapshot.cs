using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public readonly struct EnemyDefenseSnapshot
    {
        public float Money { get; }
        public float IncomePerSecond { get; }
        public float ProductionReserve { get; }
        public EnemyStrategicSnapshot Strategic { get; }
        public ForceComposition Own { get; }
        public ForceComposition Hostile { get; }
        public IReadOnlyList<EnemyDefensePad> Pads { get; }
        public IReadOnlyList<EnemyDefenseOption> Options { get; }

        public EnemyDefenseSnapshot(float money, float incomePerSecond, float productionReserve,
            EnemyStrategicSnapshot strategic, ForceComposition own, ForceComposition hostile,
            IReadOnlyList<EnemyDefensePad> pads, IReadOnlyList<EnemyDefenseOption> options)
        {
            Money = money;
            IncomePerSecond = incomePerSecond;
            ProductionReserve = productionReserve;
            Strategic = strategic;
            Own = own;
            Hostile = hostile;
            Pads = pads;
            Options = options;
        }
    }
}
