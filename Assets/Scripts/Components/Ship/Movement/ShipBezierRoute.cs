using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public readonly struct CubicBezierSegment
    {
        public CubicBezierSegment(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        public Vector3 P0 { get; }
        public Vector3 P1 { get; }
        public Vector3 P2 { get; }
        public Vector3 P3 { get; }

        public Vector3 Evaluate(float parameter)
        {
            float t = Mathf.Clamp01(parameter);
            float inverse = 1f - t;
            return inverse * inverse * inverse * P0 +
                3f * inverse * inverse * t * P1 +
                3f * inverse * t * t * P2 +
                t * t * t * P3;
        }

        public Vector3 EvaluateTangent(float parameter)
        {
            float t = Mathf.Clamp01(parameter);
            float inverse = 1f - t;
            Vector3 tangent =
                3f * inverse * inverse * (P1 - P0) +
                6f * inverse * t * (P2 - P1) +
                3f * t * t * (P3 - P2);
            return tangent.sqrMagnitude > Mathf.Epsilon
                ? tangent.normalized
                : (P3 - P0).normalized;
        }
    }

    public sealed class ShipBezierRoute
    {
        private const int ARC_LENGTH_SAMPLES = 24;
        private const int DEBUG_SAMPLES_PER_SEGMENT = 12;

        private readonly SegmentArcLength[] _segments;

        public ShipBezierRoute(IReadOnlyList<CubicBezierSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            if (segments.Count == 0)
            {
                throw new ArgumentException(
                    "A Bézier route requires at least one segment.",
                    nameof(segments));
            }

            _segments = new SegmentArcLength[segments.Count];
            float totalLength = 0f;
            for (int i = 0; i < segments.Count; i++)
            {
                _segments[i] = new SegmentArcLength(
                    segments[i],
                    ARC_LENGTH_SAMPLES);
                totalLength += _segments[i].Length;
            }

            Length = totalLength;
            Samples = BuildSamples();
        }

        public float Length { get; }
        public Vector3[] Samples { get; }

        public Vector3 InitialTangent => _segments[0].Segment.EvaluateTangent(0f);

        public Vector3 EvaluateNormalizedDistance(
            float normalizedDistance,
            out Vector3 tangent)
        {
            if (Length <= Mathf.Epsilon)
            {
                tangent = InitialTangent;
                return _segments[_segments.Length - 1].Segment.P3;
            }

            float remainingDistance = Mathf.Clamp01(normalizedDistance) * Length;
            for (int i = 0; i < _segments.Length; i++)
            {
                SegmentArcLength segment = _segments[i];
                if (remainingDistance <= segment.Length || i == _segments.Length - 1)
                {
                    float parameter = segment.GetParameter(remainingDistance);
                    tangent = segment.Segment.EvaluateTangent(parameter);
                    return segment.Segment.Evaluate(parameter);
                }

                remainingDistance -= segment.Length;
            }

            tangent = _segments[_segments.Length - 1].Segment.EvaluateTangent(1f);
            return _segments[_segments.Length - 1].Segment.P3;
        }

        private Vector3[] BuildSamples()
        {
            List<Vector3> samples = new List<Vector3>(
                _segments.Length * DEBUG_SAMPLES_PER_SEGMENT + 1);
            for (int segmentIndex = 0; segmentIndex < _segments.Length; segmentIndex++)
            {
                int firstSample = segmentIndex == 0 ? 0 : 1;
                for (int sampleIndex = firstSample;
                     sampleIndex <= DEBUG_SAMPLES_PER_SEGMENT;
                     sampleIndex++)
                {
                    samples.Add(_segments[segmentIndex].Segment.Evaluate(
                        sampleIndex / (float)DEBUG_SAMPLES_PER_SEGMENT));
                }
            }

            return samples.ToArray();
        }

        private sealed class SegmentArcLength
        {
            private readonly float[] _cumulativeLengths;

            public SegmentArcLength(CubicBezierSegment segment, int sampleCount)
            {
                Segment = segment;
                _cumulativeLengths = new float[sampleCount + 1];
                Vector3 previous = segment.P0;
                for (int i = 1; i <= sampleCount; i++)
                {
                    Vector3 current = segment.Evaluate(i / (float)sampleCount);
                    _cumulativeLengths[i] =
                        _cumulativeLengths[i - 1] + Vector3.Distance(previous, current);
                    previous = current;
                }

                Length = _cumulativeLengths[sampleCount];
            }

            public CubicBezierSegment Segment { get; }
            public float Length { get; }

            public float GetParameter(float distance)
            {
                if (Length <= Mathf.Epsilon)
                {
                    return 1f;
                }

                float clampedDistance = Mathf.Clamp(distance, 0f, Length);
                for (int i = 1; i < _cumulativeLengths.Length; i++)
                {
                    if (clampedDistance > _cumulativeLengths[i])
                    {
                        continue;
                    }

                    float segmentLength =
                        _cumulativeLengths[i] - _cumulativeLengths[i - 1];
                    float fraction = segmentLength <= Mathf.Epsilon
                        ? 0f
                        : (clampedDistance - _cumulativeLengths[i - 1]) / segmentLength;
                    return (i - 1 + fraction) / (_cumulativeLengths.Length - 1);
                }

                return 1f;
            }
        }
    }
}
