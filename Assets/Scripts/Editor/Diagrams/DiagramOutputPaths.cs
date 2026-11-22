using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EmpireAtWar.Editor.Diagrams
{
    // Diagrams live outside Assets: never imported, never tracked, never in a build.
    public static class DiagramOutputPaths
    {
        public const string ROOT = "Logs/Diagrams";

        private static readonly Regex NAME_PATTERN = new Regex("^[A-Za-z0-9_-]+$");

        public static string Folder(string name)
        {
            if (string.IsNullOrEmpty(name) || !NAME_PATTERN.IsMatch(name))
                throw new ArgumentException($"Diagram name '{name}' must use only letters, digits, '_' or '-'.", nameof(name));
            return ROOT + "/" + name;
        }

        public static string Puml(string name) => FilePath(name, ".puml");
        public static string Svg(string name) => FilePath(name, ".svg");
        public static string Png(string name) => FilePath(name, ".png");

        public static IReadOnlyList<string> ListNames()
        {
            if (!Directory.Exists(ROOT)) return Array.Empty<string>();
            return Directory.GetDirectories(ROOT)
                .Select(Path.GetFileName)
                .Where(name => NAME_PATTERN.IsMatch(name) && File.Exists(Puml(name)))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string FilePath(string name, string extension) => Folder(name) + "/" + name + extension;
    }
}
