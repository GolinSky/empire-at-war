using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    public sealed class CameraFrustumProjection
    {
        private readonly Plane[] _planes = new Plane[6];
        private List<Vector3> _input = new List<Vector3>(10);
        private List<Vector3> _output = new List<Vector3>(10);

        public IReadOnlyList<Vector3> Project(UnityEngine.Camera camera, Vector2 mapMin, Vector2 mapMax)
        {
            GeometryUtility.CalculateFrustumPlanes(camera.cullingMatrix, _planes);
            _input.Clear();
            _input.Add(new Vector3(mapMin.x, 0f, mapMin.y));
            _input.Add(new Vector3(mapMax.x, 0f, mapMin.y));
            _input.Add(new Vector3(mapMax.x, 0f, mapMax.y));
            _input.Add(new Vector3(mapMin.x, 0f, mapMax.y));

            foreach (Plane plane in _planes)
            {
                if (_input.Count == 0)
                    break;

                _output.Clear();
                Vector3 previous = _input[_input.Count - 1];
                float previousDistance = plane.GetDistanceToPoint(previous);
                foreach (Vector3 current in _input)
                {
                    float currentDistance = plane.GetDistanceToPoint(current);
                    bool previousInside = previousDistance >= 0f;
                    bool currentInside = currentDistance >= 0f;
                    if (previousInside != currentInside)
                    {
                        float t = previousDistance / (previousDistance - currentDistance);
                        _output.Add(Vector3.LerpUnclamped(previous, current, t));
                    }

                    if (currentInside)
                        _output.Add(current);

                    previous = current;
                    previousDistance = currentDistance;
                }

                List<Vector3> swap = _input;
                _input = _output;
                _output = swap;
            }

            return _input;
        }
    }
}
