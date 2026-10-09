using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public sealed class BalanceHardpointsPopup : PopupWindowContent
    {
        private readonly BalanceUnit _unit;
        private readonly BalanceRegistration _registry;
        private readonly BalanceWindowState _state;
        private readonly Action<BalanceField, string> _edit;
        private ScrollView _list;

        public BalanceHardpointsPopup(BalanceUnit unit, BalanceRegistration registry, BalanceWindowState state, Action<BalanceField, string> edit)
        {
            _unit = unit; _registry = registry; _state = state; _edit = edit;
        }

        public override Vector2 GetWindowSize() => new Vector2(620, Math.Min(560, 100 + _unit.Mounts.Distinct().Count() * 100));

        public override void OnOpen()
        {
            VisualElement root = editorWindow.rootVisualElement;
            root.AddToClassList("balance-root"); root.AddToClassList("balance-hardpoints-popup");
            root.EnableInClassList("balance-light", !EditorGUIUtility.isProSkin);
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
            VisualElement heading = new VisualElement(); heading.AddToClassList("balance-hardpoints-heading"); root.Add(heading);
            Label title = new Label(_unit.Name + " · Hardpoints"); title.AddToClassList("balance-hardpoints-title"); heading.Add(title);
            heading.Add(new Button(() => editorWindow.Close()) { text = "Close", name = "hardpoints-close" });
            Label caption = new Label(_unit.Faction + " · " + _unit.Class + " · " + _unit.Mounts.Distinct().Count() + " hardpoints");
            caption.AddToClassList("balance-muted"); root.Add(caption);
            _list = new ScrollView { name = "hardpoints-popup-list", viewDataKey = "balance-hardpoints-popup-" + _unit.Id };
            root.Add(_list);
            BuildList();
            root.RegisterCallback<KeyDownEvent>(evt => { if (evt.keyCode == KeyCode.Escape) { editorWindow.Close(); evt.StopPropagation(); } });
        }

        private void BuildList()
        {
            Vector2 offset = _list.scrollOffset;
            _list.Clear();
            BalanceCompareSummary.BuildHardpoints(_list, _unit, _registry, _state, (field, value) =>
            {
                _edit(field, value);
                _list.schedule.Execute(BuildList);
            });
            _list.schedule.Execute(() => _list.scrollOffset = offset);
        }
    }
}
