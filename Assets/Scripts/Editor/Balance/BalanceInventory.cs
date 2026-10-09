using System;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceInventory
    {
        public static BalanceRegistration Build()
        {
            BalanceRegistration registry = new BalanceRegistration();
            try
            {
                BalanceUnitAdapter.Register(registry);
                BalanceGlobalAdapter.Register(registry);
                BalanceAbilityAdapter.Register(registry);
                BalanceWeaponAdapter.Register(registry);
                BalanceSharedMountAdapter.Register(registry);
            }
            catch (Exception exception)
            {
                registry.Errors.Add(exception.Message);
            }
            return registry;
        }
    }
}
