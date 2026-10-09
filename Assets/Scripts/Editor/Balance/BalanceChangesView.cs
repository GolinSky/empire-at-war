using System;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceChangesView
    {
        public static void Build(VisualElement panel, BalanceRegistration registry, BalanceEditorController controller, Action apply, Action restore)
        {
            BalanceDraft draft = controller.State.Draft;
            var errors = BalanceValidation.Preflight(registry, draft);
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, draft);
            if (EditorApplication.isPlayingOrWillChangePlaymode) errors.Add("Apply is unavailable in Play Mode.");
            panel.Add(new Label(draft.Changes.Count + " staged changes · exact allowlisted asset diff"));
            foreach (string error in errors) panel.Add(new HelpBox(error, HelpBoxMessageType.Error));

            ScrollView scroll = new ScrollView { viewDataKey = "balance-changes-scroll" };
            scroll.style.flexGrow = 1;
            panel.Add(scroll);
            var assets = draft.Changes.GroupBy(change => registry.Fields.TryGetValue(change.Key, out BalanceField field) ? field.AssetPath : "Missing targets");
            foreach (var asset in assets)
            {
                Foldout group = new Foldout { text = asset.Key, value = true };
                scroll.Add(group);
                foreach (BalanceChange change in asset)
                {
                    if (registry.Fields.TryGetValue(change.Key, out BalanceField field)) group.Add(Row(field, change, draft, usage, controller));
                    else Missing(group, change, controller);
                }
            }

            Button write = new Button(apply) { text = "Apply reviewed changes to assets" };
            write.AddToClassList("balance-primary");
            write.SetEnabled(errors.Count == 0 && draft.Changes.Count > 0);
            panel.Add(write);
            Button previous = new Button(restore) { text = "Restore Previous Apply (separate from draft Undo)" };
            previous.SetEnabled(BalanceApplyService.CanRestore && !EditorApplication.isPlayingOrWillChangePlaymode && draft.Changes.Count == 0);
            panel.Add(previous);
        }

        private static void Missing(VisualElement group, BalanceChange change, BalanceEditorController controller)
        {
            group.Add(new Label(change.Key + " · " + change.Before + " → " + change.After));
            group.Add(new Button(() => controller.RemoveChange(change.Key)) { text = "Remove missing target from draft" });
        }

        private static VisualElement Row(BalanceField field, BalanceChange change, BalanceDraft draft, BalanceDraftUsage usage,
            BalanceEditorController controller)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("balance-card");
            row.Add(new Label(field.Context + " / " + field.Label + ": " + BalanceFieldView.Display(field, change.Before)
                + " → " + BalanceFieldView.Display(field, change.After)));
            row.Add(new Label(field.OwnerLabel + " · " + field.DescribeScope(usage.Users(field))));
            if (field.InheritsSource && draft.Changes.Any(entry => entry.Key == field.SourceKey && entry.After == change.After))
                row.Add(new Label("Linked effective value: the shared source write covers this cell; no owning override is created."));
            if (field.AliasWarning.Length != 0) row.Add(new Label(field.AliasWarning));

            Foldout impact = new Foldout { text = "Used by (includes filtered units)" };
            foreach (BalanceUnit unit in usage.Users(field)) impact.Add(new Label(unit.Caption));
            row.Add(impact);
            if (field.Owner == BalanceFieldOwner.PrefabOverride || field.SharedMountSource)
                row.Add(new Label("Derived update: affected unit weapon loadouts; squadron member counts when applicable."));
            if (field.Group == BalanceFieldGroup.Abilities) row.Add(new Label("Review affected ability descriptions for stale numeric text."));

            string current = field.Read();
            if (current == change.Before && field.Schema == change.Schema) return row;
            row.Add(new Label("Current external value: " + BalanceFieldView.Display(field, current)));
            if (field.Schema == change.Schema)
                row.Add(new Button(() => controller.KeepChange(field)) { text = "Keep draft over this current value" });
            row.Add(new Button(() => controller.RemoveChange(change.Key)) { text = "Accept current / remove draft edit" });
            return row;
        }
    }
}
