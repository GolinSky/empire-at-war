using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class SceneToolbarDropdown
{
    private const string TOOLBAR_ELEMENT_PATH = "EmpireAtWar/Scene Selector";
    private const string ADD_TO_TOOLBAR_MENU = "Tools/Scene Selector/Add to Main Toolbar";
    private const string SCENES_ROOT = "Assets/Scenes";
    private const string SELECT_SCENE_LABEL = "Select Scene";
    private const string TOOLTIP = "Open a scene from Assets/Scenes";

    static SceneToolbarDropdown()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
    }

    [MainToolbarElement(
        TOOLBAR_ELEMENT_PATH,
        defaultDockPosition = MainToolbarDockPosition.Middle,
        defaultDockIndex = 0)]
    private static MainToolbarDropdown CreateDropdown()
    {
        var activeSceneName = SceneManager.GetActiveScene().name;
        var label = string.IsNullOrEmpty(activeSceneName) ? SELECT_SCENE_LABEL : activeSceneName;

        return new MainToolbarDropdown(
            new MainToolbarContent(label, null, TOOLTIP),
            ShowSceneMenu);
    }

    [MenuItem(ADD_TO_TOOLBAR_MENU)]
    private static void AddToMainToolbar()
    {
        var showAllMethod = typeof(MainToolbar).GetMethod(
            "ShowAll",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(string) },
            null);

        if (showAllMethod == null)
        {
            throw new MissingMethodException(
                typeof(MainToolbar).FullName,
                "ShowAll");
        }

        showAllMethod.Invoke(null, new object[] { TOOLBAR_ELEMENT_PATH });
    }

    private static void ShowSceneMenu(Rect dropdownRect)
    {
        var menu = new GenericMenu();

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            menu.AddDisabledItem(new GUIContent("Exit Play Mode to switch scenes"));
            menu.DropDown(dropdownRect);
            return;
        }

        var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { SCENES_ROOT })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            .OrderBy(GetSceneLabel, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (scenePaths.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("No scenes found in Assets/Scenes"));
        }
        else
        {
            var activeScenePath = SceneManager.GetActiveScene().path;

            foreach (var scenePath in scenePaths)
            {
                menu.AddItem(
                    new GUIContent(GetSceneLabel(scenePath)),
                    string.Equals(scenePath, activeScenePath, StringComparison.OrdinalIgnoreCase),
                    OpenScene,
                    scenePath);
            }
        }

        menu.DropDown(dropdownRect);
    }

    private static string GetSceneLabel(string scenePath)
    {
        var relativePath = scenePath.Substring(SCENES_ROOT.Length + 1);
        return Path.ChangeExtension(relativePath, null).Replace("/", " > ");
    }

    private static void OpenScene(object scenePathValue)
    {
        var scenePath = scenePathValue as string;
        if (string.IsNullOrEmpty(scenePath))
        {
            throw new ArgumentException("A valid scene path is required.", nameof(scenePathValue));
        }

        if (string.Equals(
                scenePath,
                SceneManager.GetActiveScene().path,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }

    private static void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        MainToolbar.Refresh(TOOLBAR_ELEMENT_PATH);
    }
}
