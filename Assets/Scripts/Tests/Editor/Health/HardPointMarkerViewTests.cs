using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class HardPointMarkerViewTests
    {
        private GameObject _root;
        private HardPointMarkerView _marker;
        private Image _icon;
        private HardPointTargetRing _targetRing;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("MarkerTest", typeof(RectTransform));
            _marker = HardPointMarkerView.Create((RectTransform)_root.transform);
            _icon = _marker.transform.GetChild(0).GetComponent<Image>();
            _targetRing = _marker.transform.GetChild(1).GetComponent<HardPointTargetRing>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Create_ContainsHealthIconAndNonBlockingHiddenTargetRing()
        {
            Assert.That(_marker.transform.childCount, Is.EqualTo(2));
            Assert.That(_icon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(25f, 25f)));
            Assert.That(_icon.raycastTarget, Is.False);

            Outline outline = _icon.GetComponent<Outline>();
            Assert.That(outline, Is.Not.Null);
            Assert.That(outline.effectColor, Is.EqualTo(new Color(0.03f, 0.04f, 0.06f, 0.9f)));
            Assert.That(outline.effectDistance, Is.EqualTo(new Vector2(1f, -1f)));
            Assert.That(outline.useGraphicAlpha, Is.True);
            Assert.That(_targetRing.raycastTarget, Is.False);
            Assert.That(_targetRing.GetComponent<CanvasRenderer>(), Is.Not.Null,
                "A valid ring mesh cannot render without its CanvasRenderer.");
            Assert.That(_targetRing.gameObject.activeSelf, Is.False);
            Assert.That((Color32)_targetRing.color, Is.EqualTo(new Color32(210, 169, 94, 255)));
        }

        [Test]
        public void Show_TargetRingTracksTargetingWithoutChangingHealthColor()
        {
            _marker.Show(Vector2.zero,
                new HardPointMarkerData(null, Vector2.zero, 0.5f, true, false, false, 1f));
            Color healthColor = _icon.color;
            Assert.That(_targetRing.gameObject.activeSelf, Is.False);

            _marker.Show(Vector2.zero,
                new HardPointMarkerData(null, Vector2.zero, 0.5f, true, true, false, 1f));
            Assert.That(_targetRing.gameObject.activeInHierarchy, Is.True);
            Assert.That(_icon.color, Is.EqualTo(healthColor));

            _marker.Show(Vector2.zero,
                new HardPointMarkerData(null, Vector2.zero, 0.5f, false, true, false, 1f));
            Assert.That(_targetRing.gameObject.activeInHierarchy, Is.True);
            Assert.That(_icon.color, Is.EqualTo(healthColor));

            _marker.Show(Vector2.zero,
                new HardPointMarkerData(null, Vector2.zero, 0.5f, false, true, true, 1f));
            Assert.That(_targetRing.gameObject.activeInHierarchy, Is.False);

            _marker.Show(Vector2.zero,
                new HardPointMarkerData(null, Vector2.zero, 0.5f, false, false, false, 1f));
            Assert.That(_marker.gameObject.activeSelf, Is.True);
            Assert.That(_targetRing.gameObject.activeSelf, Is.False);
            Assert.That(_icon.color, Is.EqualTo(healthColor));
        }

        [Test]
        public void TargetRing_HasHollowCenterAndContrastingGoldAndDarkBands()
        {
            using VertexHelper vertices = new();
            typeof(HardPointTargetRing).GetMethod("OnPopulateMesh",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.DeclaredOnly)
                .Invoke(_targetRing, new object[] { vertices });

            Assert.That(vertices.currentIndexCount, Is.GreaterThan(0));
            bool hasGold = false;
            bool hasDarkBorder = false;
            UIVertex vertex = default;
            for (int index = 0; index < vertices.currentVertCount; index++)
            {
                vertices.PopulateUIVertex(ref vertex, index);
                Assert.That(vertex.position.magnitude, Is.InRange(17.99f, 22.01f));
                hasGold |= vertex.color.Equals((Color32)_targetRing.color);
                hasDarkBorder |= vertex.color.r < 20 && vertex.color.g < 20 && vertex.color.b < 20;
            }
            Assert.That(hasGold, Is.True);
            Assert.That(hasDarkBorder, Is.True);
        }

        [TestCase(1f, 0.3375f, 0.75f)]
        [TestCase(0.5f, 0.75f, 0.75f)]
        [TestCase(0f, 0.75f, 0.3375f)]
        [TestCase(2f, 0.3375f, 0.75f)]
        [TestCase(-1f, 0.75f, 0.3375f)]
        public void Show_TintsTheIconWithMutedGreenYellowRed(float health, float red, float green)
        {
            _marker.Show(Vector2.zero,
                new HardPointMarkerData(null, Vector2.zero, health, false, false, false, 1f));

            Assert.That(_icon.color.r, Is.EqualTo(red).Within(0.0001f));
            Assert.That(_icon.color.g, Is.EqualTo(green).Within(0.0001f));
            Assert.That(_icon.color.b, Is.EqualTo(0.3375f).Within(0.0001f));
            Assert.That(_icon.color.a, Is.EqualTo(1f));
        }

        [Test]
        public void Show_DestroyedHardPointHidesMarker_AndLiveReuseShowsItAgain()
        {
            _marker.Show(Vector2.zero,
                new HardPointMarkerData(null, Vector2.zero, 0f, true, true, true, 1f));
            Assert.That(_marker.gameObject.activeSelf, Is.False);

            _marker.Show(new Vector2(10f, 20f),
                new HardPointMarkerData(null, Vector2.zero, 1f, false, false, false, 1f));
            Assert.That(_marker.gameObject.activeSelf, Is.True);
            Assert.That(_icon.color.g, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(((RectTransform)_marker.transform).anchoredPosition,
                Is.EqualTo(new Vector2(10f, 20f)));
        }

        [Test]
        public void ShowAndHide_FadeAlphaInAndOut_AndDeactivateOnlyWhenFullyFaded()
        {
            CanvasGroup canvasGroup = _marker.GetComponent<CanvasGroup>();
            Assert.That(canvasGroup.blocksRaycasts, Is.False);

            _marker.Show(Vector2.zero, new HardPointMarkerData(null, Vector2.zero, 1f, false, false, false, 1f));
            Assert.That(canvasGroup.alpha, Is.Zero);
            _marker.Fade(0.01f);
            Assert.That(canvasGroup.alpha, Is.InRange(0.01f, 0.99f));
            _marker.Fade(10f);
            Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

            _marker.Hide();
            Assert.That(_marker.gameObject.activeSelf, Is.True, "Hide must fade out, not vanish.");
            _marker.Fade(0.01f);
            Assert.That(canvasGroup.alpha, Is.InRange(0.01f, 0.99f));
            Assert.That(_marker.gameObject.activeSelf, Is.True);
            _marker.Fade(10f);
            Assert.That(canvasGroup.alpha, Is.Zero);
            Assert.That(_marker.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void FadeOut_MovesMarkerWhileFading_AndReportsWhenFullyHidden()
        {
            _marker.Show(Vector2.zero, new HardPointMarkerData(null, Vector2.zero, 1f, false, false, false, 1f));
            _marker.Fade(10f);

            Assert.That(_marker.FadeOut(new Vector2(40f, -15f)), Is.True);
            Assert.That(((RectTransform)_marker.transform).anchoredPosition, Is.EqualTo(new Vector2(40f, -15f)));

            _marker.Fade(10f);
            Assert.That(_marker.FadeOut(new Vector2(60f, 5f)), Is.False);
            Assert.That(_marker.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void IconGradient_IsBlackAtBottomAndHealthTintAtTop_PreservingAlpha()
        {
            _icon.color = new Color(0.8f, 0.6f, 0.4f, 0.35f);
            using VertexHelper vertices = new();
            typeof(HardPointIconImage).GetMethod("OnPopulateMesh",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.DeclaredOnly)
                .Invoke(_icon, new object[] { vertices });

            Assert.That(vertices.currentVertCount, Is.EqualTo(4));
            UIVertex vertex = default;
            for (int index = 0; index < vertices.currentVertCount; index++)
            {
                vertices.PopulateUIVertex(ref vertex, index);
                Color tint = vertex.color;
                Assert.That(tint.a, Is.EqualTo(0.35f).Within(1f / 255f));
                if (vertex.position.y < 0f)
                {
                    Assert.That(tint.r, Is.Zero);
                    Assert.That(tint.g, Is.Zero);
                    Assert.That(tint.b, Is.Zero);
                }
                else
                {
                    Assert.That(tint.r, Is.EqualTo(0.8f).Within(1f / 255f));
                    Assert.That(tint.g, Is.EqualTo(0.6f).Within(1f / 255f));
                    Assert.That(tint.b, Is.EqualTo(0.4f).Within(1f / 255f));
                }
            }
        }
    }
}
