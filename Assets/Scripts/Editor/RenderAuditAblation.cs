using System;
using System.Linq;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.VContainer;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    [EditorToolInfo("Camera-pinned rendering ablation: 2 seconds settle + 6 seconds sample per phase.")]
    public static class RenderAuditAblation
    {
        private static RenderAuditAblationCapture _capture;

        static RenderAuditAblation()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            EditorApplication.quitting += Cancel;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Cancel();
            };
        }

        [MenuItem("Tools/Empire At War/Diagnostics/Performance/Rendering Ablation (Up to 208 Seconds)")]
        [CliCommand("render_audit_ablation", "Run camera-pinned rendering ablation in an existing battle.")]
        public static object Start()
        {
            if (_capture != null) throw new InvalidOperationException("An ablation is already running.");
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                throw new InvalidOperationException("A running, unpaused battle is required.");
            if (RenderAuditProfilerTool.IsCapturing || RenderAuditRenderGraphTool.IsCapturing || RenderAuditFrameDebuggerTool.IsCapturing)
                throw new InvalidOperationException("Finish the existing Render Audit capture first.");
            var scope = UnityEngine.Object.FindObjectsByType<SkirmishScope>().Single();
            var camera = scope.Container.Resolve<ICameraService>();
            var directory = RenderAuditCaptureStatus.CreateCaptureDirectory("ablation");
            _capture = new RenderAuditAblationCapture(scope, camera, directory);
            _capture.Start();
            return new { status = "running", capture_directory = directory };
        }

        [MenuItem("Tools/Empire At War/Diagnostics/Performance/Cancel Rendering Ablation")]
        [CliCommand("render_audit_ablation_cancel", "Restore all ablation toggles and release the camera.")]
        public static void Cancel()
        {
            if (_capture == null) return;
            _capture.Finish("cancelled");
            _capture = null;
        }

        internal static void Completed() => _capture = null;
    }
}
