using EmpireAtWar.Entities.SuperWeapons;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class SuperWeaponReadyHintCondition : IHintCondition
    {
        private readonly ISuperWeaponModelObserver _superWeapons;

        public bool IsMet { get; private set; }

        public SuperWeaponReadyHintCondition(ISuperWeaponModelObserver superWeapons)
        {
            _superWeapons = superWeapons;
            _superWeapons.OnStateChanged += HandleStateChanged;
        }

        private void HandleStateChanged(SuperWeaponType type, SuperWeaponState state)
        {
            if (state == SuperWeaponState.Ready) IsMet = true;
        }

        public void Dispose() => _superWeapons.OnStateChanged -= HandleStateChanged;
    }
}
