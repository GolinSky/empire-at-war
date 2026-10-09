using System;
using System.Globalization;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceFieldView
    {
        public static VisualElement Create(BalanceField field, BalanceWindowState state, BalanceDraftUsage usage, Action<string> edit, Action select, bool bulk = false, Action bulkSelectionChanged = null)
        {
            VisualElement card = new VisualElement { userData = field.Key }; card.AddToClassList("balance-card");
            string current = field.Read();
            string value = field.DraftValue(state.Draft);
            bool changed = value != current;
            card.EnableInClassList("balance-changed", changed);
            VisualElement heading = new VisualElement(); heading.AddToClassList("balance-field-heading"); card.Add(heading);
            Label title = new Label((changed ? "• " : "") + field.Label) { tooltip = field.Context + " / " + field.Label };
            title.AddToClassList("balance-title"); heading.Add(title);
            Button details = new Button(select) { text = "Info", tooltip = "Source, canonical identity and all affected units" };
            details.AddToClassList("balance-info"); heading.Add(details);
            Label scope = new Label(field.Shared ? field.Owner + " · " + field.DescribeScope(usage.Users(field)) : field.Owner);
            scope.AddToClassList("balance-scope"); card.Add(scope);
            if (field.AliasWarning.Length != 0) card.Add(new Label(field.AliasWarning));
            VisualElement control = Control(field, value, usage, edit); control.AddToClassList("balance-editable");
            control.tooltip = "Original value: " + Display(field, current);
            bool sourceStaged = field.InheritsSource && state.Draft.Changes.Any(change => change.Key == field.SourceKey);
            control.SetEnabled(!sourceStaged); card.Add(control);
            if (sourceStaged) card.Add(new Label("Linked to staged shared source. Apply it before staging a distinct local override."));
            if (changed)
            {
                Label values = new Label("Original " + Display(field, current) + " → Draft " + Display(field, value));
                values.AddToClassList("balance-value"); card.Add(values);
            }
            string error = usage.Validate(field, value);
            if (error.Length != 0) card.Add(new HelpBox(error, HelpBoxMessageType.Error));
            if (bulk)
            {
                Toggle selected = new Toggle("Select for bulk edit") { value = state.BulkKeys.Contains(field.Key) };
                selected.RegisterValueChangedCallback(evt =>
                {
                    state.BulkKeys.Remove(field.Key); if (evt.newValue) state.BulkKeys.Add(field.Key);
                    if (bulkSelectionChanged != null) bulkSelectionChanged();
                });
                card.Add(selected);
            }
            return card;
        }

        public static VisualElement Control(BalanceField field, string value, BalanceDraftUsage usage, Action<string> edit)
        {
            switch (field.Kind)
            {
                case BalanceValueKind.Float:
                    FloatField number = new FloatField(field.Label) { value = float.Parse(value, CultureInfo.InvariantCulture), isDelayed = true };
                    number.RegisterValueChangedCallback(evt => edit(evt.newValue.ToString("R", CultureInfo.InvariantCulture)));
                    return number;
                case BalanceValueKind.Integer:
                    IntegerField integer = new IntegerField(field.Label) { value = int.Parse(value), isDelayed = true };
                    integer.RegisterValueChangedCallback(evt => edit(evt.newValue.ToString(CultureInfo.InvariantCulture)));
                    return integer;
                case BalanceValueKind.Boolean:
                    Toggle boolean = new Toggle(field.Label) { value = bool.Parse(value) };
                    boolean.RegisterValueChangedCallback(evt => edit(evt.newValue ? "true" : "false"));
                    return boolean;
                case BalanceValueKind.Enum:
                    if (field.Stat == "WeaponType")
                    {
                        var choices = usage.WeaponIds.Select(id => Enum.GetName(field.EnumType, id)).ToList();
                        DropdownField weapon = new DropdownField(field.Label, choices, -1);
                        weapon.SetValueWithoutNotify(Display(field, value));
                        weapon.RegisterValueChangedCallback(evt => edit(Convert.ToInt32(Enum.Parse(field.EnumType, evt.newValue)).ToString()));
                        return weapon;
                    }
                    EnumField enumeration = new EnumField(field.Label, (Enum)Enum.ToObject(field.EnumType, int.Parse(value)));
                    enumeration.RegisterValueChangedCallback(evt => edit(Convert.ToInt32(evt.newValue).ToString()));
                    return enumeration;
                default:
                    Foldout set = new Foldout { text = field.Label + " (existing canonical IDs)", value = false };
                    foreach (Enum id in Enum.GetValues(field.EnumType))
                    {
                        string token = Convert.ToInt32(id).ToString();
                        Toggle item = new Toggle(id.ToString()) { value = value.Split(',').Contains(token) };
                        item.RegisterValueChangedCallback(evt =>
                        {
                            var selected = value.Length == 0 ? new System.Collections.Generic.List<string>() : value.Split(',').ToList();
                            selected.Remove(token);
                            if (evt.newValue) selected.Add(token);
                            value = string.Join(",", selected);
                            edit(value);
                        });
                        set.Add(item);
                    }
                    return set;
            }
        }

        public static string Display(BalanceField field, string value) => field.Kind == BalanceValueKind.Enum
            ? Enum.GetName(field.EnumType, int.Parse(value)) : field.Kind == BalanceValueKind.EnumSet
                ? string.Join(", ", value.Length == 0 ? Array.Empty<string>() : value.Split(',').Select(id => Enum.GetName(field.EnumType, int.Parse(id)))) : value;

        public static void Details(VisualElement panel, BalanceField field, BalanceDraftUsage usage)
        {
            panel.Clear();
            panel.Add(new Label(field.Context + " / " + field.Label));
            panel.Add(new Label(field.Owner));
            panel.Add(new Label(field.DescribeScope(usage.Users(field))));
            if (field.AliasWarning.Length != 0) panel.Add(new Label(field.AliasWarning));
            ObjectField source = new ObjectField("Source asset / object") { value = field.Target, allowSceneObjects = false };
            source.SetEnabled(false);
            panel.Add(source);
            panel.Add(new Label(field.AssetPath));
            TextField identity = new TextField("Canonical identity") { value = field.Key, isReadOnly = true, multiline = true };
            panel.Add(identity);
            Foldout users = new Foldout { text = "Used by · " + usage.Users(field).Select(unit => unit.Id).Distinct().Count(), value = true };
            foreach (BalanceUnit unit in usage.Users(field).GroupBy(unit => unit.Id).Select(group => group.First()).OrderBy(unit => unit.Faction).ThenBy(unit => unit.Name))
                users.Add(new Label(unit.Caption));
            panel.Add(users);
            if (field.Dependency.Length != 0) panel.Add(new Label("Dependencies: current subtype, component and mount bindings are checked at Apply."));
            if (field.Group == "Abilities") panel.Add(new HelpBox("Tuning can leave numeric text in ability descriptions stale. Review the descriptions after Apply.", HelpBoxMessageType.Warning));
            if (field.Owner == "Prefab Override") panel.Add(new HelpBox("Writes create overrides in this owning unit prefab. Nested source assets and structure remain intact.", HelpBoxMessageType.Info));
            if (field.Stat == "MinYaw" || field.Stat == "MaxYaw") panel.Add(new HelpBox("Yaw is measured in degrees. Targeting compares angles in −180 … 180; wider stored limits are valid and preserved.", HelpBoxMessageType.Info));
        }
    }
}
