using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.ControlGroups
{
    public sealed class ControlGroupModel
    {
        public const int SLOT_COUNT = 10;
        private readonly List<long>[] _members = new List<long>[SLOT_COUNT];
        private readonly IReadOnlyList<long>[] _readOnlyMembers = new IReadOnlyList<long>[SLOT_COUNT];

        public event Action Changed;

        public ControlGroupModel()
        {
            for (int slot = 0; slot < SLOT_COUNT; slot++)
            {
                _members[slot] = new List<long>();
                _readOnlyMembers[slot] = _members[slot].AsReadOnly();
            }
        }

        public IReadOnlyList<long> GetMembers(int slot) => _readOnlyMembers[slot];

        public bool HasSameMembers(int slot, IReadOnlyList<long> members)
        {
            var unique = new HashSet<long>(members);
            return unique.Count == _members[slot].Count && unique.SetEquals(_members[slot]);
        }

        public void Replace(int slot, IReadOnlyList<long> members)
        {
            if (HasSameMembers(slot, members)) return;
            List<long> target = _members[slot];
            target.Clear();
            var seen = new HashSet<long>();
            for (int i = 0; i < members.Count; i++)
                if (seen.Add(members[i])) target.Add(members[i]);
            NotifyChanged();
        }

        public void Remove(long id)
        {
            bool changed = false;
            for (int slot = 0; slot < SLOT_COUNT; slot++)
                changed |= _members[slot].Remove(id);
            if (changed) NotifyChanged();
        }

        public void Prune(int slot, Predicate<long> isInvalid)
        {
            if (_members[slot].RemoveAll(isInvalid) > 0) NotifyChanged();
        }

        public void Clear()
        {
            bool changed = false;
            for (int slot = 0; slot < SLOT_COUNT; slot++)
            {
                changed |= _members[slot].Count > 0;
                _members[slot].Clear();
            }
            if (changed) NotifyChanged();
        }

        private void NotifyChanged()
        {
            if (Changed != null) Changed();
        }
    }
}
