using System.Threading;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Networking.Online.Messages;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: waits until every connected client has loaded (or <see cref="LOAD_TIMEOUT_SECONDS"/>), then sends each
    /// its baseline spawns and the begin message. A client that loads later (rejoin) gets the same at once.
    /// </summary>
    public sealed class HostBattleStartGate : IBattleStartGate, IInitializable, ILateDisposable
    {
        private const float LOAD_TIMEOUT_SECONDS = 60f;

        private readonly OnlineMatchService _online;
        private readonly NetworkTickService _ticks;
        private readonly HostReplicatedStateSender _states;
        private readonly HostWeaponTargetSender _weaponTargets;
        private readonly HostMatchStateSender _matchState;
        private readonly HostCombatEventSender _combat;
        private readonly HostShipRouteSender _routes;

        public HostBattleStartGate(OnlineMatchService online, NetworkTickService ticks, HostReplicatedStateSender states,
            HostWeaponTargetSender weaponTargets, HostMatchStateSender matchState, HostCombatEventSender combat,
            HostShipRouteSender routes)
        {
            _routes = routes;
            _weaponTargets = weaponTargets;
            _matchState = matchState;
            _combat = combat;
            _online = online;
            _ticks = ticks;
            _states = states;
        }

        public void Initialize() => _online.ClientLoaded += OnClientLoaded;

        public void LateDispose() => _online.ClientLoaded -= OnClientLoaded;

        public async Awaitable WaitAsync(CancellationToken cancellationToken)
        {
            float deadline = Time.unscaledTime + LOAD_TIMEOUT_SECONDS;
            while (!_online.AllClientsLoaded && Time.unscaledTime < deadline)
                await Awaitable.NextFrameAsync(cancellationToken);

            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection != NetworkServer.localConnection) Begin(connection);
            }

            _online.MarkBegun();
        }

        private void OnClientLoaded(NetworkConnectionToClient connection)
        {
            if (_online.HasBegun) Begin(connection);
        }

        private void Begin(NetworkConnectionToClient connection)
        {
            if (!_online.IsBattleLoaded(connection)) return;
            _ticks.SendBaseline(connection);
            _states.SendBaseline(connection);
            // After the spawns: targets refer to entities the client now has.
            _weaponTargets.SendBaseline(connection);
            _combat.SendBaseline(connection);
            _routes.SendBaseline(connection);
            _matchState.SendBaseline(connection);
            connection.Send(new BattleBeginMessage());
        }
    }
}
