using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Popups;

namespace EmpireAtWar.Services.ControlGroups
{
    public sealed class ControlGroupAssignmentFlow : IInitializable, ILateDisposable, IObserver<BattleState>
    {
        private readonly ControlGroupService _groups;
        private readonly IControlGroupInput _input;
        private readonly IInputLock _inputLock;
        private readonly IConfirmPopupService _popups;
        private readonly IBattleNotifications _notifications;
        private readonly SkirmishStateModel _battle;
        private readonly HashSet<long> _removedCandidates = new HashSet<long>();
        private IDisposable _pendingLock;
        private PopupHandle _popup;
        private int _version;

        public event Action<int> Assigned;

        public ControlGroupAssignmentFlow(ControlGroupService groups, IControlGroupInput input,
            IInputLock inputLock, IConfirmPopupService popups, IBattleNotifications notifications,
            SkirmishStateModel battle)
        {
            _groups = groups;
            _input = input;
            _inputLock = inputLock;
            _popups = popups;
            _notifications = notifications;
            _battle = battle;
        }

        public void Initialize()
        {
            _input.SlotRequested += HandleSlot;
            _groups.MemberRemoved += HandleMemberRemoved;
            _battle.AddObserver(this);
        }

        public void LateDispose()
        {
            _input.SlotRequested -= HandleSlot;
            _groups.MemberRemoved -= HandleMemberRemoved;
            _battle.RemoveObserver(this);
            CancelPending();
        }

        public void UpdateState(BattleState state)
        {
            if (state is not (BattleState.Running or BattleState.Paused))
                CancelPending();
        }

        private void HandleSlot(int slot, bool assign)
        {
            if (_pendingLock != null || _inputLock.IsLocked ||
                _battle.State is not (BattleState.Running or BattleState.Paused))
                return;

            if (!assign)
            {
                if (!_groups.Recall(slot))
                    Notify(slot, _groups.HasMembers(slot)
                        ? $"No units available in Squad {slot}." : $"Squad {slot} is empty.");
                return;
            }

            long[] candidates = _groups.CaptureSelection();
            if (candidates.Length == 0)
            {
                Notify(slot, "Select your units first.");
                return;
            }

            if (_groups.HasSameMembers(slot, candidates))
            {
                Assigned?.Invoke(slot);
                return;
            }
            int oldCount = _groups.GetCount(slot);
            if (!_groups.HasMembers(slot))
            {
                Commit(slot, candidates);
                return;
            }

            int version = ++_version;
            _removedCandidates.Clear();
            _pendingLock = _inputLock.Acquire();
            try
            {
                _popup = _popups.ShowAccept(new AcceptPopupContent($"Replace Squad {slot}?",
                    $"Replace its {oldCount} units with the {candidates.Length} selected units?",
                    "Replace", "Cancel"), result => Complete(version, slot, candidates, result));
            }
            catch
            {
                CancelPending();
                throw;
            }
        }

        private void Complete(int version, int slot, long[] candidates, ConfirmPopupResult result)
        {
            if (version != _version || _pendingLock == null)
                return;
            ++_version;
            _popup = null;
            IDisposable pendingLock = _pendingLock;
            _pendingLock = null;
            pendingLock.Dispose();
            if (result == ConfirmPopupResult.Accepted &&
                _battle.State is BattleState.Running or BattleState.Paused)
            {
                var survivingCandidates = new List<long>(candidates.Length);
                foreach (long id in candidates)
                    if (!_removedCandidates.Contains(id)) survivingCandidates.Add(id);
                Commit(slot, survivingCandidates.ToArray(), candidates.Length);
            }
            _removedCandidates.Clear();
        }

        private void Commit(int slot, long[] candidates, int originalCount = -1)
        {
            int count = _groups.Assign(slot, candidates);
            if (count == 0)
                Notify(slot, "Squad unchanged: selected units are no longer available.");
            else
            {
                Assigned?.Invoke(slot);
                if (count != (originalCount < 0 ? candidates.Length : originalCount))
                    Notify(slot, $"Squad {slot} assigned: {count} surviving units.");
            }
        }

        private void CancelPending()
        {
            ++_version;
            PopupHandle popup = _popup;
            IDisposable pendingLock = _pendingLock;
            _popup = null;
            _pendingLock = null;
            _removedCandidates.Clear();
            try
            {
                if (popup != null) _popups.Close(popup);
            }
            finally
            {
                if (pendingLock != null) pendingLock.Dispose();
            }
        }

        private void Notify(int slot, string message) =>
            _notifications.Post(BattleNotificationPriority.Feedback, message, $"squad-{slot}:{message}", message);

        private void HandleMemberRemoved(long id)
        {
            if (_pendingLock != null) _removedCandidates.Add(id);
        }
    }
}
