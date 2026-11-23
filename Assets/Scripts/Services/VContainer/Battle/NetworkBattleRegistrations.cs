using EmpireAtWar.Commands.Game;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Diagnostics;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Services.TacticalMarkers;
using global::VContainer;
using global::VContainer.Unity;
using UnityEngine;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>
    /// Battle services that differ by network role. Offline and host simulate; the host also replicates and
    /// receives client commands. A client sends commands, renders replicated entities and never deals damage.
    /// </summary>
    public static class NetworkBattleRegistrations
    {
        public static void Register(IContainerBuilder builder, OnlineMatchService online, Transform root)
        {
            OnlineRole role = online.IsInBattle ? online.Role : OnlineRole.None;
            builder.Register<ReplicatedStateRegistry>(Lifetime.Singleton);
            builder.RegisterInstance(new NetworkStatusModel(role != OnlineRole.None));
            builder.RegisterInstance<IPingSource>(new NetworkPingSource(role));
            if (role == OnlineRole.Client)
            {
                RegisterClient(builder);
                RegisterDiagnostics(builder, root);
                return;
            }

            builder.Register<AuthorityCommandSink>(Lifetime.Singleton).As<IPlayerCommandSink>().AsSelf();
            builder.Register<MarkerResolutionWatcher>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<AttackDataFactory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SquadronLaunchSlots>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (role == OnlineRole.None)
            {
                builder.Register<ImmediateBattleStartGate>(Lifetime.Singleton).As<IBattleStartGate>();
                return;
            }

            builder.Register<NetworkTickService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostCommandReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HostTacticalMarkerSender>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HostTacticalMarkerReplySender>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HostBattleStartGate>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HostMatchStateSender>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostReplicatedStateSender>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostCombatEventSender>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostWeaponTargetSender>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostVisionRevealSender>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostShipRouteSender>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostUnitOrderSender>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HostBattleRecordSender>(Lifetime.Singleton);
            builder.Register<OnlineHostGameCommand>(Lifetime.Singleton).As<IGameCommand>();
            builder.Register<AbandonedPlayerTakeover>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OnlineBattleLifetime>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HostNetworkStats>(Lifetime.Singleton).As<INetworkRoleStats>();
            RegisterDiagnostics(builder, root);
        }

        // Network status overlay + per-battle _net.csv on every online machine (Multiplayer Connection Analysis).
        private static void RegisterDiagnostics(IContainerBuilder builder, Transform root)
        {
            builder.Register<NetworkStatsCollector>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            GameObject overlayRoot = new GameObject(nameof(NetworkStatusOverlayView));
            overlayRoot.transform.SetParent(root, false);
            builder.RegisterComponent(overlayRoot.AddComponent<NetworkStatusOverlayView>());
        }

        private static void RegisterClient(IContainerBuilder builder)
        {
            builder.Register<ClientReplicaBuffer>(Lifetime.Singleton);
            builder.Register<NetworkCommandSink>(Lifetime.Singleton).As<IPlayerCommandSink>();
            builder.Register<ClientAttackDataFactory>(Lifetime.Singleton).As<IAttackDataFactory>();
            builder.Register<ClosedSquadronLaunchSlots>(Lifetime.Singleton).As<ISquadronLaunchSlots>();
            builder.Register<ReplicaEntitySpawner>(Lifetime.Singleton);
            builder.Register<ClientReplicationReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientTacticalMarkerReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientTacticalMarkerReplyReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientReplicatedStateReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientCombatEventReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientVisionRevealReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientShipRouteReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientUnitOrderReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientBattleRecordReceiver>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ClientBattleStartGate>(Lifetime.Singleton).As<IBattleStartGate>();
            builder.Register<ClientNetworkStats>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OnlineBattleLifetime>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
