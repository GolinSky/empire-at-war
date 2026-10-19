using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>Scene-view drawing and hull-click placement for <see cref="HardPointPlacementWindow"/>.</summary>
    internal static class HardPointSceneDrawer
    {
        private const float MARKER_SIZE = 0.09f;
        private const float BONE_SIZE = 0.05f;
        private const float ARC_SIZE = 0.6f;
        private static readonly Color BONE_COLOR = new Color(0.75f, 0.75f, 0.75f, 0.8f);
        private static readonly Color SELECTED_COLOR = Color.white;
        private static readonly Color PREVIEW_COLOR = new Color(1f, 0.85f, 0.2f, 0.9f);

        public static Color GetColor(HardPoint hardPoint)
        {
            if (hardPoint is WeaponHardPoint) return new Color(1f, 0.35f, 0.25f);
            switch (hardPoint.HardPointType)
            {
                case HardPointType.Engines: return new Color(1f, 0.6f, 0.1f);
                case HardPointType.ShieldGenerator: return new Color(0.3f, 0.7f, 1f);
                case HardPointType.Hangar: return new Color(0.75f, 0.45f, 1f);
                case HardPointType.TractorBeam: return new Color(0.2f, 1f, 0.8f);
                default: return new Color(1f, 1f, 0.4f);
            }
        }

        public static void DrawHardPoints(IReadOnlyList<HardPoint> hardPoints, bool labels, bool arcs,
            Action<HardPoint> clicked)
        {
            foreach (HardPoint hardPoint in hardPoints)
            {
                if (hardPoint == null) continue;
                Vector3 position = hardPoint.transform.position;
                float size = HandleUtility.GetHandleSize(position);
                bool selected = Selection.activeGameObject == hardPoint.gameObject;
                Handles.color = selected ? SELECTED_COLOR : GetColor(hardPoint);
                if (Handles.Button(position, Quaternion.identity, size * MARKER_SIZE, size * MARKER_SIZE,
                        Handles.SphereHandleCap))
                    clicked(hardPoint);
                if (labels) Handles.Label(position + Vector3.up * size * MARKER_SIZE, $"{hardPoint.Id} {hardPoint.name}");
                if (arcs && hardPoint is WeaponHardPoint weapon) DrawArc(weapon, size * ARC_SIZE);
            }
        }

        public static void DrawBones(IReadOnlyList<Transform> bones, bool labels, Action<Transform> clicked)
        {
            Handles.color = BONE_COLOR;
            foreach (Transform bone in bones)
            {
                Vector3 position = bone.position;
                float size = HandleUtility.GetHandleSize(position) * BONE_SIZE;
                if (Handles.Button(position, Quaternion.identity, size, size, Handles.RectangleHandleCap))
                    clicked(bone);
                if (labels) Handles.Label(position + Vector3.up * size, bone.name);
            }
        }

        /// <summary>
        /// Takes over left-clicks while placement mode is on, previews the hull point under the cursor and
        /// returns true with that point when the user clicks the hull.
        /// </summary>
        public static bool TryPlace(SceneView sceneView, out Vector3 point)
        {
            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (current.type == EventType.Layout) HandleUtility.AddDefaultControl(controlId);
            if (current.type == EventType.MouseMove) sceneView.Repaint();

            bool hit = HandleUtility.PlaceObject(current.mousePosition, out point, out Vector3 normal);
            if (!hit) return false;
            if (current.type == EventType.Repaint)
            {
                Handles.color = PREVIEW_COLOR;
                Handles.DrawWireDisc(point, normal, HandleUtility.GetHandleSize(point) * MARKER_SIZE * 1.5f);
            }
            if (current.type != EventType.MouseDown || current.button != 0 || current.alt) return false;
            current.Use();
            return true;
        }

        private static void DrawArc(WeaponHardPoint weapon, float radius)
        {
            float minYaw = Mathf.Clamp(weapon.MinYaw, -180f, 180f);
            float maxYaw = Mathf.Clamp(weapon.MaxYaw, -180f, 180f);
            if (maxYaw < minYaw) return;
            Transform parent = weapon.transform.parent;
            Quaternion parentRotation = parent == null ? Quaternion.identity : parent.rotation;
            Vector3 up = parentRotation * Vector3.up;
            Vector3 start = Quaternion.AngleAxis(minYaw, up) * (parentRotation * Vector3.forward);
            Vector3 position = weapon.transform.position;
            Handles.color = new Color(0.2f, 1f, 0.3f, 0.12f);
            Handles.DrawSolidArc(position, up, start, maxYaw - minYaw, radius);
            Handles.color = new Color(0.2f, 1f, 0.3f, 0.7f);
            Handles.DrawWireArc(position, up, start, maxYaw - minYaw, radius);
        }
    }
}
