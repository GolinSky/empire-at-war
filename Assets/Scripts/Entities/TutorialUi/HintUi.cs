using System.Collections.Generic;
using EmpireAtWar.Services.Tutorial;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>
    /// Coach card above a UI spotlight, and a marker for world targets (clamped to the screen edge).
    /// </summary>
    public sealed class HintUi : BaseUi
    {
        private const float SLIDE_SECONDS = 0.2f;
        private const float DONE_SECONDS = 0.5f;
        private const float SLIDE_DISTANCE = 40f;
        private const float SPOTLIGHT_PADDING = 6f;

        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform card;
        [SerializeField] private CanvasGroup cardGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private GameObject doneMark;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Button turnOffButton;
        [SerializeField] private HintSpotlightView spotlight;
        [SerializeField] private HintWorldMarkerView worldMarker;
        [SerializeField] private HintDragSelectDemoView dragSelectDemo;
        [SerializeField] private HintElementPreviewView elementPreview;
        [SerializeField] private RectTransform inputRow;
        [SerializeField] private HintInputGlyph inputTemplate;

        private readonly List<HintInputGlyph> _inputGlyphs = new List<HintInputGlyph>();
        private IHintPresenter _presenter;
        private Vector2 _cardRestPosition;
        private float _slide;
        private float _doneTimer;
        private bool _isOpen;
        private bool _isInitialized;

        public void AssignPresenter(IHintPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_isInitialized) return;
            _cardRestPosition = card.anchoredPosition;
            dismissButton.onClick.AddListener(_presenter.Dismiss);
            turnOffButton.onClick.AddListener(_presenter.TurnOffHints);
            SetCardVisible(false);
            SetSpotlight(false, default);
            SetWorldMarker(false, default);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            dismissButton.onClick.RemoveListener(_presenter.Dismiss);
            turnOffButton.onClick.RemoveListener(_presenter.TurnOffHints);
            _isInitialized = false;
        }

        public void OpenHint(string title, string body, string progress)
        {
            titleText.text = title;
            bodyText.text = body;
            progressText.text = progress;
            progressText.gameObject.SetActive(progress.Length > 0);
            doneMark.SetActive(false);
            _doneTimer = 0f;
            _slide = 0f;
            _isOpen = true;
            SetCardVisible(true);
            ApplySlide();
        }

        public void SetBody(string body) => bodyText.text = body;

        public void SetInputs(IReadOnlyList<HintInputPrompt> prompts)
        {
            for (int i = 0; i < prompts.Count; i++)
            {
                if (i == _inputGlyphs.Count)
                    _inputGlyphs.Add(Instantiate(inputTemplate, inputRow));
                _inputGlyphs[i].Render(prompts[i]);
                _inputGlyphs[i].gameObject.SetActive(true);
            }
            for (int i = prompts.Count; i < _inputGlyphs.Count; i++)
                _inputGlyphs[i].gameObject.SetActive(false);
            inputRow.gameObject.SetActive(prompts.Count > 0);
        }

        /// <param name="completed">True shows a short done mark before the card leaves.</param>
        public void CloseHint(bool completed)
        {
            _isOpen = false;
            SetSpotlight(false, default);
            SetWorldMarker(false, default);
            dismissButton.interactable = false;
            turnOffButton.interactable = false;
            if (completed)
            {
                doneMark.SetActive(true);
                _doneTimer = DONE_SECONDS;
            }
        }

        /// <param name="screenRect">Element bounds in screen pixels.</param>
        public void SetSpotlight(bool isVisible, Rect screenRect, bool blockPointerOutside = false)
        {
            if (!isVisible || !screenRect.Overlaps(new Rect(0f, 0f, Screen.width, Screen.height)))
            {
                spotlight.Hide();
                return;
            }
            Vector2 min = ToLocal(screenRect.min) - Vector2.one * SPOTLIGHT_PADDING;
            Vector2 max = ToLocal(screenRect.max) + Vector2.one * SPOTLIGHT_PADDING;
            // Tooltip views can be created after this view; keep the coach overlay above them.
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1)
                transform.SetAsLastSibling();
            spotlight.Show(Rect.MinMaxRect(min.x, min.y, max.x, max.y), blockPointerOutside);
        }

        /// <param name="screenPoint">Projected world point; z below zero means behind the camera.</param>
        public void SetWorldMarker(bool isVisible, Vector3 screenPoint)
        {
            if (isVisible) worldMarker.Show(screenPoint, root);
            else worldMarker.Hide();
        }

        public void SetDemo(HintDemo demo)
        {
            if (demo == HintDemo.DragSelect) dragSelectDemo.Play();
            else dragSelectDemo.Stop();
        }

        /// <param name="source">HUD element to copy onto the card; null hides the preview.</param>
        public void SetPreview(RectTransform source)
        {
            if (source != null) elementPreview.Show(source);
            else elementPreview.Hide();
        }

        private void Update()
        {
            if (_isOpen)
            {
                if (_slide < 1f)
                {
                    _slide = Mathf.Min(1f, _slide + Time.unscaledDeltaTime / SLIDE_SECONDS);
                    ApplySlide();
                }
                return;
            }
            if (_doneTimer > 0f)
            {
                _doneTimer -= Time.unscaledDeltaTime;
                return;
            }
            if (_slide > 0f)
            {
                _slide = Mathf.Max(0f, _slide - Time.unscaledDeltaTime / SLIDE_SECONDS);
                ApplySlide();
                if (_slide <= 0f) SetCardVisible(false);
            }
        }

        private void ApplySlide()
        {
            float eased = 1f - (1f - _slide) * (1f - _slide);
            card.anchoredPosition = _cardRestPosition + Vector2.left * (SLIDE_DISTANCE * (1f - eased));
            cardGroup.alpha = eased;
        }

        private void SetCardVisible(bool isVisible)
        {
            card.gameObject.SetActive(isVisible);
            dismissButton.interactable = isVisible;
            turnOffButton.interactable = isVisible;
        }

        private Vector2 ToLocal(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, null, out Vector2 local);
            return local;
        }

        private void OnDestroy() => Dispose();
    }
}
