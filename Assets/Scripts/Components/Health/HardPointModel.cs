﻿using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public interface IHardPointModel
    {
        event Action OnHardPointHealthChanged;
        HardPointType HardPointType { get; }

        float HealthPercentage { get; }
        int Id { get; }
        bool IsDestroyed { get; }
        
        Vector3 Position { get; }
    }

    [Serializable]
    public class HardPointModel : IHardPointModel
    {
        [field: SerializeField] public int Id { get; private set; }
        [field: SerializeField] public HardPointType HardPointType { get; private set; }

        private float _originHealth;
        private float _health;
        private HardPointView _hardPointView;

        public event Action OnHardPointHealthChanged;
        public float HealthPercentage { get; private set; } = 1f;

        public float Health => _health;
        
        public bool IsDestroyed => HealthPercentage <= 0f;
        public Vector3 Position => _hardPointView.Position;

        public void SetHealth(float health)
        {
            _originHealth = health;
            _health = health;
            HealthPercentage = health;
            _hardPointView.UpdateData(HealthPercentage);
        }

        public void ApplyDamage(float damage)
        {
            _health -= damage;
            HealthPercentage = _health / _originHealth;
            OnHardPointHealthChanged?.Invoke();
            _hardPointView.UpdateData(HealthPercentage);
        }


#if UNITY_EDITOR
        public void SetDataFromEditor(int id, HardPointType hardPointType)
        {
            Id = id;
            HardPointType = hardPointType;
        }
#endif
        public void SetData(HardPointView hardPointView)
        {
            _hardPointView = hardPointView;
            Id = hardPointView.Id;
            HardPointType = hardPointView.HardPointType;
        }

        public float TryApplyDamage(float damage)
        {
            if (_health >= damage)
            {
                ApplyDamage(damage);
                return 0.0f;
            }

            float damageLeft = damage - _health;
            ApplyDamage(_health);
            return damageLeft;
        }
    }
}