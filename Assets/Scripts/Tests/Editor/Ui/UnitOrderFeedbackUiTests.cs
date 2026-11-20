using System.Reflection;
using EmpireAtWar.Entities.UnitOrderFeedback;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class UnitOrderFeedbackUiTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/UnitOrderFeedback/UnitOrderFeedbackUi.prefab";

        [Test]
        public void PlayTargetOrder_PreservesGuardAspectAndRestoresAttackReticle()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                UnitOrderFeedbackUi ui = instance.GetComponent<UnitOrderFeedbackUi>();
                using var bindings = new SerializedObject(ui);
                Image image = (Image)bindings.FindProperty("attackImage").objectReferenceValue;
                RectTransform anchor = (RectTransform)bindings.FindProperty("attackAnchor").objectReferenceValue;
                Vector3 attackScale = anchor.localScale;
                Sprite guardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/Textures/Ui/UnitActions/Guard.png");
                Sprite attackSprite = image.sprite;
                Assert.That(guardSprite, Is.Not.Null);
                Assert.That(attackSprite, Is.Not.Null.And.Not.EqualTo(guardSprite));
                ui.Initialize();

                ui.PlayTargetOrder(Vector2.zero, isGuard: true, () => { });
                Assert.That(anchor.gameObject.activeSelf, Is.True);
                Assert.That(image.overrideSprite, Is.SameAs(guardSprite));
                Assert.That(image.preserveAspect, Is.True);
                Assert.That(image.rectTransform.TransformVector(Vector3.right).magnitude,
                    Is.EqualTo(image.rectTransform.TransformVector(Vector3.up).magnitude).Within(0.0001f));

                ui.PlayTargetOrder(Vector2.zero, isGuard: false, () => { });
                Assert.That(image.overrideSprite, Is.SameAs(attackSprite));
                Assert.That(anchor.localScale, Is.EqualTo(attackScale));

                ui.StopTargetOrder();
                Assert.That(anchor.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PlayMovement_AppliesDistinctFormationAndLooseWaveColors()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            Assert.That(prefab, Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                UnitOrderFeedbackUi ui = instance.GetComponent<UnitOrderFeedbackUi>();
                Assert.That(ui, Is.Not.Null);
                ui.Initialize();

                FieldInfo waveField = typeof(UnitOrderFeedbackUi).GetField("movementWave",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo echoField = typeof(UnitOrderFeedbackUi).GetField("movementEcho",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo formationField = typeof(UnitOrderFeedbackUi).GetField("formationColor",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo looseField = typeof(UnitOrderFeedbackUi).GetField("looseColor",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Image wave = (Image)waveField.GetValue(ui);
                Image echo = (Image)echoField.GetValue(ui);
                Color formation = (Color)formationField.GetValue(ui);
                Color loose = (Color)looseField.GetValue(ui);

                Assert.That(loose.g, Is.GreaterThan(loose.b), "Loose move wave should be green.");
                Assert.That(formation.b, Is.GreaterThan(formation.g), "Formation move wave should be blue.");

                ui.PlayMovement(Vector2.zero, isLoose: false, () => { });
                Assert.That(wave.color.r, Is.EqualTo(formation.r).Within(0.01f));
                Assert.That(wave.color.g, Is.EqualTo(formation.g).Within(0.01f));
                Assert.That(wave.color.b, Is.EqualTo(formation.b).Within(0.01f));
                Assert.That(echo.color.b, Is.EqualTo(formation.b).Within(0.01f));

                ui.PlayMovement(Vector2.zero, isLoose: true, () => { });
                Assert.That(wave.color.r, Is.EqualTo(loose.r).Within(0.01f));
                Assert.That(wave.color.g, Is.EqualTo(loose.g).Within(0.01f));
                Assert.That(wave.color.b, Is.EqualTo(loose.b).Within(0.01f));
                Assert.That(echo.color.g, Is.EqualTo(loose.g).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
