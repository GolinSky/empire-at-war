using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.MPUIKIT;

namespace EmpireAtWar.Tests.Editor
{
    public class MPImageMeshTests
    {
        [Test]
        public void SpriteFreeImages_ProduceFiniteMeshAtRequestedSize(
            [Values(0f, 0.25f, 0.5f, 288f)] float width,
            [Values(0f, 0.25f, 24f)] float height, [Values(false, true)] bool filled,
            [Values(false, true)] bool preserveAspect)
        {
            GameObject imageObject = new("ProceduralImage", typeof(RectTransform));
            Mesh mesh = new();
            try
            {
                RectTransform rectTransform = (RectTransform)imageObject.transform;
                rectTransform.sizeDelta = new Vector2(width, height);
                using VertexHelper vertices = new();
                if (filled)
                {
                    MPImageHelper.GenerateFilledSprite(vertices, preserveAspect, null, rectTransform,
                        null, Color.white, Image.FillMethod.Horizontal, 0.5f,
                        (int)Image.OriginHorizontal.Left, true, 1f);
                }
                else
                {
                    MPImageHelper.GenerateSimpleSprite(vertices, preserveAspect, null, rectTransform,
                        null, Color.white, 1f);
                }

                Assert.That(vertices.currentVertCount, Is.EqualTo(4));
                for (int i = 0; i < vertices.currentVertCount; i++)
                {
                    UIVertex vertex = default;
                    vertices.PopulateUIVertex(ref vertex, i);
                    Assert.That(float.IsNaN(vertex.position.x) || float.IsInfinity(vertex.position.x),
                        Is.False, $"Vertex {i} has an invalid x coordinate.");
                    Assert.That(float.IsNaN(vertex.position.y) || float.IsInfinity(vertex.position.y),
                        Is.False, $"Vertex {i} has an invalid y coordinate.");
                }

                vertices.FillMesh(mesh);
                float expectedWidth = filled ? width * 0.5f : width;
                Assert.That(mesh.bounds.size.x, Is.EqualTo(expectedWidth).Within(0.0001f));
                Assert.That(mesh.bounds.size.y, Is.EqualTo(height).Within(0.0001f));
                Assert.That(mesh.bounds.min.x, Is.EqualTo(rectTransform.rect.xMin).Within(0.0001f));
                Assert.That(mesh.bounds.min.y, Is.EqualTo(rectTransform.rect.yMin).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(imageObject);
            }
        }
    }
}
