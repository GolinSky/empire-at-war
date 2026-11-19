using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Saves;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Client: takes the host's battle time and losses from the baseline, then keeps counting locally.</summary>
    public sealed class ClientBattleRecordReceiver : IInitializable, ILateDisposable
    {
        private readonly BattleClock _clock;
        private readonly BattleLossTracker _losses;

        public ClientBattleRecordReceiver(BattleClock clock, BattleLossTracker losses)
        {
            _clock = clock;
            _losses = losses;
        }

        public void Initialize() => NetworkClient.RegisterHandler<BattleRecordMessage>(OnRecord);

        public void LateDispose() => NetworkClient.UnregisterHandler<BattleRecordMessage>();

        private void OnRecord(BattleRecordMessage message) => BattleRecordSnapshot.Apply(message.Json, _clock, _losses);
    }
}
