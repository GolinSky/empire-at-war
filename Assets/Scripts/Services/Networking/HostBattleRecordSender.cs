using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Saves;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Host: puts the battle time and losses so far into each client's baseline.</summary>
    public sealed class HostBattleRecordSender
    {
        private readonly BattleClock _clock;
        private readonly BattleLossTracker _losses;

        public HostBattleRecordSender(BattleClock clock, BattleLossTracker losses)
        {
            _clock = clock;
            _losses = losses;
        }

        public void SendBaseline(NetworkConnectionToClient connection) =>
            connection.Send(new BattleRecordMessage { Json = BattleRecordSnapshot.Capture(_clock, _losses) });
    }
}
