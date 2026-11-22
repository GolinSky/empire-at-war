using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EmpireAtWar.Editor.Diagrams
{
    // Expands folders and files into a sorted, distinct list of .cs paths with forward slashes.
    public static class DiagramSourceCollector
    {
        private const string SCRIPT_EXTENSION = ".cs";

        public static IReadOnlyList<string> Collect(IEnumerable<string> paths)
        {
            var scripts = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in paths)
            {
                string trimmed = path.Trim().TrimEnd('/', '\\');
                if (trimmed.Length == 0) continue;

                if (Directory.Exists(trimmed))
                {
                    foreach (string file in Directory.GetFiles(trimmed, "*" + SCRIPT_EXTENSION, SearchOption.AllDirectories))
                        scripts.Add(Normalize(file));
                }
                else if (File.Exists(trimmed))
                {
                    if (!trimmed.EndsWith(SCRIPT_EXTENSION, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException($"Not a C# script: {trimmed}");
                    scripts.Add(Normalize(trimmed));
                }
                else
                    throw new FileNotFoundException($"Diagram source path not found: {trimmed}", trimmed);
            }

            if (scripts.Count == 0)
                throw new ArgumentException("No .cs files found in the given paths.");
            return scripts.ToArray();
        }

        public static IReadOnlyList<string> Split(string paths) =>
            paths.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        private static string Normalize(string path) => path.Replace('\\', '/');
    }
}
