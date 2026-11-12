using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using VContainer.Unity;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Publishes the project's VContainer settings as after a Play Mode session, when a parentless EditMode scope
    /// would fall back to the production root and fail on DontDestroyOnLoad. Restores the previous settings on dispose.
    /// </summary>
    public sealed class PopulatedVContainerSettings : IDisposable
    {
        private const string SETTINGS_PATH = "Assets/Settings/VContainerSettings.asset";
        private static readonly MethodInfo SET_INSTANCE = typeof(VContainerSettings)
            .GetProperty(nameof(VContainerSettings.Instance)).GetSetMethod(true);
        private static readonly FieldInfo ROOT_INSTANCE = typeof(VContainerSettings)
            .GetField("rootLifetimeScopeInstance", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly VContainerSettings _previous;
        private readonly VContainerSettings _settings;
        private readonly object _rootInstance;
        private readonly bool _rootPrefabActive;

        public PopulatedVContainerSettings()
        {
            _previous = VContainerSettings.Instance;
            _settings = AssetDatabase.LoadAssetAtPath<VContainerSettings>(SETTINGS_PATH);
            Assert.That(_settings.RootLifetimeScope, Is.Not.Null, "The production root must be configured.");
            _rootInstance = ROOT_INSTANCE.GetValue(null);
            _rootPrefabActive = _settings.RootLifetimeScope.gameObject.activeSelf;
            SET_INSTANCE.Invoke(null, new object[] { _settings });
        }

        public void AssertProductionRootUntouched()
        {
            Assert.That(VContainerSettings.Instance, Is.SameAs(_settings));
            Assert.That(ReferenceEquals(ROOT_INSTANCE.GetValue(null), _rootInstance), Is.True,
                "A fixture scope instantiated the production root.");
            Assert.That(_settings.RootLifetimeScope.gameObject.activeSelf, Is.EqualTo(_rootPrefabActive),
                "The saved root prefab's activation changed.");
        }

        public void Dispose() => SET_INSTANCE.Invoke(null, new object[] { _previous });
    }
}
