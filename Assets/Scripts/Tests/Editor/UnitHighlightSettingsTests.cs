using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.VContainer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitHighlightSettingsTests
    {
        [Test]
        public void BattleScopeUsesPersistedHighlightSettings()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab");
            var scope = prefab.GetComponent<SkirmishScope>();
            var settings = new SerializedObject(scope).FindProperty("unitHighlightSettings")
                .objectReferenceValue as UnitHighlightSettings;

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.Color, Is.EqualTo(Color.white));
            Assert.That(settings.HoverStrength, Is.EqualTo(0.35f));
            Assert.That(settings.SelectedStrength, Is.EqualTo(0.25f));
        }

        [Test]
        public void InitializeUploadsHighlightGlobals()
        {
            var settings = ScriptableObject.CreateInstance<UnitHighlightSettings>();
            var previousColor = Shader.GetGlobalVector("_UnitHighlightColor");
            var previousStrength = Shader.GetGlobalVector("_UnitHighlightStrength");
            try
            {
                new UnitHighlightService(settings).Initialize();

                Assert.That(Shader.GetGlobalVector("_UnitHighlightColor"),
                    Is.EqualTo((Vector4)settings.Color.linear));
                Assert.That(Shader.GetGlobalVector("_UnitHighlightStrength"),
                    Is.EqualTo(new Vector4(0.35f, 0.25f, 0f, 0f)));
            }
            finally
            {
                Shader.SetGlobalVector("_UnitHighlightColor", previousColor);
                Shader.SetGlobalVector("_UnitHighlightStrength", previousStrength);
                Object.DestroyImmediate(settings);
            }
        }
    }
}
