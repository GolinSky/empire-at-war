using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.VContainer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
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
            Assert.That(settings.HoverColor, Is.EqualTo(Color.white));
            Assert.That(settings.SelectedColor, Is.EqualTo(new Color(1f, 0.5f, 0f, 1f)));
            Assert.That(settings.AttackColor, Is.EqualTo(Color.red));
            Assert.That(settings.OutlineWidth, Is.EqualTo(2f));
            Assert.That(settings.AttackDuration, Is.EqualTo(2f));
        }

        [Test]
        public void InitializeUploadsHighlightGlobals()
        {
            var settings = ScriptableObject.CreateInstance<UnitHighlightSettings>();
            var previousHover = Shader.GetGlobalVector("_UnitOutlineHoverColor");
            var previousSelected = Shader.GetGlobalVector("_UnitOutlineSelectedColor");
            var previousAttack = Shader.GetGlobalVector("_UnitOutlineAttackColor");
            float previousWidth = Shader.GetGlobalFloat("_UnitOutlineWidth");
            var entities = new EntityLocator();
            var service = new UnitHighlightService(settings,
                new UnitOrderService(entities, null, null),
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), entities);
            try
            {
                service.Initialize();

                Assert.That(Shader.GetGlobalVector("_UnitOutlineHoverColor"),
                    Is.EqualTo((Vector4)settings.HoverColor.linear));
                Assert.That(Shader.GetGlobalVector("_UnitOutlineSelectedColor"),
                    Is.EqualTo((Vector4)settings.SelectedColor.linear));
                Assert.That(Shader.GetGlobalVector("_UnitOutlineAttackColor"),
                    Is.EqualTo((Vector4)settings.AttackColor.linear));
                Assert.That(Shader.GetGlobalFloat("_UnitOutlineWidth"), Is.EqualTo(settings.OutlineWidth));
            }
            finally
            {
                service.LateDispose();
                Shader.SetGlobalVector("_UnitOutlineHoverColor", previousHover);
                Shader.SetGlobalVector("_UnitOutlineSelectedColor", previousSelected);
                Shader.SetGlobalVector("_UnitOutlineAttackColor", previousAttack);
                Shader.SetGlobalFloat("_UnitOutlineWidth", previousWidth);
                Object.DestroyImmediate(settings);
            }
        }
    }
}
