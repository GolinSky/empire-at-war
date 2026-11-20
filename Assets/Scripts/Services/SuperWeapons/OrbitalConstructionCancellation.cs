using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Popups;

namespace EmpireAtWar.Services.SuperWeapons
{
    public sealed class OrbitalConstructionCancellation : IInitializable, ILateDisposable, IObserver<BattleState>
    {
        private readonly IPlayerFactionModelObserver _faction;
        private readonly PlayerSlot _owner;
        private readonly SkirmishStateModel _battle;
        private readonly IPlayerCommandSink _commands;
        private readonly IConfirmPopupService _popups;
        private readonly IInputLock _inputLock;
        private PopupHandle _popup;
        private string _pendingId;

        public OrbitalConstructionCancellation(IPlayerFactionModelObserver faction, PlayerSlot owner,
            SkirmishStateModel battle, IPlayerCommandSink commands, IConfirmPopupService popups, IInputLock inputLock)
        {
            _faction = faction;
            _owner = owner;
            _battle = battle;
            _commands = commands;
            _popups = popups;
            _inputLock = inputLock;
        }

        public bool IsPending => _pendingId != null;
        private bool IsBattleActive => _battle.State is BattleState.Running or BattleState.Paused;

        public void Initialize()
        {
            _battle.AddObserver(this);
            _faction.OnProductionChanged += HandleProductionChanged;
        }

        public void LateDispose()
        {
            _faction.OnProductionChanged -= HandleProductionChanged;
            _battle.RemoveObserver(this);
            Dismiss();
        }

        public void UpdateState(BattleState state)
        {
            if (!IsBattleActive) Dismiss();
        }

        public void Request(SuperWeaponUnitRequest request, Action onCanceled = null)
        {
            if (IsPending || _inputLock.IsLocked || !IsBattleActive) return;

            string id = request.Id;
            IDisposable gameplayLock = _inputLock.Acquire();
            _pendingId = id;
            try
            {
                _popup = _popups.ShowAccept(new AcceptPopupContent("Cancel orbital construction?",
                    $"Cancel construction of {request.FactionData.DisplayName} and refund its cost?",
                    "Cancel build", "Keep building"), result =>
                {
                    _popup = null;
                    _pendingId = null;
                    gameplayLock.Dispose();
                    if (result != ConfirmPopupResult.Accepted || !IsBattleActive ||
                        !ContainsOrbitalBuild(_faction.GetProductionQueueSnapshots(), id)) return;

                    _commands.Submit(new CancelPurchaseCommand(_owner.Id, id));
                    onCanceled?.Invoke();
                });
            }
            catch
            {
                _pendingId = null;
                gameplayLock.Dispose();
                throw;
            }
        }

        private void HandleProductionChanged(IReadOnlyList<ProductionQueueSnapshot> queues)
        {
            if (IsPending && !ContainsOrbitalBuild(queues, _pendingId)) Dismiss();
        }

        private void Dismiss()
        {
            if (_popup != null) _popups.Close(_popup);
        }

        private static bool ContainsOrbitalBuild(IReadOnlyList<ProductionQueueSnapshot> queues, string id)
        {
            foreach (ProductionQueueSnapshot queue in queues)
                if (queue.UnitRequest is SuperWeaponUnitRequest request && request.Id == id) return true;
            return false;
        }
    }
}
