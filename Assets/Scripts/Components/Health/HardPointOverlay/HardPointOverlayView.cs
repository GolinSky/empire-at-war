using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    public sealed class HardPointOverlayView : MonoBehaviour, IHardPointOverlayView
    {
        private readonly List<HardPointMarkerView> _markers = new List<HardPointMarkerView>();
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _markerRoot;

        public float ScaleFactor => _canvas.scaleFactor;

        private void Awake()
        {
            BuildCanvas();
        }

        public void ShowMarker(int slot, HardPointMarkerData data)
        {
            while (_markers.Count <= slot)
            {
                _markers.Add(HardPointMarkerView.Create(_markerRoot));
            }

            _markers[slot].Show(ToLocal(data.ScreenPosition), data);
        }

        public void HideMarkersFrom(int slot)
        {
            for (int index = slot; index < _markers.Count; index++)
            {
                _markers[index].Hide();
            }
        }

        private Vector2 ToLocal(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, null,
                out Vector2 localPosition);
            return localPosition;
        }

        private void BuildCanvas()
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            GameObject canvasObject = new("HardPointOverlayCanvas", typeof(RectTransform));
            canvasObject.layer = uiLayer;
            canvasObject.transform.SetParent(transform, false);

            _canvasRect = (RectTransform)canvasObject.transform;
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = -1;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject markerRootObject = new("Markers", typeof(RectTransform));
            markerRootObject.layer = uiLayer;
            _markerRoot = (RectTransform)markerRootObject.transform;
            _markerRoot.SetParent(_canvasRect, false);
        }
    }
}
