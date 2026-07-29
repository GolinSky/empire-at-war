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
                "TouchMap/CameraMove",
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
            InputAction action = _inputActions.FindAction(
                "TouchMap/Zoom",
                true);
            InputAction scroll = _inputActions.FindAction(
                "TouchMap/Scroll",
                true);

            AssertBindings(
                action,
                "<Keyboard>/r",
                "<Keyboard>/f");
            AssertBindings(scroll, "<Mouse>/scroll/y");
        }

        [Test]
        public void CameraDrag_UsesMiddleMouseWithoutRotationActions()
        {
            AssertBindings(
                _inputActions.FindAction("TouchMap/CameraDrag", true),
                "<Mouse>/middleButton");
            Assert.That(
                _inputActions.FindAction("TouchMap/CameraRotate", false),
                Is.Null);
            Assert.That(
                _inputActions.FindAction("TouchMap/CameraReset", false),
                Is.Null);
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
