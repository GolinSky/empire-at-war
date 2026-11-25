using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>
    /// Battle-wide slot registry. Claims are keyed by entity id and dropped when the attacker or the target leaves the
    /// battle; a destroyed holder stops counting at once. Runs where unit states run (the authority).
    /// </summary>
    public sealed class FocusFireSlots : IFocusFireSlots, IDisposable
    {
        private readonly IEntityLocator _locator;
        private readonly IPlayerRelations _relations;
        private readonly IFocusFireCaps _caps;
        private readonly Dictionary<long, IEntity> _claimedTargets = new Dictionary<long, IEntity>();
        private readonly Dictionary<long, List<IEntity>> _holders = new Dictionary<long, List<IEntity>>();

        public event Action<IEntity> Changed;

        public FocusFireSlots(IEntityLocator locator, IPlayerRelations relations, IFocusFireCaps caps)
        {
            _locator = locator;
            _relations = relations;
            _caps = caps;
            _locator.EntityRemoved += HandleEntityRemoved;
        }

        public void Dispose() => _locator.EntityRemoved -= HandleEntityRemoved;

        public int GetCap(IEntity target, bool isSquadron) => _caps.GetCap(target, isSquadron);

        public int CountHolders(PlayerId team, IEntity target, bool isSquadron)
        {
            if (!_holders.TryGetValue(target.Id, out List<IEntity> holders)) return 0;
            int count = 0;
            foreach (IEntity holder in holders)
                if (!holder.HealthModel.IsDestroyed && holder.HealthModel.HasUnits &&
                    holder.IsSquadron() == isSquadron && _relations.IsAllied(team, holder.Owner)) count++;
            return count;
        }

        public int FreeSlots(PlayerId team, IEntity target, bool isSquadron)
        {
            int cap = GetCap(target, isSquadron);
            if (cap == FocusFireCaps.UNLIMITED) return int.MaxValue;
            return Math.Max(0, cap - CountHolders(team, target, isSquadron));
        }

        public bool Holds(IEntity attacker, IEntity target) =>
            _claimedTargets.TryGetValue(attacker.Id, out IEntity held) && held.Id == target.Id;

        public bool HasSlotFor(IEntity attacker, IEntity target) =>
            Holds(attacker, target) || FreeSlots(attacker.Owner, target, attacker.IsSquadron()) > 0;

        public bool TryClaim(IEntity attacker, IEntity target)
        {
            if (Holds(attacker, target)) return true;
            Release(attacker.Id);
            if (FreeSlots(attacker.Owner, target, attacker.IsSquadron()) == 0) return false;
            _claimedTargets[attacker.Id] = target;
            if (!_holders.TryGetValue(target.Id, out List<IEntity> holders))
                _holders[target.Id] = holders = new List<IEntity>();
            holders.Add(attacker);
            Changed?.Invoke(target);
            return true;
        }

        public void Release(long attackerId)
        {
            if (!_claimedTargets.Remove(attackerId, out IEntity target)) return;
            List<IEntity> holders = _holders[target.Id];
            for (int i = holders.Count - 1; i >= 0; i--)
                if (holders[i].Id == attackerId) holders.RemoveAt(i);
            if (holders.Count == 0) _holders.Remove(target.Id);
            Changed?.Invoke(target);
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            Release(entity.Id);
            if (!_holders.Remove(entity.Id, out List<IEntity> holders)) return;
            foreach (IEntity holder in holders) _claimedTargets.Remove(holder.Id);
            Changed?.Invoke(entity);
        }
    }
}
