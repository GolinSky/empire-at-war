using System;
using EmpireAtWar.Models.Players;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public sealed class SkirmishColorPaletteView : MonoBehaviour
    {
        private const float PANEL_GAP = 6f;

        [SerializeField] private RectTransform bounds;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text colorName;
        [SerializeField] private RectTransform selectedMarker;
        [SerializeField] private SkirmishColorSwatch[] swatches;

        private readonly Vector3[] _openerCorners = new Vector3[4];
        private TeamColorPalette _palette;
        private Button _opener;
        private int _selectedIndex;
        private int _previewIndex = -1;
        private Func<int, string> _swapHint;
        private (UnityAction Select, Action Preview)[] _listeners;

        public event Action<int> ColorSelected;
        public bool IsOpen => gameObject.activeSelf;

        public void Initialize(TeamColorPalette palette)
        {
            _palette = palette;
            _listeners = new (UnityAction, Action)[swatches.Length];
            for (int i = 0; i < swatches.Length; i++)
            {
                int index = i;
                SkirmishColorSwatch swatch = swatches[i];
                swatch.targetGraphic.color = palette.GetColor(index);
                _listeners[i] = (() => SelectColor(index), () => ShowPreview(index));
                swatch.onClick.AddListener(_listeners[i].Select);
                swatch.Previewed += _listeners[i].Preview;
                swatch.PreviewEnded += ResetPreview;
            }

            dismissButton.onClick.AddListener(Dismiss);
            closeButton.onClick.AddListener(Dismiss);
        }

        public void Dispose()
        {
            for (int i = 0; i < swatches.Length; i++)
            {
                SkirmishColorSwatch swatch = swatches[i];
                swatch.onClick.RemoveListener(_listeners[i].Select);
                swatch.Previewed -= _listeners[i].Preview;
                swatch.PreviewEnded -= ResetPreview;
            }

            dismissButton.onClick.RemoveListener(Dismiss);
            closeButton.onClick.RemoveListener(Dismiss);
        }

        public void Open(Button opener, int selectedIndex, Func<int, string> swapHint)
        {
            _opener = opener;
            _swapHint = swapHint;
            _previewIndex = -1;
            gameObject.SetActive(true);
            SetSelection(selectedIndex);
            PositionPanel();
            swatches[selectedIndex].Select();
        }

        public bool Close()
        {
            if (!IsOpen) return false;

            gameObject.SetActive(false);
            if (_opener.interactable) _opener.Select();
            _opener = null;
            return true;
        }

        public void SetSelection(int index)
        {
            _selectedIndex = index;
            selectedMarker.SetParent(swatches[index].transform, false);
            RenderColorName();
        }

        private void LateUpdate() => PositionPanel();

        private void PositionPanel()
        {
            ((RectTransform)_opener.transform).GetWorldCorners(_openerCorners);
            Vector3 bottomRight = bounds.InverseTransformPoint(_openerCorners[3]);
            Vector3 topRight = bounds.InverseTransformPoint(_openerCorners[2]);
            Rect area = bounds.rect;
            Rect size = panel.rect;
            float right = Mathf.Clamp(bottomRight.x, area.xMin + size.width, area.xMax);
            float top = bottomRight.y - PANEL_GAP;
            if (top - size.height < area.yMin) top = topRight.y + PANEL_GAP + size.height;
            top = Mathf.Clamp(top, area.yMin + size.height, area.yMax);
            panel.localPosition = new Vector3(right, top, 0f);
        }

        private void Dismiss() => Close();

        private void SelectColor(int index)
        {
            if (Close()) ColorSelected?.Invoke(index);
        }

        private void ShowPreview(int index)
        {
            _previewIndex = index;
            RenderColorName();
        }

        private void ResetPreview()
        {
            _previewIndex = -1;
            RenderColorName();
        }

        private void RenderColorName()
        {
            int index = _previewIndex >= 0 ? _previewIndex : _selectedIndex;
            colorName.text = _palette.GetName(index) + _swapHint(index);
        }
    }
}
