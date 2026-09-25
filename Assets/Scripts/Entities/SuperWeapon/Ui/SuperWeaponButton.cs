using System;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public sealed class SuperWeaponButton : MonoBehaviour
    {
        [SerializeField] private SuperWeaponType weaponType;
        [SerializeField] private Button button;
        [SerializeField] private GameObject pendingHighlight;

        public SuperWeaponType WeaponType => weaponType;
        public event Action<SuperWeaponType> Pressed;

        public void Initialize() => button.onClick.AddListener(HandleClick);
        public void Dispose() => button.onClick.RemoveListener(HandleClick);
        public void SetState(SuperWeaponState state) => button.interactable = state == SuperWeaponState.Ready;
        public void SetPending(bool pending) => pendingHighlight.SetActive(pending);

        private void HandleClick() => Pressed?.Invoke(weaponType);
    }
}
