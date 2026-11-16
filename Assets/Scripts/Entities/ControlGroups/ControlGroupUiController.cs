using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.ControlGroups;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.ControlGroups
{
    public sealed class ControlGroupUiController : UiController, IInitializable, ITickable, ILateDisposable
    {
        private const float REFRESH_SECONDS = 0.15f;
        private const float SURVIVAL_TOP_OFFSET = 160f;
        private const float SIMULATION_TOP_OFFSET = 288f;
        private readonly ControlGroupService _groups;
        private readonly IControlGroupInput _input;
        private readonly ControlGroupAssignmentFlow _assignment;
        private readonly ControlGroupIconSelector _icons;
        private readonly ISelectionService _selection;
        private readonly SkirmishStateModel _battle;
        private readonly BattleDefinition _definition;
        private readonly List<IEntity> _members = new List<IEntity>();
        private ControlGroupUi _ui;
        private float _refreshAt;
        private bool _dirty = true;

        public ControlGroupUiController(IUiService uiService, ControlGroupService groups, IControlGroupInput input,
            ControlGroupAssignmentFlow assignment, ControlGroupIconSelector icons, ISelectionService selection,
            SkirmishStateModel battle, BattleDefinition definition) : base(uiService)
        {
            _groups = groups;
            _input = input;
            _assignment = assignment;
            _icons = icons;
            _selection = selection;
            _battle = battle;
            _definition = definition;
        }

        public void Initialize()
        {
            _ui = CreateUi<ControlGroupUi>();
            if (_definition.Mode == GameMode.Survival)
                ((RectTransform)_ui.transform).anchoredPosition = new Vector2(0, -SURVIVAL_TOP_OFFSET);
            else if (_definition.Mode == GameMode.BattleSimulation)
                ((RectTransform)_ui.transform).anchoredPosition = new Vector2(0, -SIMULATION_TOP_OFFSET);
            _ui.Hide();
            _groups.Changed += MarkDirty;
            _assignment.Assigned += HandleAssigned;
        }

        public void Tick()
        {
            bool visible = _input.IsPreviewHeld && _battle.State is BattleState.Running or BattleState.Paused;
            if (!visible)
            {
                if (_ui.IsVisible) _ui.Hide();
                return;
            }
            if (!_ui.IsVisible)
            {
                _ui.Show();
                _dirty = true;
            }
            if (!_dirty && Time.unscaledTime < _refreshAt) return;
            _dirty = false;
            _refreshAt = Time.unscaledTime + REFRESH_SECONDS;
            for (int index = 0; index < 10; index++)
            {
                int slot = (index + 1) % 10;
                _groups.GetMembers(slot, _members);
                _icons.Select(_members, out Sprite first, out Sprite second);
                bool selected = _members.Count > 0 && _members.Count == _selection.PlayerSelectionContext.Count;
                for (int i = 0; selected && i < _members.Count; i++)
                    selected = _selection.PlayerSelectionContext.Contains(_members[i]);
                _ui.RenderSlot(slot, _members.Count, first, second, selected);
            }
        }

        public void LateDispose()
        {
            _groups.Changed -= MarkDirty;
            _assignment.Assigned -= HandleAssigned;
            if (_ui != null)
            {
                if (Application.isPlaying) Object.Destroy(_ui.gameObject);
                else Object.DestroyImmediate(_ui.gameObject);
            }
        }

        private void MarkDirty() => _dirty = true;

        private void HandleAssigned(int slot)
        {
            _dirty = true;
            _ui.HighlightSlot(slot);
        }
    }
}
