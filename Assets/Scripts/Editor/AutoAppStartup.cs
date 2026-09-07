using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EmpireAtWar.Editor.Tools
{
    /// <summary>
    /// Unity Editor extension that automatically launches configured external applications (e.g., Obsidian)
    /// on Unity Editor startup if they are not already running.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoAppStartup
    {
        private const string PREF_ENABLED_KEY = "AutoAppStartup_Enabled";
        private const string PREF_PATHS_KEY = "AutoAppStartup_Paths";
        private const string SESSION_CHECKED_KEY = "AutoAppStartup_AlreadyChecked_Session";
        private const string DEFAULT_OBSIDIAN_PATH = @"%LOCALAPPDATA%\Programs\Obsidian\Obsidian.exe";

        static AutoAppStartup()
        {
            // Execute on Editor launch / assembly reload, but avoid repeated launches in the same session.
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (SessionState.GetBool(SESSION_CHECKED_KEY, false))
            {
                return;
            }

            SessionState.SetBool(SESSION_CHECKED_KEY, true);

            if (!IsAutoStartupEnabled())
            {
                return;
            }

            LaunchAllApps(isManualTrigger: false);
        }

        public static bool IsAutoStartupEnabled()
        {
            return EditorPrefs.GetBool(PREF_ENABLED_KEY, true);
        }

        public static void SetAutoStartupEnabled(bool enabled)
        {
            EditorPrefs.SetBool(PREF_ENABLED_KEY, enabled);
        }

        public static List<string> GetConfiguredAppPaths()
        {
            string rawPaths = EditorPrefs.GetString(PREF_PATHS_KEY, DEFAULT_OBSIDIAN_PATH);
            if (string.IsNullOrWhiteSpace(rawPaths))
            {
                return new List<string> { DEFAULT_OBSIDIAN_PATH };
            }

            return rawPaths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Distinct()
                .ToList();
        }

        public static void SaveConfiguredAppPaths(List<string> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            string combined = string.Join(";", paths.Where(p => !string.IsNullOrWhiteSpace(p)));
            EditorPrefs.SetString(PREF_PATHS_KEY, combined);
        }

        public static void LaunchAllApps(bool isManualTrigger)
        {
            List<string> configuredPaths = GetConfiguredAppPaths();
            foreach (string rawPath in configuredPaths)
            {
                LaunchAppIfNecessary(rawPath, isManualTrigger);
            }
        }

        public static bool LaunchAppIfNecessary(string rawPath, bool isManualTrigger)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return false;
            }

            string expandedPath = Environment.ExpandEnvironmentVariables(rawPath);
            if (!File.Exists(expandedPath))
            {
                if (isManualTrigger)
                {
                    Debug.LogWarning($"[AutoAppStartup] File not found at configured path: {expandedPath}");
                }
                return false;
            }

            string processName = Path.GetFileNameWithoutExtension(expandedPath);
            Process[] runningProcesses = Process.GetProcessesByName(processName);

            if (runningProcesses.Length > 0)
            {
                if (isManualTrigger)
                {
                    Debug.Log($"[AutoAppStartup] '{processName}' is already running ({runningProcesses.Length} instance(s)).");
                }
                return true;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = expandedPath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(expandedPath) ?? string.Empty
                };

                Process.Start(startInfo);
                Debug.Log($"[AutoAppStartup] Successfully launched '{processName}' from '{expandedPath}'.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoAppStartup] Failed to start process at '{expandedPath}': {ex.Message}");
                return false;
            }
        }

        [MenuItem("Tools/Auto App Startup/Launch Required Applications Now", false, 10)]
        private static void MenuItemLaunchNow()
        {
            LaunchAllApps(isManualTrigger: true);
        }

        [MenuItem("Tools/Auto App Startup/Configure...", false, 11)]
        private static void MenuItemOpenConfig()
        {
            AutoAppStartupWindow.ShowWindow();
        }
    }

    /// <summary>
    /// Editor Window allowing users to configure external application paths and auto-startup settings.
    /// </summary>
    public class AutoAppStartupWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "Auto App Startup";
        private List<string> _appPaths = new List<string>();
        private bool _isEnabled;
        private Vector2 _scrollPosition;

        public static void ShowWindow()
        {
            var window = GetWindow<AutoAppStartupWindow>(false, WINDOW_TITLE, true);
            window.minSize = new Vector2(450, 300);
            window.Show();
        }

        private void OnEnable()
        {
            _isEnabled = AutoAppStartup.IsAutoStartupEnabled();
            _appPaths = AutoAppStartup.GetConfiguredAppPaths();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("External Applications Auto-Startup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure external Windows applications (e.g., Obsidian) to automatically launch when Unity Editor starts if they are not already running.",
                MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            _isEnabled = EditorGUILayout.Toggle("Enable Auto-Startup", _isEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                AutoAppStartup.SetAutoStartupEnabled(_isEnabled);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Configured Application Exe Paths:", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
            for (int i = 0; i < _appPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _appPaths[i] = EditorGUILayout.TextField(_appPaths[i]);

                if (GUILayout.Button("Browse", GUILayout.Width(65)))
                {
                    string selected = EditorUtility.OpenFilePanel("Select Application Executable", "", "exe");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        _appPaths[i] = selected;
                        GUI.changed = true;
                    }
                }

                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    AutoAppStartup.LaunchAppIfNecessary(_appPaths[i], isManualTrigger: true);
                }

                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    _appPaths.RemoveAt(i);
                    GUI.changed = true;
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("+ Add Application Path", GUILayout.Height(25)))
            {
                _appPaths.Add(@"C:\Program Files\App\app.exe");
                GUI.changed = true;
            }

            if (GUI.changed)
            {
                AutoAppStartup.SaveConfiguredAppPaths(_appPaths);
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Launch All Apps Now", GUILayout.Height(30)))
            {
                AutoAppStartup.LaunchAllApps(isManualTrigger: true);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
