using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    /// <summary>
    /// The forward arrival search (round 5, E1b): per unit, <see cref="POINTS"/> fixed points on a ring of
    /// <see cref="RADIUS"/>, the first straight away from the enemy, then alternating left and right; at most
    /// <see cref="MAX_UNITS"/> units. The cheap open test runs before the hull test.
    /// </summary>
    public static class ForwardSpawnRing
    {
        public const float RADIUS = 250f;
        public const int POINTS = 8;
        public const int MAX_UNITS = 3;

        public static bool TryFind(IReadOnlyList<Vector3> units, Vector3 awayFrom, Func<Vector3, bool> isOpen,
            Func<Vector3, bool> isClear, out Vector3 position, out ForwardSpawnAttempt attempt)
        {
            attempt = new ForwardSpawnAttempt { UnitIndex = -1 };
            int count = Mathf.Min(units.Count, MAX_UNITS);
            for (int unit = 0; unit < count; unit++)
            {
                Vector3 center = units[unit];
                Vector2 away = new Vector2(center.x - awayFrom.x, center.z - awayFrom.z);
                float baseAngle = away.sqrMagnitude > 1e-6f ? Mathf.Atan2(away.y, away.x) : 0f;
                for (int point = 0; point < POINTS; point++)
                {
                    // 0, +45, -45, +90, -90, +135, -135, 180 degrees from straight away.
                    int step = (point + 1) / 2;
                    float sign = point % 2 == 1 ? 1f : -1f;
                    float angle = baseAngle + sign * step * (2f * Mathf.PI / POINTS);
                    position = new Vector3(center.x + Mathf.Cos(angle) * RADIUS, 0f,
                        center.z + Mathf.Sin(angle) * RADIUS);
                    attempt.Points++;
                    if (!isOpen(position))
                    {
                        attempt.NotOpen++;
                        continue;
                    }
                    if (!isClear(position))
                    {
                        attempt.NotClear++;
                        continue;
                    }
                    attempt.UnitIndex = unit;
                    return true;
                }
            }

            position = default;
            return false;
        }
    }
}
