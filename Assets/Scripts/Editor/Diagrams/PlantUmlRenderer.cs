using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace EmpireAtWar.Editor.Diagrams
{
    // Renders a .puml with the local pinned jar. Never uses the public PlantUML server.
    public static class PlantUmlRenderer
    {
        public const string JAR_PATH = "Tools/PlantUml/bin/plantuml.jar";
        private const string INSTALL_COMMAND = "pwsh Tools/PlantUml/Install-PlantUml.ps1";
        private const int TIMEOUT_MILLISECONDS = 60000;

        // PlantUML writes only the last -t format per call, so SVG and PNG need one call each.
        public static void Render(string pumlPath)
        {
            if (!File.Exists(JAR_PATH))
                throw new FileNotFoundException($"PlantUML is not installed. Run: {INSTALL_COMMAND}", JAR_PATH);
            if (!File.Exists(pumlPath))
                throw new FileNotFoundException($"Diagram source not found: {pumlPath}", pumlPath);

            Run(pumlPath, "-tsvg");
            Run(pumlPath, "-tpng");
        }

        private static void Run(string pumlPath, string format)
        {
            var startInfo = new ProcessStartInfo("java",
                $"-Djava.awt.headless=true -jar \"{Path.GetFullPath(JAR_PATH)}\" -charset UTF-8 {format} \"{Path.GetFullPath(pumlPath)}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process process;
            try
            {
                process = Process.Start(startInfo);
            }
            catch (Win32Exception exception)
            {
                throw new InvalidOperationException($"Java is not on PATH. Install Java 11+, then run: {INSTALL_COMMAND}", exception);
            }

            using (process)
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(TIMEOUT_MILLISECONDS))
                {
                    process.Kill();
                    throw new TimeoutException($"PlantUML {format} render of {pumlPath} exceeded {TIMEOUT_MILLISECONDS / 1000} s.");
                }
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(
                        $"PlantUML {format} failed for {pumlPath} (exit {process.ExitCode}): {error.Result}{output.Result}".Trim());
            }
        }
    }
}
