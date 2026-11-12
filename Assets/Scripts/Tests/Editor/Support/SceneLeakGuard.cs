using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

// Global namespace: an NUnit SetUpFixture only wraps tests in its own namespace, and the editor tests span several.
[SetUpFixture]
public sealed class SceneLeakGuard
{
    private HashSet<GameObject> _rootsBefore;

    [OneTimeSetUp]
    public void RecordRoots() => _rootsBefore = new HashSet<GameObject>(CollectSavedRoots());

    // Edit Mode tests build real scopes in the open scene; an unparented object a service forgets to destroy
    // there is written to disk on the next scene save (how MusicSource(Clone) ended up in MainMenuScene).
    [OneTimeTearDown]
    public void AssertNoLeakedRoots()
    {
        string[] leaked = CollectSavedRoots()
            .Where(root => !_rootsBefore.Contains(root))
            .Select(root => $"{root.scene.path}: {root.name}")
            .ToArray();
        Assert.That(leaked, Is.Empty, "Edit Mode tests left objects in loaded scenes.");
    }

    private static IEnumerable<GameObject> CollectSavedRoots()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
                if ((root.hideFlags & HideFlags.DontSaveInEditor) == 0) yield return root;
        }
    }
}
