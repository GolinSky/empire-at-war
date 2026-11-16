using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;

namespace EmpireAtWar.Services.ControlGroups
{
    public sealed class ControlGroupService : IInitializable, ILateDisposable
    {
        private readonly ControlGroupModel _model;
        private readonly IEntityLocator _entities;
        private readonly ISelectionService _selection;
        private readonly ILocalPlayer _localPlayer;
        private readonly List<IEntity> _recallMembers = new List<IEntity>();
        private readonly Predicate<long> _isPermanentlyInvalid;

        public event Action Changed
        {
            add => _model.Changed += value;
            remove => _model.Changed -= value;
        }

        public event Action<long> MemberRemoved;

        public ControlGroupService(ControlGroupModel model, IEntityLocator entities,
            ISelectionService selection, ILocalPlayer localPlayer)
        {
            _model = model;
            _entities = entities;
            _selection = selection;
            _localPlayer = localPlayer;
            _isPermanentlyInvalid = IsPermanentlyInvalid;
        }

        public void Initialize() => _entities.EntityRemoved += HandleEntityRemoved;

        public void LateDispose()
        {
            _entities.EntityRemoved -= HandleEntityRemoved;
            _model.Clear();
        }

        public long[] CaptureSelection()
        {
            var ids = new List<long>();
            var seen = new HashSet<long>();
            foreach (IEntity entity in _selection.PlayerSelectionContext.Entities)
                if (IsEligible(entity) && seen.Add(entity.Id)) ids.Add(entity.Id);
            return ids.ToArray();
        }

        public void GetMembers(int slot, List<IEntity> results)
        {
            Prune(slot);
            results.Clear();
            IReadOnlyList<long> ids = _model.GetMembers(slot);
            for (int i = 0; i < ids.Count; i++)
                if (_entities.TryGetEntity(ids[i], out IEntity entity) && IsEligible(entity))
                    results.Add(entity);
        }

        public int GetCount(int slot)
        {
            Prune(slot);
            int count = 0;
            IReadOnlyList<long> ids = _model.GetMembers(slot);
            for (int i = 0; i < ids.Count; i++)
                if (_entities.TryGetEntity(ids[i], out IEntity entity) && IsEligible(entity)) count++;
            return count;
        }

        public bool HasMembers(int slot)
        {
            Prune(slot);
            return _model.GetMembers(slot).Count > 0;
        }

        public bool HasSameMembers(int slot, IReadOnlyList<long> ids)
        {
            Prune(slot);
            return _model.HasSameMembers(slot, ids);
        }

        public int Assign(int slot, IReadOnlyList<long> ids)
        {
            var members = new List<long>();
            var seen = new HashSet<long>();
            for (int i = 0; i < ids.Count; i++)
                if (_entities.TryGetEntity(ids[i], out IEntity entity) && IsEligible(entity) && seen.Add(ids[i]))
                    members.Add(ids[i]);
            if (members.Count > 0) _model.Replace(slot, members);
            return members.Count;
        }

        public bool Recall(int slot)
        {
            GetMembers(slot, _recallMembers);
            if (_recallMembers.Count == 0) return false;
            _selection.SelectEntities(_recallMembers);
            return true;
        }

        public void Clear() => _model.Clear();

        private bool IsEligible(IEntity entity) => _localPlayer.IsLocal(entity.Owner) && entity.IsUnit() &&
            !entity.HealthModel.IsDestroyed && entity.TryGetFacade(out IEntitySelectionFacade _);

        private void Prune(int slot) => _model.Prune(slot, _isPermanentlyInvalid);

        private bool IsPermanentlyInvalid(long id) => !_entities.TryGetEntity(id, out IEntity entity) ||
            entity.HealthModel.IsDestroyed || !_localPlayer.IsLocal(entity.Owner);

        private void HandleEntityRemoved(IEntity entity)
        {
            _model.Remove(entity.Id);
            if (MemberRemoved != null) MemberRemoved(entity.Id);
        }
    }
}
