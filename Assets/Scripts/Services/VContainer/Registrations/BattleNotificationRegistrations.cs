using EmpireAtWar.Entities.BattleNotifications;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.BattleNotifications;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>
    /// Battle-scope notification feed, its top-center UI and the battle-wide sources. The feed is always registered
    /// so battle and player services can post; without the UI (simulations) it never accepts gameplay cards.
    /// Player-scope sources register with their feature: economy in the local human scope, capture sites and
    /// superweapons in their modules, survival in its mode.
    /// </summary>
    public static class BattleNotificationRegistrations
    {
        public static void Register(IContainerBuilder builder, BattleDefinition battle)
        {
            builder.Register<BattleNotificationFeed>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (battle.Mode == GameMode.BattleSimulation) return;
            builder.Register<BattleNotificationUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ThreatNotifier>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<MapNotifier>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<BattleOutcomeNotifier>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<AbilityTargetingNotifier>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
