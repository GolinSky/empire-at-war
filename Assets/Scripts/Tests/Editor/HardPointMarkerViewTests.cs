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

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("MarkerTest", typeof(RectTransform));
            _marker = HardPointMarkerView.Create((RectTransform)_root.transform);
            _icon = _marker.transform.GetChild(0).GetComponent<Image>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Create_ContainsOnlyTheLargerIcon()
        {
            Assert.That(_marker.transform.childCount, Is.EqualTo(1));
            Assert.That(_icon.rectTransform.sizeDelta, Is.EqualTo(new Vector2(25f, 25f)));
            Assert.That(_icon.raycastTarget, Is.False);

            Outline outline = _icon.GetComponent<Outline>();
            Assert.That(outline, Is.Not.Null);
            Assert.That(outline.effectColor, Is.EqualTo(new Color(0.03f, 0.04f, 0.06f, 0.9f)));
            Assert.That(outline.effectDistance, Is.EqualTo(new Vector2(1f, -1f)));
            Assert.That(outline.useGraphicAlpha, Is.True);
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
