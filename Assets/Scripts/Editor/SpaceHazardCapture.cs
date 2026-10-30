using System.IO;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Map.Hazards;
using CameraData = EmpireAtWar.Services.Camera.CameraData;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace EmpireAtWar.Editor
{
    public static class SpaceHazardCapture
    {
        private const string OUTPUT_FOLDER = "Logs/SpaceHazardsShapeRework";

        public static void CaptureLoadedMap(MapLayoutView view, Camera sourceCamera)
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
            HazardCloudView[] hazards = view.GetComponentsInChildren<HazardCloudView>();
            if (hazards.Length == 0)
                throw new System.InvalidOperationException("The loaded MapLayoutView contains no hazard visuals.");

            RenderTexture previous = RenderTexture.active;
            var texture = new RenderTexture(1600, 1000, 24);
            var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("LoadedMapHazardCaptureCamera");
            try
            {
                SceneManager.MoveGameObjectToScene(cameraObject, sourceCamera.gameObject.scene);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.enabled = false;
                camera.scene = sourceCamera.scene;
                camera.aspect = 1.6f;
                var sourceData = sourceCamera.GetUniversalAdditionalCameraData();
                var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                EditorUtility.CopySerialized(sourceData, data);
                data.cameraStack.Clear();
                var settings = AssetDatabase.LoadAssetAtPath<CameraData>("Assets/Settings/Data/Models/Camera/CameraData.asset");
                for (int i = 0; i < hazards.Length; i++)
                {
                    Vector3 center = hazards[i].transform.position;
                    Render(camera, sourceCamera.transform.rotation, center, settings.ZoomRange.Max,
                        $"loaded-map-hazard-{i}-max-zoom.png", texture, image);
                    Render(camera, sourceCamera.transform.rotation, center, 650f,
                        $"loaded-map-hazard-{i}-medium-zoom.png", texture, image);
                }
                File.WriteAllText(Path.Combine(OUTPUT_FOLDER, "loaded-map-capture.txt"),
                    $"Actual loaded map hazards: {hazards.Length}\nCamera mask: {sourceCamera.cullingMask}\n" +
                    $"Scene: {sourceCamera.gameObject.scene.path}\nCamera rotation: {sourceCamera.transform.eulerAngles}\n");
            }
            finally
            {
                RenderTexture.active = previous;
                texture.Release();
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(cameraObject);
            }
        }

        public static void Capture(bool showBoundary)
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
            Scene scene = EditorSceneManager.NewPreviewScene();
            RenderTexture previous = RenderTexture.active;
            var texture = new RenderTexture(1600, 1000, 24);
            var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            try
            {
                var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
                var cameraSettings = AssetDatabase.LoadAssetAtPath<CameraData>("Assets/Settings/Data/Models/Camera/CameraData.asset");
                var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/View/Camera/Main Camera.prefab");
                var cameraObject = new GameObject("HazardCaptureCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.CopyFrom(cameraPrefab.GetComponent<Camera>());
                camera.enabled = false;
                camera.scene = scene;
                camera.backgroundColor = new Color(0.015f, 0.022f, 0.045f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.cullingMask = -1;
                camera.aspect = 1.6f;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                Quaternion rotation = cameraPrefab.transform.rotation;
                var lightObject = new GameObject("PreviewLight");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2f;
                light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

                for (int i = 0; i < settings.Hazards.Count; i++)
                {
                    SpaceHazardData data = settings.Hazards[i];
                    Vector3 center = new Vector3((i == 0 ? -1f : 1f) * 400f, 0f, 0f);
                    HazardSpot spot = i == 0
                        ? HazardCloudGenerator.CreateStream(data, center, Vector3.forward, 1600f, 200f, 10f, new Random(11))
                        : HazardCloudGenerator.CreateIsland(data, center, Vector3.forward, 1300f, 260f, 10f, new Random(5));
                    var shape = new HazardShape(spot);
                    var cloudPrefab = AssetDatabase.LoadAssetAtPath<HazardCloudView>("Assets/Prefabs/Vfx/SpaceHazards/SpaceHazard.prefab");
                    HazardCloudView cloud = Object.Instantiate(cloudPrefab);
                    SceneManager.MoveGameObjectToScene(cloud.gameObject, scene);
                    cloud.Configure(shape, settings.HazardFloor, settings.HazardCeiling);
                    GameObject ship = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Republic/View/VenatorShipView.prefab"));
                    SceneManager.MoveGameObjectToScene(ship, scene);
                    ship.transform.position = center + new Vector3(0f, -60f, 70f);
                    if (!showBoundary) continue;
                    var boundaryObject = new GameObject("GameplayBoundary");
                    SceneManager.MoveGameObjectToScene(boundaryObject, scene);
                    boundaryObject.AddComponent<MeshFilter>().sharedMesh = CreateBoundaryMarks(shape, -60f);
                    boundaryObject.AddComponent<MeshRenderer>().sharedMaterial =
                        AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/SpaceHazardArc.mat");
                }

                string suffix = showBoundary ? "-boundary" : string.Empty;
                Render(camera, rotation, Vector3.zero, cameraSettings.ZoomRange.Max, $"both-max-zoom{suffix}.png", texture, image);
                Render(camera, rotation, new Vector3(-400f, 0f, 70f), cameraSettings.ZoomRange.Min, $"nebula-min-zoom{suffix}.png", texture, image);
                Render(camera, rotation, new Vector3(400f, 0f, 70f), cameraSettings.ZoomRange.Min, $"ion-min-zoom{suffix}.png", texture, image);
                Render(camera, rotation, Vector3.zero, 650f, $"both-medium-zoom{suffix}.png", texture, image);
                Render(camera, Quaternion.Euler(18f, 0f, 0f), Vector3.zero, 450f, $"both-cinematic{suffix}.png", texture, image);
            }
            finally
            {
                RenderTexture.active = previous;
                texture.Release();
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(image);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // Small marks where the distance grid crosses zero, i.e. the gameplay outline.
        private static Mesh CreateBoundaryMarks(HazardShape shape, float height)
        {
            var vertices = new System.Collections.Generic.List<Vector3>();
            var colors = new System.Collections.Generic.List<Color>();
            var triangles = new System.Collections.Generic.List<int>();
            for (int row = 0; row < shape.Rows; row++)
                for (int column = 0; column < shape.Columns - 1; column++)
                {
                    float a = shape.GetDistance(column, row);
                    float b = shape.GetDistance(column + 1, row);
                    if ((a <= 0f) == (b <= 0f)) continue;
                    Vector3 mark = Vector3.Lerp(shape.GetSamplePosition(column, row), shape.GetSamplePosition(column + 1, row), a / (a - b));
                    mark.y = height;
                    int index = vertices.Count;
                    vertices.Add(mark + new Vector3(-3f, 0f, -3f));
                    vertices.Add(mark + new Vector3(-3f, 0f, 3f));
                    vertices.Add(mark + new Vector3(3f, 0f, 3f));
                    vertices.Add(mark + new Vector3(3f, 0f, -3f));
                    for (int i = 0; i < 4; i++) colors.Add(new Color(1f, 1f, 1f, 0.9f));
                    triangles.AddRange(new[] { index, index + 1, index + 2, index, index + 2, index + 3 });
                }
            var mesh = new Mesh { name = "Hazard boundary marks", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            return mesh;
        }

        private static void Render(Camera camera, Quaternion rotation, Vector3 target, float height,
            string filename, RenderTexture destination, Texture2D image)
        {
            Vector3 forward = rotation * Vector3.forward;
            camera.transform.SetPositionAndRotation(target - forward * (height / -forward.y), rotation);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = destination });
            RenderTexture.active = destination;
            image.ReadPixels(new Rect(0f, 0f, destination.width, destination.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(OUTPUT_FOLDER, filename), image.EncodeToPNG());
        }
    }
}
