using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.BattleNotifications
{
    /// <summary>Top-center stack of dismissible notification cards. Only the cards take clicks.</summary>
    public sealed class BattleNotificationUi : BaseUi
    {
        [SerializeField] private RectTransform stack;
        [SerializeField] private BattleNotificationCardView cardTemplate;
        [SerializeField] private TooltipIconData icons;

        [Header("Placement")]
        [SerializeField] private float topOffset = 54f;
        [Tooltip("Survival keeps its wave and status rows at the top, so the cards start below them.")]
        [SerializeField] private float survivalTopOffset = 164f;

        [Header("Priority Accents")]
        [SerializeField] private Color outcomeColor = new Color32(0xF8, 0xFA, 0xFC, 0xFF);
        [SerializeField] private Color urgentColor = new Color32(0xEF, 0x44, 0x44, 0xFF);
        [SerializeField] private Color tacticalColor = new Color32(0xF5, 0x9E, 0x0B, 0xFF);
        [SerializeField] private Color feedbackColor = new Color32(0x94, 0xA3, 0xB8, 0xFF);
        [SerializeField] private Color progressColor = new Color32(0x38, 0xBD, 0xF8, 0xFF);

        private readonly List<BattleNotificationCardView> _cards = new List<BattleNotificationCardView>();
        private IBattleNotificationPresenter _presenter;
        private IBattleNotificationFeedObserver _feed;
        private bool _isInitialized;

        public void AssignPresenter(IBattleNotificationPresenter presenter) => _presenter = presenter;

        public void SetFeed(IBattleNotificationFeedObserver feed) => _feed = feed;

        public void SetSurvivalLayout(bool isSurvival) =>
            stack.anchoredPosition = new Vector2(0f, -(isSurvival ? survivalTopOffset : topOffset));

        public void Initialize()
        {
            if (_isInitialized) return;
            cardTemplate.gameObject.SetActive(false);
            _feed.Shown += HandleShown;
            _feed.Updated += HandleUpdated;
            _feed.Hidden += HandleHidden;
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            _feed.Shown -= HandleShown;
            _feed.Updated -= HandleUpdated;
            _feed.Hidden -= HandleHidden;
            foreach (BattleNotificationCardView card in _cards) card.Dispose();
            _isInitialized = false;
        }

        private void HandleShown(BattleNotification notification) =>
            FreeCard().Open(notification.Id, notification.Text, AccentColor(notification.Priority), Icon(notification), notification.OnActivate);

        private void HandleUpdated(BattleNotification notification)
        {
            if (TryGetOpenCard(notification.Id, out BattleNotificationCardView card))
                card.SetContent(notification.Text, Icon(notification));
        }

        private Sprite Icon(BattleNotification notification) =>
            string.IsNullOrEmpty(notification.IconKey) ? null : icons.Resolve(notification.IconKey);

        private void HandleHidden(BattleNotification notification)
        {
            if (TryGetOpenCard(notification.Id, out BattleNotificationCardView card)) card.Close();
        }

        private BattleNotificationCardView FreeCard()
        {
            foreach (BattleNotificationCardView card in _cards)
                if (card.IsFree) return card;
            BattleNotificationCardView created = Instantiate(cardTemplate, stack);
            created.Initialize(_presenter.Dismiss);
            _cards.Add(created);
            return created;
        }

        private bool TryGetOpenCard(int notificationId, out BattleNotificationCardView result)
        {
            foreach (BattleNotificationCardView card in _cards)
            {
                if (!card.IsOpen || card.NotificationId != notificationId) continue;
                result = card;
                return true;
            }
            result = null;
            return false;
        }

        private Color AccentColor(BattleNotificationPriority priority)
        {
            switch (priority)
            {
                case BattleNotificationPriority.Outcome: return outcomeColor;
                case BattleNotificationPriority.Urgent: return urgentColor;
                case BattleNotificationPriority.Tactical: return tacticalColor;
                case BattleNotificationPriority.Feedback: return feedbackColor;
                default: return progressColor;
            }
        }

        private void OnDestroy() => Dispose();
    }
}
