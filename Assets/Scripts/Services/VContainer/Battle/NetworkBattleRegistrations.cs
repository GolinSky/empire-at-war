using EmpireAtWar.Commands.Game;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Squadrons;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>
    /// Battle services that differ by network role. Offline and host simulate; the host also replicates and
    /// receives client commands. A client sends commands, renders replicated entities and never deals damage.
    /// </summary>
    public static class NetworkBattleRegistrations
    {
        public static void Register(IContainerBuilder builder, OnlineMatchService online)
        {
            OnlineRole role = online.IsInBattle ? online.Role : OnlineRole.None;
            builder.Register<ReplicatedStateRegistry>(Lifetime.Singleton);
            if (role == OnlineRole.Client)
            {
                RegisterClient(builder);
                return;
            }

            builder.Register<AuthorityCommandSink>(Lifetime.Singleton).As<IPlayerCommandSink>().AsSelf();
            builder.Register<AttackDataFactory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SquadronLaunchSlots>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (role == OnlineRole.None)
            {
                builder.Register<ImmediateBattleStartGate>(Lifetime.Singleton).As<IBattleStartGate>();
                return;
            }

            builder.Register<NetworkTickService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostCommandReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HostBattleStartGate>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HostMatchStateSender>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HostReplicatedStateSender>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostCombatEventSender>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OnlineHostGameCommand>(Lifetime.Singleton).As<IGameCommand>();
            builder.Register<AbandonedPlayerTakeover>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OnlineBattleLifetime>(Lifetime.Singleton).AsImplementedInterfaces();
        }

        private static void RegisterClient(IContainerBuilder builder)
        {
            builder.Register<ClientReplicaBuffer>(Lifetime.Singleton);
            builder.Register<NetworkCommandSink>(Lifetime.Singleton).As<IPlayerCommandSink>();
            builder.Register<ClientAttackDataFactory>(Lifetime.Singleton).As<IAttackDataFactory>();
            builder.Register<ClosedSquadronLaunchSlots>(Lifetime.Singleton).As<ISquadronLaunchSlots>();
            builder.Register<ReplicaEntitySpawner>(Lifetime.Singleton);
            builder.Register<ClientReplicationReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientReplicatedStateReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientCombatEventReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientBattleStartGate>(Lifetime.Singleton).As<IBattleStartGate>();
            builder.Register<HostLeftBattleExit>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OnlineBattleLifetime>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
