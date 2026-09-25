using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public sealed class SuperWeaponsView : MonoBehaviour, ISuperWeaponsView
    {
        [SerializeField] private List<SuperWeaponButton> buttons;

        public event Action<SuperWeaponType> Pressed;

        public void Initialize()
        {
            foreach (SuperWeaponButton button in buttons)
            {
                button.Initialize();
                button.Pressed += HandlePressed;
            }
            SetPending(null);
        }

        public void Dispose()
        {
            foreach (SuperWeaponButton button in buttons)
            {
                button.Pressed -= HandlePressed;
                button.Dispose();
            }
        }

        public void SetState(SuperWeaponType type, SuperWeaponState state)
        {
            foreach (SuperWeaponButton button in buttons)
                if (button.WeaponType == type) button.SetState(state);
        }

        public void SetPending(SuperWeaponType? type)
        {
            foreach (SuperWeaponButton button in buttons)
                button.SetPending(type == button.WeaponType);
        }

        private void HandlePressed(SuperWeaponType type) => Pressed?.Invoke(type);
    }
}
