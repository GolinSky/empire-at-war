using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>
    /// Adds the hull-raycast recommendation to the selected weapon hardpoint's arc gizmo: red ring where the hull
    /// blocks fire, cyan arc for the widest clear span, dimmer cyan for a second clear span of 30° or more.
    /// </summary>
    internal static class RecommendedArcGizmo
    {
        private const float BLOCKED_RADIUS = 1.1f;
        private const float SUGGESTED_RADIUS = 1.25f;
        private const float BLOCKED_THICKNESS = 3f;
        private const float SUGGESTED_THICKNESS = 2f;
        private static readonly Color BLOCKED_COLOR = new Color(1f, 0.3f, 0.2f, 0.9f);
        private static readonly Color SUGGESTED_COLOR = new Color(0.25f, 0.85f, 1f, 0.95f);
        private static readonly Color SUGGESTED_FILL = new Color(0.25f, 0.85f, 1f, 0.1f);
        private static readonly Color SECOND_COLOR = new Color(0.25f, 0.85f, 1f, 0.45f);

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void Draw(WeaponHardPoint weapon, GizmoType gizmoType)
        {
            HardPointArcProbeResult result = HardPointArcProbe.Probe(weapon);
            Transform transform = weapon.transform;
            Quaternion frame = transform.parent == null ? Quaternion.identity : transform.parent.rotation;
            Vector3 position = transform.position;
            Vector3 up = frame * Vector3.up;
            Vector3 forward = frame * Vector3.forward;
            float size = HandleUtility.GetHandleSize(position);
            Color previousColor = Handles.color;

            DrawBlocked(result, position, up, forward, size * BLOCKED_RADIUS);
            if (!result.IsInconclusive)
            {
                DrawArc(result.Suggested, position, up, forward, size * SUGGESTED_RADIUS, SUGGESTED_COLOR, true);
                if (result.HasSecondArc)
                    DrawArc(result.SecondArc, position, up, forward, size * SUGGESTED_RADIUS, SECOND_COLOR, false);
            }
            Handles.color = previousColor;
            Handles.Label(position + up * size * SUGGESTED_RADIUS, Describe(weapon, result));
        }

        private static void DrawBlocked(HardPointArcProbeResult result, Vector3 position, Vector3 up, Vector3 forward,
            float radius)
        {
            Handles.color = BLOCKED_COLOR;
            int count = result.Blocked.Count;
            int index = 0;
            while (index < count)
            {
                if (!result.Blocked[index])
                {
                    index++;
                    continue;
                }
                int start = index;
                while (index < count && result.Blocked[index]) index++;
                Vector3 from = Quaternion.AngleAxis(HardPointArcCalculator.YawOf(start), up) * forward;
                Handles.DrawWireArc(position, up, from, index - start, radius, BLOCKED_THICKNESS);
            }
        }

        private static void DrawArc(YawArc arc, Vector3 position, Vector3 up, Vector3 forward, float radius,
            Color color, bool filled)
        {
            Vector3 from = Quaternion.AngleAxis(arc.Min, up) * forward;
            if (filled)
            {
                Handles.color = SUGGESTED_FILL;
                Handles.DrawSolidArc(position, up, from, arc.Width, radius);
            }
            Handles.color = color;
            Handles.DrawWireArc(position, up, from, arc.Width, radius, SUGGESTED_THICKNESS);
        }

        private static string Describe(WeaponHardPoint weapon, HardPointArcProbeResult result)
        {
            string current = $"Current {weapon.MinYaw:0.#}..{weapon.MaxYaw:0.#}";
            if (result.IsInconclusive) return $"{current}\nRecommended: none, hardpoint is inside hull geometry";
            string text = $"{current}\nRecommended {result.Suggested}";
            if (result.Suggested.CrossesRear) text += " crosses rear, not one range";
            if (result.HasSecondArc) text += $"\nSecond {result.SecondArc}";
            return text;
        }
    }
}
