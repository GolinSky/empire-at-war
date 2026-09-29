using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Tests.Camera
{
    public sealed class CameraInputBindingsTests
    {
        private const string INPUT_ACTIONS_PATH =
            "Assets/Settings/Input/EmpireAtWar.inputactions";

        private InputActionAsset _inputActions;

        [SetUp]
        public void SetUp()
        {
            _inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                INPUT_ACTIONS_PATH);
            Assert.That(_inputActions, Is.Not.Null);
        }

        [Test]
        public void CameraMove_UsesArrowKeysWasdAndQe()
        {
            InputAction action = _inputActions.FindAction(
                "Camera/Move",
                true);

            AssertBindings(
                action,
                "<Keyboard>/upArrow",
                "<Keyboard>/downArrow",
                "<Keyboard>/leftArrow",
                "<Keyboard>/rightArrow",
                "<Keyboard>/w",
                "<Keyboard>/s",
                "<Keyboard>/a",
                "<Keyboard>/d",
                "<Keyboard>/q",
                "<Keyboard>/e");
            Assert.That(
                action.bindings.Any(binding =>
                    binding.effectivePath == "<Keyboard>/digit2" ||
                    binding.effectivePath == "<Keyboard>/digit4" ||
                    binding.effectivePath == "<Keyboard>/digit5" ||
                    binding.effectivePath == "<Keyboard>/digit6" ||
                    binding.effectivePath == "<Keyboard>/digit8"),
                Is.False);
        }

        [Test]
        public void Zoom_UsesMouseWheelAndLetterKeys()
        {
            AssertBindings(
                _inputActions.FindAction("Camera/Zoom", true),
                "<Keyboard>/r",
                "<Keyboard>/f");
            AssertBindings(
                _inputActions.FindAction("Camera/ZoomScroll", true),
                "<Mouse>/scroll/y");
        }

        [Test]
        public void CameraDrag_UsesMiddleMouseWithoutRotationActions()
        {
            AssertBindings(
                _inputActions.FindAction("Camera/DragPan", true),
                "<Mouse>/middleButton");
            Assert.That(
                _inputActions.FindAction("Camera/Rotate", false),
                Is.Null);
            Assert.That(
                _inputActions.FindAction("Camera/Reset", false),
                Is.Null);
        }

        [Test]
        public void BattleShortcuts_UseModifierComposites()
        {
            AssertBindings(
                _inputActions.FindAction("Battle/SelectVisible", true),
                "<Keyboard>/ctrl",
                "<Keyboard>/a");
            AssertBindings(
                _inputActions.FindAction("Battle/SelectAll", true),
                "<Keyboard>/ctrl",
                "<Keyboard>/shift",
                "<Keyboard>/a");
            AssertBindings(
                _inputActions.FindAction("Battle/QueueWaypoint", true),
                "<Keyboard>/alt");
            AssertBindings(
                _inputActions.FindAction("Ui/Cancel", true),
                "<Keyboard>/escape");
        }

        [Test]
        public void Asset_HasOnlyKeyboardAndMouseScheme()
        {
            Assert.That(
                _inputActions.controlSchemes.Select(scheme => scheme.name),
                Is.EquivalentTo(new[] { "Keyboard&Mouse" }));
        }

        private static void AssertBindings(
            InputAction action,
            params string[] expectedPaths)
        {
            string[] paths = action.bindings
                .Where(binding => !binding.isComposite)
                .Select(binding => binding.effectivePath)
                .ToArray();

            foreach (string expectedPath in expectedPaths)
            {
                Assert.That(paths, Does.Contain(expectedPath));
            }
        }
    }
}
