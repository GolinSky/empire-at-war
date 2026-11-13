using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public sealed class ShipRoute
    {
        private const float DISPLAY_ARC_STEP = 10f * Mathf.Deg2Rad;
        private const float TANGENT_BREAK_DEGREES = 1f;
        private readonly ShipRoutePiece[] _pieces;
        private readonly float[] _starts;
        private Vector3[] _samples;

        public IReadOnlyList<ShipRoutePiece> Pieces => _pieces;
        public float Length { get; }
        public Vector3 InitialTangent => _pieces[0].InitialTangent;
        public Vector3[] Samples => _samples ?? (_samples = BuildSamples());

        public ShipRoute(IReadOnlyList<ShipRoutePiece> pieces)
        {
            if (pieces.Count == 0) throw new ArgumentException("A route requires at least one piece.", nameof(pieces));
            _pieces = new ShipRoutePiece[pieces.Count];
            _starts = new float[pieces.Count];
            float length = 0f;
            for (int i = 0; i < pieces.Count; i++)
            {
                _pieces[i] = pieces[i];
                _starts[i] = length;
                length += pieces[i].Length;
            }
            Length = length;
        }

        public float GetPieceStartDistance(int index) => _starts[index];

        public bool RequiresTurnAtPiece(int index) => index > 0 &&
            Vector3.Angle(_pieces[index - 1].EndTangent, _pieces[index].InitialTangent) > TANGENT_BREAK_DEGREES;

        public Vector3 EvaluateDistance(float distance, ref int pieceIndex, out Vector3 tangent, out float curvature)
        {
            float clamped = Mathf.Clamp(distance, 0f, Length);
            while (pieceIndex < _pieces.Length - 1 && clamped >= _starts[pieceIndex + 1]) pieceIndex++;
            while (pieceIndex > 0 && clamped < _starts[pieceIndex]) pieceIndex--;
            ShipRoutePiece piece = _pieces[pieceIndex];
            curvature = piece.Curvature;
            return piece.Evaluate(clamped - _starts[pieceIndex], out tangent);
        }

        public Vector3 EvaluateNormalizedDistance(float normalizedDistance, out Vector3 tangent)
        {
            int pieceIndex = 0;
            return EvaluateDistance(Mathf.Clamp01(normalizedDistance) * Length, ref pieceIndex, out tangent, out _);
        }

        private Vector3[] BuildSamples()
        {
            List<Vector3> samples = new List<Vector3> { _pieces[0].Start };
            foreach (ShipRoutePiece piece in _pieces)
            {
                int count = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(piece.Length * piece.Curvature) / DISPLAY_ARC_STEP));
                for (int i = 1; i <= count; i++) samples.Add(piece.Evaluate(piece.Length * i / count, out _));
            }
            return samples.ToArray();
        }
    }
}
