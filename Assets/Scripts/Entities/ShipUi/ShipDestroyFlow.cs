using System;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Entities.ShipUi
{
    public sealed class ShipDestroyFlow : IUiCancelHandler, IObserver<BattleState>
    {
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _localPlayer;
        private readonly IPlayerCommandSink _commands;
        private readonly SkirmishStateModel _battle;
        private readonly IMatchSession _session;
        private readonly IInputLock _inputLock;
        private readonly IUiCancelRouter _cancelRouter;
        private readonly IConfirmPopupService _confirmPopups;
        private readonly IUiPlacement _placement;
        private ShipDestroyMenuUi _menu;
        private IEntity _target;
        private string _displayName;
        private PopupHandle _confirmation;
        private IDisposable _gameplayLock;
        private int _version;

        public ShipDestroyFlow(IEntityLocator entities, ILocalPlayer localPlayer, IPlayerCommandSink commands,
            SkirmishStateModel battle, IMatchSession session, IInputLock inputLock, IUiCancelRouter cancelRouter,
            IConfirmPopupService confirmPopups, IUiPlacement placement)
        {
            _entities = entities;
            _localPlayer = localPlayer;
            _commands = commands;
            _battle = battle;
            _session = session;
            _inputLock = inputLock;
            _cancelRouter = cancelRouter;
            _confirmPopups = confirmPopups;
            _placement = placement;
        }

        private bool IsRemoteClient => _session.IsOnline && !_session.IsAuthority;
        private bool IsBattleActive => _battle.State is BattleState.Running or BattleState.Paused;

        public void Initialize(ShipDestroyMenuUi menu)
        {
            _menu = menu;
            _menu.Hide();
            _menu.transform.SetParent(_placement.ContextualSlot, false);
            _menu.Initialize();
            _menu.DestroyPressed += Confirm;
            _menu.Dismissed += Invalidate;
            _battle.AddObserver(this);
            if (IsRemoteClient)
                NetworkClient.OnDisconnectedEvent += Invalidate;
        }

        public void Open(long targetId, string displayName, Vector2 screenPosition)
        {
            if (_target != null || _inputLock.IsLocked ||
                !TryFindEligibleTarget(targetId, out IEntity target))
                return;

            _target = target;
            _displayName = displayName;
            _target.HealthModel.OnDestroy += Invalidate;
            _entities.EntityRemoved += HandleEntityRemoved;
            _gameplayLock = _inputLock.Acquire();
            _cancelRouter.Focus(this);
            _menu.Show(screenPosition);
        }

        public bool TryCancel()
        {
            if (_gameplayLock == null || _confirmation != null)
                return false;

            Invalidate();
            return true;
        }

        public void Invalidate()
        {
            if (_target == null)
                return;

            _version++;
            _target.HealthModel.OnDestroy -= Invalidate;
            _entities.EntityRemoved -= HandleEntityRemoved;
            _target = null;
            _displayName = null;
            _menu.Hide();
            _cancelRouter.Unfocus(this);
            PopupHandle confirmation = _confirmation;
            _confirmation = null;
            IDisposable gameplayLock = _gameplayLock;
            _gameplayLock = null;
            try
            {
                if (confirmation != null)
                    _confirmPopups.Close(confirmation);
            }
            finally
            {
                gameplayLock?.Dispose();
            }
        }

        public void Dispose()
        {
            Invalidate();
            if (IsRemoteClient)
                NetworkClient.OnDisconnectedEvent -= Invalidate;
            _battle.RemoveObserver(this);
            _menu.DestroyPressed -= Confirm;
            _menu.Dismissed -= Invalidate;
            _menu.Dispose();
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_menu.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(_menu.gameObject);
        }

        public void UpdateState(BattleState state)
        {
            if (state is not (BattleState.Running or BattleState.Paused))
                Invalidate();
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (entity == _target)
                Invalidate();
        }

        private void Confirm()
        {
            if (_gameplayLock == null || _confirmation != null)
                return;

            _menu.Hide();
            _cancelRouter.Unfocus(this);
            int version = _version;
            long targetId = _target.Id;
            try
            {
                _confirmation = _confirmPopups.ShowAccept(
                    new AcceptPopupContent("Destroy ship?", $"Destroy {_displayName}? This cannot be undone.", "Yes", "No"),
                    result => Complete(version, targetId, result));
            }
            catch
            {
                Invalidate();
                throw;
            }
        }

        private void Complete(int version, long targetId, ConfirmPopupResult result)
        {
            if (version != _version || _target == null)
                return;

            bool submit = result == ConfirmPopupResult.Accepted &&
                          TryFindEligibleTarget(targetId, out IEntity target) && target == _target;
            _confirmation = null;
            if (submit && IsRemoteClient)
            {
                // The replica stays alive until the host's death update arrives. Keep the target
                // reserved for this binding, while returning control of the battle to the player.
                _version++;
                IDisposable gameplayLock = _gameplayLock;
                _gameplayLock = null;
                gameplayLock.Dispose();
            }
            else
            {
                Invalidate();
            }

            if (submit)
                _commands.Submit(new DestroyShipCommand(_localPlayer.Id, targetId));
        }

        private bool TryFindEligibleTarget(long id, out IEntity entity)
        {
            entity = null;
            return IsBattleActive && (!IsRemoteClient || NetworkClient.isConnected) &&
                   !_localPlayer.Id.IsNone && _entities.TryGetEntity(id, out entity) &&
                   entity.Owner == _localPlayer.Id && !entity.HealthModel.IsDestroyed && entity.IsShip();
        }
    }
}
