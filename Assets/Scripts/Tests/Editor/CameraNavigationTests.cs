using System.Reflection;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Camera;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Camera
{
    public sealed class CameraNavigationTests
    {
        [TestCase(60f, 30f, 0f)]
        [TestCase(500f, 30f, 0f)]
        [TestCase(940f, 30f, 0f)]
        [TestCase(940f, 60f, 90f)]
        public void MoveTo_CentresMinimapTargetsAndClampsOutsideTargets(float height, float pitch, float yaw)
        {
            GameObject cameraObject = new GameObject("Navigation test camera");
            try
            {
                CameraService service = cameraObject.AddComponent<CameraService>();
                var range = new Vector2Range();
                range.GetType().BaseType.GetField("<Min>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(range, new Vector2(-100f, -80f));
                range.GetType().BaseType.GetField("<Max>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(range, new Vector2(200f, 120f));
                var map = new MapLayout(null, null, null, null, null, range, Vector3.zero);
                CameraData data = UnityEditor.AssetDatabase.LoadAssetAtPath<CameraData>(
                    "Assets/Settings/Data/Models/Camera/CameraData.asset");
                service.Constructor(null, map, null, null, null, data);
                service.SetPose(new Vector3(0f, height, 0f), Quaternion.Euler(pitch, yaw, 0f));

                foreach (Vector3 target in new[]
                {
                    Vector3.zero, new Vector3(-100f, 0f, -80f), new Vector3(200f, 0f, 120f),
                    new Vector3(-10000f, 0f, 10000f), new Vector3(10000f, 0f, -10000f)
                })
                {
                    service.MoveTo(target);
                    var ray = new Ray(service.CameraPosition, service.CameraForward);
                    Assert.That(new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance), Is.True);
                    Vector3 ground = ray.GetPoint(distance);
                    Assert.That(ground.x, Is.EqualTo(Mathf.Clamp(target.x, -100f, 200f)).Within(0.001f));
                    Assert.That(ground.z, Is.EqualTo(Mathf.Clamp(target.z, -80f, 120f)).Within(0.001f));
                    Assert.That(service.CameraPosition.y, Is.EqualTo(height).Within(0.001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
