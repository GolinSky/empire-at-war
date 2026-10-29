using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    [CreateAssetMenu(fileName = nameof(HardPointHealthSettings), menuName = "Data/HardPointHealthSettings")]
    public sealed class HardPointHealthSettings : Data
    {
        [SerializeField, Min(1f)] private float budgetRatio = HardPointHealthBudget.DEFAULT_BUDGET_RATIO;
        [Tooltip("Indexed by HardPointType.")]
        [SerializeField] private float[] typeWeights = HardPointHealthBudget.DefaultTypeWeights;
        [Tooltip("Indexed by ShipClass.")]
        [SerializeField] private float[] classMinimums = HardPointHealthBudget.DefaultClassMinimums;

        public HardPointHealthBudget CreateBudget() => new HardPointHealthBudget(budgetRatio, typeWeights, classMinimums);
    }
}
