using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    [Serializable]
    internal sealed class ShipTangentFailureSnapshot
    {
        private const int CAPTURE_SLOTS = 4;
        private static int _captureIndex;

        [Serializable]
        internal struct AgentContext
        {
            public bool IsAvailable;
            public int RegistrationId;
            public float CurrentSpeed;
            public float Clearance;
            public float Height;
        }

        [Serializable]
        internal struct Corner
        {
            public Vector3 Position;
            public int DiskIndex;
            public int TurnSign;
            public float PortalWidth;
        }

        public int Version = 1;
        public string CapturedUtc;
        public long BuildId;
        public int Frame;
        public AgentContext Agent;
        public Vector3 Forward;
        public float TurnRadius;
        public bool AllowEscape;
        public bool CollapseLinks;
        public int FailureCode;
        public int RepairCount;
        public int RepairLimit;
        public float2 BoundsMin;
        public float2 BoundsMax;
        public Vector3[] Waypoints;
        public Corner[] Corners;
        public float3[] Disks;
        public ShipRouteCircle[] FinalCircles;

        public static ShipTangentFailureSnapshot Create(IShipPathGraph graph,
            IReadOnlyList<Vector3> waypoints, Vector3 forward, float turnRadius,
            bool allowEscape, bool collapseLinks, long buildId, AgentContext agent,
            ShipTangentRouteJob job)
        {
            var snapshot = new ShipTangentFailureSnapshot
            {
                CapturedUtc = DateTime.UtcNow.ToString("O"), BuildId = buildId, Frame = Time.frameCount,
                Agent = agent, Forward = forward, TurnRadius = turnRadius,
                AllowEscape = allowEscape, CollapseLinks = collapseLinks,
                FailureCode = job.Failure.Value, RepairCount = job.RepairMetrics.Value.x, RepairLimit = job.RepairMetrics.Value.y,
                BoundsMin = graph.Layout.Min, BoundsMax = graph.Layout.Max,
                Waypoints = new Vector3[waypoints.Count], Corners = new Corner[graph.RouteCorners.Count],
                Disks = graph.Disks.ToArray(), FinalCircles = job.Circles.AsArray().ToArray()
            };
            for (int i = 0; i < waypoints.Count; i++) snapshot.Waypoints[i] = waypoints[i];
            for (int i = 0; i < graph.RouteCorners.Count; i++)
            {
                NavMeshCorner corner = graph.RouteCorners[i];
                snapshot.Corners[i] = new Corner
                {
                    Position = corner.Position, DiskIndex = corner.DiskIndex,
                    TurnSign = corner.TurnSign, PortalWidth = corner.PortalWidth
                };
            }
            return snapshot;
        }

        public string Save(string directory)
        {
            Directory.CreateDirectory(directory);
            int slot = _captureIndex;
            _captureIndex = (_captureIndex + 1) % CAPTURE_SLOTS;
            string path = Path.Combine(directory, $"tangent-failure-{slot}.json");
            File.WriteAllText(path, JsonUtility.ToJson(this, true));
            return path;
        }

        public void SaveAndReport()
        {
            try
            {
                string path = Save(Path.Combine(Application.persistentDataPath, "ShipNavigationFailures"));
                Debug.Log($"[ShipNavigation] Failure snapshot: {path}; build {BuildId}, frame {Frame}, " +
                    $"agent registration {Agent.RegistrationId}, repairs {RepairCount}/{RepairLimit}.");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // Diagnostics must not prevent the checked polyline fallback from being returned.
                Debug.LogWarning($"[ShipNavigation] Could not save failure snapshot for build {BuildId}: {exception.Message}");
            }
        }
    }
}
