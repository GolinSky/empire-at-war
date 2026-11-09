using System.Collections.Generic;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>
    /// Coach card at a fixed screen edge, a pulsing frame on the highlighted HUD element, and a marker for world
    /// targets (clamped to the screen edge when off screen). Only the card's buttons take clicks.
    /// </summary>
    public sealed class HintUi : BaseUi
    {
        private const float SLIDE_SECONDS = 0.2f;
        private const float DONE_SECONDS = 0.5f;
        private const float SLIDE_DISTANCE = 40f;
        private const float PULSE_SPEED = 5f;
        private const float FRAME_PADDING = 6f;
        private const float EDGE_MARGIN = 48f;

        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform card;
        [SerializeField] private CanvasGroup cardGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private GameObject doneMark;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Button turnOffButton;
        [SerializeField] private RectTransform anchorFrame;
        [SerializeField] private CanvasGroup anchorFrameGroup;
        [SerializeField] private RectTransform worldMarker;
        [SerializeField] private CanvasGroup worldMarkerGroup;
        [SerializeField] private RectTransform edgeArrow;
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
            SetAnchorFrame(false, default);
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
            SetAnchorFrame(false, default);
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
        public void SetAnchorFrame(bool isVisible, Rect screenRect)
        {
            anchorFrame.gameObject.SetActive(isVisible);
            if (!isVisible) return;
            Vector2 min = ToLocal(screenRect.min) - Vector2.one * FRAME_PADDING;
            Vector2 max = ToLocal(screenRect.max) + Vector2.one * FRAME_PADDING;
            anchorFrame.anchoredPosition = (min + max) * 0.5f;
            anchorFrame.sizeDelta = max - min;
        }

        /// <param name="screenPoint">Projected world point; z below zero means behind the camera.</param>
        public void SetWorldMarker(bool isVisible, Vector3 screenPoint)
        {
            worldMarker.gameObject.SetActive(isVisible);
            if (!isVisible) return;
            Vector2 point = screenPoint;
            if (screenPoint.z < 0f) point = new Vector2(Screen.width, Screen.height) - point;
            Rect inner = new Rect(EDGE_MARGIN, EDGE_MARGIN, Screen.width - EDGE_MARGIN * 2f,
                Screen.height - EDGE_MARGIN * 2f);
            bool isOnScreen = screenPoint.z >= 0f && inner.Contains(point);
            Vector2 clamped = new Vector2(Mathf.Clamp(point.x, inner.xMin, inner.xMax),
                Mathf.Clamp(point.y, inner.yMin, inner.yMax));
            worldMarker.anchoredPosition = ToLocal(clamped);
            edgeArrow.gameObject.SetActive(!isOnScreen);
            if (isOnScreen) return;
            Vector2 direction = point - new Vector2(Screen.width, Screen.height) * 0.5f;
            edgeArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void Update()
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * PULSE_SPEED);
            anchorFrameGroup.alpha = pulse;
            worldMarkerGroup.alpha = pulse;
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
