﻿using System;
using UnityEngine;
using LightWeightFramework.Model;

namespace EmpireAtWar.Models.Economy
{
    public interface IEconomyModelObserver : IModelObserver
    {
        event Action<float> OnMoneyChanged;
        float Money { get; }
    }

    [CreateAssetMenu(fileName = "EconomyModel", menuName = "Model/EconomyModel")]
    public class EconomyModel : Model, IEconomyModelObserver
    {
        public event Action<float> OnMoneyChanged;

        [field: SerializeField] public float IncomeDelay { get; private set; }
        [field: SerializeField] public float StartMoneyAmount { get; private set; }
        
        private float _money;
        
        public float Money
        {
            get => _money;
            set
            {
                _money = value;
                OnMoneyChanged?.Invoke(_money);
            }
        }
    }
}