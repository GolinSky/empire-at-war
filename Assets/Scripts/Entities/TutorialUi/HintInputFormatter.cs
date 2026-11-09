using System.Collections.Generic;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Tutorial;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Entities.TutorialUi
{
    public static class HintInputFormatter
    {
        public static List<HintInputPrompt> CreatePrompts(HintDefinition hint, InputActionAsset actions)
        {
            List<HintInputPrompt> prompts = new List<HintInputPrompt>();
            foreach (string actionPath in hint.InputActions)
                AddAction(prompts, actions.FindAction(actionPath, throwIfNotFound: true));
            foreach (string controlPath in hint.InputControls)
                AddControl(prompts, controlPath, prompts.Count == 0 ? string.Empty : "or");
            return prompts;
        }

        public static string FormatBody(HintDefinition hint, InputActionAsset actions)
        {
            if (hint.InputActions.Count == 0) return hint.Body;
            object[] labels = new object[hint.InputActions.Count];
            for (int i = 0; i < labels.Length; i++)
            {
                List<HintInputPrompt> parts = new List<HintInputPrompt>();
                AddAction(parts, actions.FindAction(hint.InputActions[i], throwIfNotFound: true));
                string label = string.Empty;
                foreach (HintInputPrompt part in parts)
                    label += part.SeparatorBefore + part.Label;
                labels[i] = $"<b>{label}</b>";
            }
            return string.Format(hint.Body, labels);
        }

        // Show the primary binding; alternative bindings remain available in Settings.
        private static void AddAction(List<HintInputPrompt> prompts, InputAction action)
        {
            string separator = prompts.Count == 0 ? string.Empty : "or";
            if (action.bindings.Count == 0)
            {
                AddControl(prompts, string.Empty, separator);
                return;
            }
            InputBinding first = action.bindings[0];
            if (!first.isComposite)
            {
                AddControl(prompts, first.effectivePath, separator);
                return;
            }
            string composite = first.GetNameOfComposite();
            string partSeparator = composite == "OneModifier" || composite == "TwoModifiers" ? "+" : "/";
            for (int i = 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
            {
                AddControl(prompts, action.bindings[i].effectivePath, i == 1 ? separator : partSeparator);
            }
        }

        private static void AddControl(List<HintInputPrompt> prompts, string path, string separator)
        {
            string label = string.IsNullOrEmpty(path) ? "Unbound" : BindingPathNames.Readable(path);
            prompts.Add(new HintInputPrompt(string.IsNullOrEmpty(path) ? string.Empty : path, label, separator));
        }
    }
}
