using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.BattleNotifications
{
    /// <summary>One notification card: slides down and fades in, pulses when merged, fades out when closed.</summary>
    public sealed class BattleNotificationCardView : MonoBehaviour
    {
        private const float SLIDE_SECONDS = 0.2f;
        private const float SLIDE_DISTANCE = 24f;
        private const float PULSE_SECONDS = 0.25f;
        private const float PULSE_SCALE = 0.06f;

        [SerializeField] private RectTransform body;
        [SerializeField] private CanvasGroup bodyGroup;
        [SerializeField] private Image accent;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button dismissButton;

        private Action<int> _dismiss;
        private float _slide;
        private float _pulse;
        private bool _isClosing;

        public int NotificationId { get; private set; }
        public bool IsOpen => gameObject.activeSelf && !_isClosing;
        public bool IsFree => !gameObject.activeSelf;

        public void Initialize(Action<int> dismiss)
        {
            _dismiss = dismiss;
            dismissButton.onClick.AddListener(HandleDismissClicked);
        }

        public void Dispose() => dismissButton.onClick.RemoveListener(HandleDismissClicked);

        public void Open(int notificationId, string message, Color accentColor)
        {
            NotificationId = notificationId;
            messageText.text = message;
            accent.color = accentColor;
            _slide = 0f;
            _pulse = 0f;
            _isClosing = false;
            dismissButton.interactable = true;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Apply();
        }

        public void SetMessage(string message)
        {
            messageText.text = message;
            _pulse = PULSE_SECONDS;
        }

        public void Close()
        {
            _isClosing = true;
            dismissButton.interactable = false;
        }

        private void HandleDismissClicked() => _dismiss(NotificationId);

        private void Update()
        {
            float step = Time.unscaledDeltaTime / SLIDE_SECONDS;
            _slide = _isClosing ? Mathf.Max(0f, _slide - step) : Mathf.Min(1f, _slide + step);
            _pulse = Mathf.Max(0f, _pulse - Time.unscaledDeltaTime);
            Apply();
            if (_isClosing && _slide <= 0f) gameObject.SetActive(false);
        }

        private void Apply()
        {
            float eased = 1f - (1f - _slide) * (1f - _slide);
            body.anchoredPosition = Vector2.up * (SLIDE_DISTANCE * (1f - eased));
            bodyGroup.alpha = eased;
            body.localScale = Vector3.one * (1f + PULSE_SCALE * (_pulse / PULSE_SECONDS));
        }
    }
}
