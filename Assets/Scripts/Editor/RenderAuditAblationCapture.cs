using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.VContainer;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace EmpireAtWar.Editor
{
    internal sealed class RenderAuditAblationCapture
    {
        private readonly string[] _phases =
        {
            "baseline", "decor_off", "decor_restored", "field_rocks_off", "field_rocks_restored",
            "all_rocks_off", "all_rocks_restored", "nebula_off", "nebula_restored", "stars_off", "stars_restored",
            "hazard_dust_off", "hazard_dust_restored", "fog_off", "fog_restored", "postfx_off", "postfx_restored",
            "sun_shadows_off", "sun_shadows_restored", "rocks_nebula_postfx_shadows_off", "restored"
        };
        private readonly string _directory;
        private readonly ICameraService _cameraService;
        private readonly Camera _camera;
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;
        private readonly float _fieldOfView;
        private readonly float _zoomHeight;
        private readonly HazardShape[] _hazards;
        private readonly Vector3 _hazardPosition;
        private Vector3 _capturePosition;
        private readonly UniversalAdditionalCameraData _cameraData;
        private readonly bool _postFx;
        private readonly bool _nebulaEnabled;
        private readonly Dictionary<Renderer, bool> _renderers;
        private readonly Dictionary<DecorAsteroidRenderer, bool> _decor;
        private readonly Dictionary<Light, LightShadows> _lights;
        private readonly Dictionary<string, Renderer[]> _groups;
        private readonly List<object> _results = new List<object>();
        private readonly List<double> _frames = new List<double>();
        private readonly List<double> _gpu = new List<double>();
        private readonly List<double> _triangles = new List<double>();
        private readonly FrameTiming[] _timings = new FrameTiming[1];
        private readonly object _metadata;
        private readonly IInputLock _inputLock;
        private IDisposable _lock;
        private int _phase;
        private int _lastFrame = -1;
        private double _phaseStart;
        private double _lastSample;
        private double _sampleStart;
        private ulong _lastGpuTimestamp;
        private bool _finished;

        internal RenderAuditAblationCapture(SkirmishScope scope, ICameraService camera, string directory)
        {
            _directory = directory;
            _cameraService = camera;
            _camera = camera.SceneCamera;
            _position = _camera.transform.position;
            _rotation = _camera.transform.rotation;
            _fieldOfView = _camera.fieldOfView;
            _zoomHeight = camera.ZoomTargetHeight;
            _capturePosition = _position;
            _hazards = scope.Container.Resolve<IHazardService>().Sources.OfType<HazardShape>().ToArray();
            if (_hazards.Length > 0)
            {
                HazardShape largest = _hazards.OrderByDescending(h => h.Bounds.size.x * h.Bounds.size.z).First();
                Vector3 forward = _rotation * Vector3.forward;
                Vector3 target = largest.Bounds.center;
                target.y = -60f;
                _hazardPosition = target - forward * ((_position.y - target.y) / -forward.y);
                _phases = _phases.Concat(new[] { "hazard_pose_baseline", "hazard_pose_nebula_off",
                    "hazard_pose_nebula_restored", "hazard_pose_hazards_off", "hazard_pose_hazards_restored" }).ToArray();
            }
            _cameraData = _camera.GetUniversalAdditionalCameraData();
            _postFx = _cameraData.renderPostProcessing;
            _nebulaEnabled = MatchNebula.NebulaEnabled;
            _inputLock = scope.Container.Resolve<IInputLock>();
            // Discovery belongs to this Editor diagnostic only; production dependencies stay explicit.
            _renderers = UnityEngine.Object.FindObjectsByType<Renderer>().ToDictionary(r => r, r => r.enabled);
            _decor = UnityEngine.Object.FindObjectsByType<DecorAsteroidRenderer>().ToDictionary(r => r, r => r.enabled);
            _lights = UnityEngine.Object.FindObjectsByType<Light>().Where(l => l.type == LightType.Directional).ToDictionary(l => l, l => l.shadows);
            var map = UnityEngine.Object.FindObjectsByType<MapLayoutView>().Single();
            var fieldRoot = (Transform)new SerializedObject(map).FindProperty("fieldRoot").objectReferenceValue;
            Renderer[] hazards = ByShader("SpaceHazard", "Map Space Dust");
            _groups = new Dictionary<string, Renderer[]>
            {
                ["field"] = _renderers.Keys.Where(r => r is MeshRenderer && r.transform.IsChildOf(fieldRoot) && !hazards.Contains(r)).ToArray(),
                ["nebula"] = ByShader("Nebula Filament Volume", "Nebula Cloud Volume", "Nebula Billow Volume", "VolumetricNebula", "Nebula Impostor"),
                ["stars"] = _renderers.Keys.Where(r => r.name == "StarVfx" || r.name == "BrightStars" || HasShader(r, "Nebula Stars")).ToArray(),
                ["hazard"] = hazards,
                ["fog"] = ByShader("URP_FogOfWar")
            };
            _renderers = _groups.Values.SelectMany(group => group).Distinct().ToDictionary(r => r, r => r.enabled);
            _metadata = new
            {
                seed = scope.Battle.Map.Seed, battle_mode = scope.Battle.Map.Type.ToString(),
                source_revision = ReadGit("rev-parse HEAD"), source_state = ReadGit("status --porcelain --untracked-files=normal"),
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                unity = Application.unityVersion, platform = Application.platform.ToString(),
                gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(),
                width = _camera.pixelWidth, height = _camera.pixelHeight,
                position = new[] { _position.x, _position.y, _position.z }, rotation = new[] { _rotation.x, _rotation.y, _rotation.z, _rotation.w },
                fov = _fieldOfView, far_clip = _camera.farClipPlane,
                vsync = QualitySettings.vSyncCount, target_fps = Application.targetFrameRate,
                time_scale = Time.timeScale, nebula_variant = UnityEngine.Object.FindObjectsByType<MatchNebula>().Select(n => n.SelectedVariant).ToArray(),
                counts = _groups.ToDictionary(p => p.Key, p => p.Value.Length), decor_renderers = _decor.Count,
                settle_seconds = 2, sample_seconds = 6,
                hazard_pose_position = new[] { _hazardPosition.x, _hazardPosition.y, _hazardPosition.z },
                hazard_coverage_method = "32x18 viewport ray grid against gameplay SDF at top sheet y=-60; excludes wisps and occlusion",
                caveat = "Editor-wide triangle counts; close Scene view. Battle simulation continues. Zero GPU timing means unsupported and is omitted."
            };
        }

        internal void Start()
        {
            _lock = _inputLock.Acquire();
            _cameraService.SetPose(_capturePosition, _rotation);
            RenderPipelineManager.beginCameraRendering += Pin;
            RenderPipelineManager.endCameraRendering += Sample;
            EditorApplication.update += Update;
            _phaseStart = Time.realtimeSinceStartupAsDouble;
            Write("running");
        }

        private Renderer[] ByShader(params string[] names) => _renderers.Keys.Where(r => names.Any(n => HasShader(r, n))).ToArray();
        private static bool HasShader(Renderer renderer, string name) => renderer.sharedMaterials.Any(m => m != null && m.shader.name.Contains(name));

        private static string ReadGit(string arguments)
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            });
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException($"git {arguments} failed ({process.ExitCode}).");
            return output;
        }

        private void Pin(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera) return;
            _cameraService.SetPose(_capturePosition, _rotation);
            _camera.fieldOfView = _fieldOfView;
        }

        private void Update()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || _camera == null)
            {
                Finish("interrupted");
                RenderAuditAblation.Completed();
                return;
            }
            _cameraService.SetPose(_capturePosition, _rotation);
            if (Time.realtimeSinceStartupAsDouble - _phaseStart > 20)
            {
                Finish("failed_no_rendered_frames");
                RenderAuditAblation.Completed();
            }
        }

        private void Sample(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || _lastFrame == Time.frameCount) return;
            _lastFrame = Time.frameCount;
            double now = Time.realtimeSinceStartupAsDouble;
            FrameTimingManager.CaptureFrameTimings();
            if (now - _phaseStart < 2) return;
            if (_lastSample == 0) { _lastSample = now; _sampleStart = now; return; }
            _frames.Add((now - _lastSample) * 1000);
            _lastSample = now;
            _triangles.Add(UnityStats.triangles);
            if (FrameTimingManager.GetLatestTimings(1, _timings) > 0 && _timings[0].gpuFrameTime > 0 &&
                _timings[0].frameStartTimestamp != _lastGpuTimestamp)
            {
                _lastGpuTimestamp = _timings[0].frameStartTimestamp;
                _gpu.Add(_timings[0].gpuFrameTime);
            }
            if (now - _sampleStart < 6) return;
            _results.Add(new { phase = _phases[_phase], hazard_screen_coverage = HazardScreenCoverage(), frames = _frames.Count, fps = _frames.Count / (now - _sampleStart), mean_ms = _frames.Average(), p95_ms = Percentile(_frames, 0.95), gpu_samples = _gpu.Count, gpu_median_ms = _gpu.Count > 0 ? (double?)Percentile(_gpu, 0.5) : null, median_triangles = Percentile(_triangles, 0.5), battle_time = Time.time });
            Restore();
            if (++_phase == _phases.Length)
            {
                Finish("completed");
                RenderAuditAblation.Completed();
                return;
            }
            ApplyPhase();
            _frames.Clear(); _gpu.Clear(); _triangles.Clear();
            _lastSample = 0;
            _phaseStart = now;
            Write("running");
        }

        private void ApplyPhase()
        {
            string phase = _phases[_phase];
            _capturePosition = phase.StartsWith("hazard_pose_", StringComparison.Ordinal) ? _hazardPosition : _position;
            _cameraService.SetPose(_capturePosition, _rotation);
            bool all = phase == "rocks_nebula_postfx_shadows_off";
            if (all || phase == "decor_off" || phase == "all_rocks_off") foreach (var pair in _decor) pair.Key.enabled = false;
            if (all || phase == "field_rocks_off" || phase == "all_rocks_off") Disable("field");
            if (all || phase == "nebula_off" || phase == "hazard_pose_nebula_off")
            {
                MatchNebula.NebulaEnabled = false;
                Disable("nebula");
            }
            if (phase == "stars_off") Disable("stars");
            if (phase == "hazard_dust_off" || phase == "hazard_pose_hazards_off") Disable("hazard");
            if (phase == "fog_off") Disable("fog");
            if (all || phase == "postfx_off") _cameraData.renderPostProcessing = false;
            if (all || phase == "sun_shadows_off") foreach (var pair in _lights) pair.Key.shadows = LightShadows.None;
        }

        private void Disable(string group) { foreach (Renderer renderer in _groups[group]) renderer.enabled = false; }

        private float HazardScreenCoverage()
        {
            const int COLUMNS = 32;
            const int ROWS = 18;
            Plane plane = new Plane(Vector3.up, new Vector3(0f, -60f, 0f));
            int covered = 0;
            for (int y = 0; y < ROWS; y++)
                for (int x = 0; x < COLUMNS; x++)
                {
                    Ray ray = _camera.ViewportPointToRay(new Vector3((x + 0.5f) / COLUMNS, (y + 0.5f) / ROWS));
                    if (!plane.Raycast(ray, out float distance)) continue;
                    Vector3 point = ray.GetPoint(distance);
                    if (_hazards.Any(hazard => hazard.SignedDistance(point) <= 0f)) covered++;
                }
            return (float)covered / (COLUMNS * ROWS);
        }
        private static double Percentile(List<double> values, double p) => values.OrderBy(v => v).ElementAt(Math.Min(values.Count - 1, (int)(values.Count * p)));

        private void Restore()
        {
            MatchNebula.NebulaEnabled = _nebulaEnabled;
            foreach (var pair in _renderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
            foreach (var pair in _decor) if (pair.Key != null) pair.Key.enabled = pair.Value;
            foreach (var pair in _lights) if (pair.Key != null) pair.Key.shadows = pair.Value;
            if (_cameraData != null) _cameraData.renderPostProcessing = _postFx;
        }

        internal void Finish(string status)
        {
            if (_finished) return;
            _finished = true;
            EditorApplication.update -= Update;
            RenderPipelineManager.beginCameraRendering -= Pin;
            RenderPipelineManager.endCameraRendering -= Sample;
            try
            {
                Restore();
                if (_camera != null) { _cameraService.RestorePose(_position, _rotation, _zoomHeight); _camera.fieldOfView = _fieldOfView; }
            }
            finally
            {
                _lock.Dispose();
                Write(status);
            }
        }

        private void Write(string status)
        {
            var settings = new JsonSerializerSettings { Culture = CultureInfo.InvariantCulture, Formatting = Formatting.Indented };
            File.WriteAllText(Path.Combine(_directory, "ablation.json"), JsonConvert.SerializeObject(new { status, captured_at_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), metadata = _metadata, results = _results }, settings));
            RenderAuditCaptureStatus.WriteJson(_directory, "capture-status.json", new { status, phase = _phases[Math.Min(_phase, _phases.Length - 1)], completed_phases = _results.Count });
        }
    }
}
