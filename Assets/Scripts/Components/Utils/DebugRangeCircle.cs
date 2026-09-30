using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Cheats;
using UnityEngine;

namespace EmpireAtWar.Utils
{
    // Game-view debug ring drawn with a LineRenderer while the range cheat is on and the unit is selected.
    // Editor and development builds only.
    public sealed class DebugRangeCircle
    {
        private const int SEGMENTS = 96;
        private const float LINE_WIDTH = 1.5f;

        private readonly GameObject _gameObject;
        private readonly LineRenderer _line;
        private readonly IRangeDebugObserver _rangeDebug;
        private readonly ISelectionModelObserver _selection;
        private float _radius = -1f;

        public DebugRangeCircle(string name, Color color, Material lineMaterial, IRangeDebugObserver rangeDebug,
            ISelectionModelObserver selection)
        {
            _rangeDebug = rangeDebug;
            _selection = selection;
            _gameObject = new GameObject(name);
            _gameObject.SetActive(false);
            _line = _gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.positionCount = SEGMENTS;
            _line.widthMultiplier = LINE_WIDTH;
            _line.sharedMaterial = lineMaterial;
            _line.startColor = color;
            _line.endColor = color;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
        }

        public void Draw(Vector3 center, float radius)
        {
            bool isVisible = _rangeDebug.IsEnabled && _selection.IsSelected;
            if (_gameObject.activeSelf != isVisible) _gameObject.SetActive(isVisible);
            if (!isVisible) return;

            _gameObject.transform.SetPositionAndRotation(center, Quaternion.identity);
            if (Mathf.Approximately(radius, _radius)) return;

            _radius = radius;
            for (int i = 0; i < SEGMENTS; i++)
            {
                float angle = i * Mathf.PI * 2f / SEGMENTS;
                _line.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius));
            }
        }

        public void Destroy()
        {
            Object.Destroy(_gameObject);
        }
    }
}
