using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.BattleNotifications
{
    /// <summary>
    /// One notification card. Opening grows its slot while the body slides down and fades in; closing fades the
    /// body out first, then collapses the slot, so the cards below move up smoothly instead of jumping.
    /// </summary>
    public sealed class BattleNotificationCardView : MonoBehaviour
    {
        private const float FADE_SECONDS = 0.18f;
        private const float COLLAPSE_SECONDS = 0.15f;
        private const float SLIDE_DISTANCE = 16f;
        // The gap to the next card is part of the slot, so it collapses with the card.
        private const float CARD_GAP = 6f;
        private const float ICON_TEXT_INSET = 56f;

        [SerializeField] private LayoutElement slot;
        [SerializeField] private RectTransform body;
        [SerializeField] private CanvasGroup bodyGroup;
        [SerializeField] private Image accent;
        [SerializeField] private Image eventIcon;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button dismissButton;

        private Action<int> _dismiss;
        private float _height;
        private float _textInset;
        private float _fade;
        private float _size;
        private bool _isClosing;

        public int NotificationId { get; private set; }
        public bool IsOpen => gameObject.activeSelf && !_isClosing;
        public bool IsFree => !gameObject.activeSelf;

        public void Initialize(Action<int> dismiss)
        {
            _dismiss = dismiss;
            _height = body.rect.height + CARD_GAP;
            _textInset = messageText.rectTransform.offsetMin.x;
            dismissButton.onClick.AddListener(HandleDismissClicked);
        }

        public void Dispose() => dismissButton.onClick.RemoveListener(HandleDismissClicked);

        public void Open(int notificationId, string message, Color accentColor, Sprite icon)
        {
            NotificationId = notificationId;
            SetContent(message, icon);
            accent.color = accentColor;
            _fade = 0f;
            _size = 0f;
            _isClosing = false;
            dismissButton.interactable = true;
            bodyGroup.blocksRaycasts = true;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Apply();
        }

        public void SetContent(string message, Sprite icon)
        {
            messageText.text = message;
            eventIcon.sprite = icon;
            eventIcon.gameObject.SetActive(icon != null);
            Vector2 offset = messageText.rectTransform.offsetMin;
            offset.x = icon != null ? ICON_TEXT_INSET : _textInset;
            messageText.rectTransform.offsetMin = offset;
        }

        public void Close()
        {
            _isClosing = true;
            dismissButton.interactable = false;
            bodyGroup.blocksRaycasts = false;
        }

        private void HandleDismissClicked() => _dismiss(NotificationId);

        private void Update()
        {
            float fadeStep = Time.unscaledDeltaTime / FADE_SECONDS;
            float sizeStep = Time.unscaledDeltaTime / COLLAPSE_SECONDS;
            if (_isClosing)
            {
                if (_fade > 0f) _fade = Mathf.Max(0f, _fade - fadeStep);
                else _size = Mathf.Max(0f, _size - sizeStep);
            }
            else
            {
                _size = Mathf.Min(1f, _size + sizeStep);
                _fade = Mathf.Min(1f, _fade + fadeStep);
            }
            Apply();
            if (_isClosing && _size <= 0f) gameObject.SetActive(false);
        }

        private void Apply()
        {
            float fade = Ease(_fade);
            slot.preferredHeight = _height * Ease(_size);
            body.anchoredPosition = Vector2.up * (SLIDE_DISTANCE * (1f - fade));
            bodyGroup.alpha = fade;
        }

        private static float Ease(float value) => 1f - (1f - value) * (1f - value);
    }
}
