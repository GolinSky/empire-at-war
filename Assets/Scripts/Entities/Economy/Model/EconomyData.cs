using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Economy
{
    [CreateAssetMenu(fileName = nameof(EconomyData), menuName = "Data/Economy Data")]
    public class EconomyData : Data
    {
        [field: SerializeField] public float IncomeDelay { get; private set; }
        [field: SerializeField] public float StartMoneyAmount { get; private set; }
    }
}
