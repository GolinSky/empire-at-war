using System;
using System.IO;
using Unity.CodeEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Diagrams
{
    public static class DiagramOpener
    {
        public static DiagramOpenTarget Parse(string target)
        {
            if (!Enum.TryParse(target, true, out DiagramOpenTarget parsed) || !Enum.IsDefined(typeof(DiagramOpenTarget), parsed))
                throw new ArgumentException($"Unknown open target '{target}'. Use none, rider, editor or browser.", nameof(target));
            return parsed;
        }

        public static void Open(string name, DiagramOpenTarget target)
        {
            switch (target)
            {
                case DiagramOpenTarget.None:
                    return;
                case DiagramOpenTarget.Rider:
                    OpenInCodeEditor(DiagramOutputPaths.Puml(name));
                    return;
                case DiagramOpenTarget.Editor:
                    DiagramPreviewWindow.ShowDiagram(name);
                    return;
                case DiagramOpenTarget.Browser:
                    OpenInBrowser(DiagramOutputPaths.Svg(name));
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        // Rider's Unity integration only opens script extensions through OpenProject, so launch the
        // configured external editor with the .puml path; the plantuml4idea plugin previews it.
        public static void OpenInCodeEditor(string path)
        {
            string fullPath = RequireFile(path);
            string editor = CodeEditor.CurrentEditorInstallation;
            if (string.IsNullOrEmpty(editor))
                throw new InvalidOperationException("No external script editor is set in Preferences > External Tools.");
            if (!CodeEditor.OSOpenFile(editor, $"\"{fullPath}\""))
                throw new InvalidOperationException($"Could not open {fullPath} with {editor}.");
        }

        public static void OpenInBrowser(string path) =>
            Application.OpenURL("file:///" + RequireFile(path).Replace('\\', '/'));

        private static string RequireFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Diagram file not found: {path}", path);
            return Path.GetFullPath(path);
        }
    }
}
