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
