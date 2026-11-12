using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Selection;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public sealed class SelectionSfx : IObserver<ISelectionSubject>, IInitializable, ILateDisposable
    {
        private readonly ISelectionService _selection;
        private readonly ILocalPlayer _localPlayer;
        private readonly IUiSfxService _sfx;
        private readonly HashSet<long>[] _previous = { new HashSet<long>(), new HashSet<long>() };
        private readonly int[] _clearedFrame = { -1, -1 };
        private readonly HashSet<long> _current = new HashSet<long>();

        public SelectionSfx(ISelectionService selection, ILocalPlayer localPlayer, IUiSfxService sfx)
        {
            _selection = selection;
            _localPlayer = localPlayer;
            _sfx = sfx;
        }

        public void Initialize() => _selection.AddObserver(this);

        public void LateDispose() => _selection.RemoveObserver(this);

        public void UpdateState(ISelectionSubject subject)
        {
            int scope = (int)subject.UpdatedScope;
            ISelectionContext context = subject.UpdatedScope == SelectionScope.Local
                ? subject.PlayerSelectionContext
                : subject.OtherSelectionContext;
            HashSet<long> previous = _previous[scope];

            // SelectionService may clear the old context immediately before replacing it.
            if (!context.HasSelectable && !subject.IsPruningSelection)
            {
                _clearedFrame[scope] = Time.frameCount;
                return;
            }

            _current.Clear();
            bool hostile = false;
            foreach (IEntity entity in context.Entities)
            {
                _current.Add(entity.Id);
                hostile |= _localPlayer.IsHostile(entity.Owner);
            }

            bool changed = (_clearedFrame[scope] >= 0 && _clearedFrame[scope] != Time.frameCount) ||
                           !previous.SetEquals(_current);
            _clearedFrame[scope] = -1;
            previous.Clear();
            previous.UnionWith(_current);
            if (subject.IsPruningSelection || !changed || context.SelectionType == SelectionType.Terrain)
                return;

            _sfx.RequestSelect(hostile ? UiSelectSfx.Enemy : context.SelectionType switch
            {
                SelectionType.Base => UiSelectSfx.Station,
                SelectionType.Ship => UiSelectSfx.Ship,
                _ => UiSelectSfx.Unit
            });
        }
    }
}
