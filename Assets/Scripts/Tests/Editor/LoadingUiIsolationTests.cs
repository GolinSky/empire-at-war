using System;
using System.Reflection;
using EmpireAtWar.Entities.Loading;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class LoadingUiIsolationTests
    {
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
