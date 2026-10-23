using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Entities.Loading;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class LoadingUiIsolationTests
    {
        [Test]
        public void LoadingSceneBindsAVisibleSpinnerArc()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Loading.unity");
            try
            {
                var ui = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LoadingUi>(true)).Single();
                var serializedUi = new SerializedObject(ui);
                var iconProperty = serializedUi.FindProperty("loadingIcon");
                Assert.That(iconProperty, Is.Not.Null);
                var icon = iconProperty.objectReferenceValue as RectTransform;
                Assert.That(icon, Is.Not.Null);
                var image = icon.GetComponent<Image>();
                Assert.That(image.enabled, Is.True);
                Assert.That(image.type, Is.EqualTo(Image.Type.Filled));
                Assert.That(image.fillMethod, Is.EqualTo(Image.FillMethod.Radial360));
                Assert.That(image.fillAmount, Is.GreaterThan(0f).And.LessThan(1f));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [UnityTest]
        public IEnumerator LoadingCircleRotatesWhileGameTimeIsPaused()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("LoadingUiTest", typeof(CanvasGroup));
            var icon = new GameObject("LoadingIcon", typeof(RectTransform));
            icon.transform.SetParent(root.transform, false);
            float previousTimeScale = Time.timeScale;
            try
            {
                var ui = root.AddComponent<LoadingUi>();
                var iconField = typeof(LoadingUi).GetField("loadingIcon", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconField, Is.Not.Null);
                iconField.SetValue(ui, (RectTransform)icon.transform);
                Time.timeScale = 0f;
                Quaternion initialRotation = icon.transform.localRotation;
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(Quaternion.Angle(initialRotation, icon.transform.localRotation), Is.GreaterThan(0.01f));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [UnityTearDown]
        public IEnumerator ExitRuntimeAfterAnimationTest()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [Test]
        public void ScenePlacedControllerHasOnlyItsOwnViewDependency()
        {
            ParameterInfo[] parameters = typeof(LoadingUiController).GetConstructors()[0].GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(1));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(LoadingUi)));
            Assert.That(typeof(LoadingUi).IsSubclassOf(typeof(BaseUi)), Is.True);
        }

        [Test]
        public void ActivationRunsOnlyWhenDestinationSceneIsReady()
        {
            var sceneService = new SceneServiceStub();
            var flow = new SceneActivationFlow(sceneService);
            flow.Tick();
            Assert.That(sceneService.ActivationCount, Is.Zero);
            sceneService.IsSceneLoaded = true;
            flow.Tick();
            Assert.That(sceneService.ActivationCount, Is.EqualTo(1));
        }

        private sealed class SceneServiceStub : ISceneService
        {
            public string Id => nameof(SceneServiceStub);
            public event Action<SceneType> OnSceneActivation { add { } remove { } }
            public SceneType TargetScene => SceneType.Loading;
            public bool IsSceneLoaded { get; set; }
            public int ActivationCount { get; private set; }
            public void LoadScene(SceneType sceneType) => throw new NotSupportedException();
            public void ActivateScene() => ActivationCount++;
        }
    }
}
