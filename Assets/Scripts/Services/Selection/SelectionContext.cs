using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using System.Collections.ObjectModel;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Selection;

namespace EmpireAtWar.Services.Battle
{
    public sealed class SelectionContext : ISelectionContext
    {
        private readonly ILocalPlayer _localPlayer;

        private readonly List<SelectionEntry> _entries = new List<SelectionEntry>();
        private readonly List<IEntity> _entities = new List<IEntity>();
        private readonly List<SelectionEntry> _replacementEntries = new List<SelectionEntry>();
        private readonly ReadOnlyCollection<IEntity> _readOnlyEntities;

        public IEntity Entity => _entries.Count > 0 ? _entries[0].Entity : null;
        public IReadOnlyList<IEntity> Entities => _readOnlyEntities;
        public IEntitySelectionFacade SelectionFacade =>
            _entries.Count > 0 ? _entries[0].Command : null;
        public SelectionType SelectionType { get; private set; } = SelectionType.None;
        public bool HasSelectable => _entries.Count > 0;
        public int Count => _entries.Count;
        public SelectionScope Scope { get; }

        public SelectionContext(ILocalPlayer localPlayer, SelectionScope scope)
        {
            Scope = scope;
            _localPlayer = localPlayer;
            _readOnlyEntities = _entities.AsReadOnly();
        }

        public bool Accepts(IEntity entity)
        {
            return _localPlayer.IsLocal(entity.Owner) == (Scope == SelectionScope.Local);
        }

        public bool Contains(IEntity entity)
        {
            return IndexOf(entity) >= 0;
        }

        public void Replace(IReadOnlyList<SelectionEntry> entries)
        {
            _replacementEntries.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                if (Accepts(entries[i].Entity) &&
                    !Contains(_replacementEntries, entries[i].Entity))
                {
                    _replacementEntries.Add(entries[i]);
                }
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                if (!Contains(_replacementEntries, _entries[i].Entity))
                {
                    _entries[i].Command.Select(false);
                }
            }

            for (int i = 0; i < _replacementEntries.Count; i++)
            {
                if (!Contains(_entries, _replacementEntries[i].Entity))
                {
                    _replacementEntries[i].Command.Select(true);
                }
            }

            _entries.Clear();
            _entities.Clear();
            for (int i = 0; i < _replacementEntries.Count; i++)
            {
                _entries.Add(_replacementEntries[i]);
                _entities.Add(_replacementEntries[i].Entity);
            }

            UpdateSelectionType();
        }

        public bool Remove(IEntity entity)
        {
            int index = IndexOf(entity);
            if (index < 0)
            {
                return false;
            }

            _entries[index].Command.Select(false);
            _entries.RemoveAt(index);
            _entities.RemoveAt(index);
            UpdateSelectionType();
            return true;
        }

        public void ResetCurrentSelectable()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Command.Select(false);
            }

            _entries.Clear();
            _entities.Clear();
            _replacementEntries.Clear();
            SelectionType = SelectionType.None;
        }

        private int IndexOf(IEntity entity)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Entity.Id == entity.Id)
                {
                    return i;
                }
            }

            return -1;
        }

        private void UpdateSelectionType()
        {
            if (_entries.Count == 0)
            {
                SelectionType = SelectionType.None;
                return;
            }

            SelectionType commonType = _entries[0].Command.SelectionType;
            for (int i = 1; i < _entries.Count; i++)
            {
                if (_entries[i].Command.SelectionType != commonType)
                {
                    SelectionType = SelectionType.None;
                    return;
                }
            }

            SelectionType = commonType;
        }

        private static bool Contains(IReadOnlyList<SelectionEntry> entries, IEntity entity)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Entity.Id == entity.Id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
