using System;
using DG.Tweening;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.UnitOrderFeedback
{
    public sealed class UnitOrderFeedbackUi : BaseUi
    {
        private const float TARGET_FADE_IN = 0.09f;
        private const float TARGET_SETTLE = 0.5f;
        private const float TARGET_FADE_START = 1.05f;
        private const float TARGET_FADE_OUT = 0.5f;
        private const float TARGET_ICON_SCALE = 0.45f;
        private const float WAVE_DURATION = 0.68f;
        private const float ECHO_DELAY = 0.12f;

        [SerializeField] private RectTransform screenRect;
        [SerializeField] private RectTransform attackAnchor;
        [SerializeField] private RectTransform movementAnchor;
        [SerializeField] private RectTransform invalidTargetAnchor;
        [SerializeField] private RectTransform attackCursorAnchor;
        [SerializeField] private RectTransform actionCursorAnchor;
        [SerializeField] private Image actionCursorImage;
        [SerializeField] private Image attackImage;
        [SerializeField] private Sprite guardIcon;
        [SerializeField] private Sprite attackMoveIcon;
        [SerializeField] private Sprite waypointIcon;
        // Ordered by SuperWeaponType.
        [SerializeField] private Sprite[] superWeaponIcons;
        [SerializeField] private Image movementWave;
        [SerializeField] private Image movementEcho;
        [SerializeField] private Color formationColor = new Color(0.21960784f, 0.7411765f, 0.972549f, 1f);
        [SerializeField] private Color looseColor = new Color(0.2901961f, 0.87058824f, 0.5019608f, 1f);
        private Sequence _targetSequence;
        private Sequence _movementSequence;
        private Color _attackColor;
        private Vector3 _attackAnchorScale;

        private bool _isDisposed;

        public void Initialize()
        {
            _attackColor = attackImage.color;
            _attackAnchorScale = attackAnchor.localScale;
            attackAnchor.gameObject.SetActive(false);
            movementAnchor.gameObject.SetActive(false);
            invalidTargetAnchor.gameObject.SetActive(false);
            attackCursorAnchor.gameObject.SetActive(false);
            actionCursorAnchor.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            StopTargetOrder();
            StopMovement();
        }

        public Sprite GetSuperWeaponIcon(SuperWeaponType type) => superWeaponIcons[(int)type];

        /// <summary>Marks a target: <see cref="OrderCursor.Attack"/> shows the reticle, other actions their icon.</summary>
        /// <param name="actionIcon">Icon for <see cref="OrderCursor.AbilityTarget"/> and <see cref="OrderCursor.SuperWeapon"/>.</param>
        public void PlayTargetOrder(Vector2 screenPosition, OrderCursor action, Sprite actionIcon, Action completed)
        {
            StopTargetOrder();
            SetPosition(attackAnchor, screenPosition);
            Sprite icon = action == OrderCursor.Attack ? null : GetIcon(action, actionIcon);
            bool isIcon = icon != null;
            attackAnchor.localScale = isIcon ? Vector3.one : _attackAnchorScale;
            attackImage.overrideSprite = icon;
            SetColor(attackImage, GetMarkerColor(action), 0f);
            float scale = isIcon ? TARGET_ICON_SCALE : 1f;
            attackAnchor.gameObject.SetActive(true);
            attackImage.rectTransform.localScale = Vector3.one * (1.45f * scale);
            attackImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            _targetSequence = DOTween.Sequence()
                .Append(attackImage.DOFade(1f, TARGET_FADE_IN).SetEase(Ease.OutQuad))
                .Insert(0f, attackImage.rectTransform.DOScale(scale, TARGET_SETTLE).SetEase(Ease.OutBack))
                .Insert(0f, attackImage.rectTransform.DOLocalRotate(Vector3.zero, TARGET_SETTLE)
                    .SetEase(Ease.OutCubic))
                .Insert(TARGET_FADE_START, attackImage.DOFade(0f, TARGET_FADE_OUT).SetEase(Ease.InQuad))
                .Insert(TARGET_FADE_START, attackImage.rectTransform.DOScale(1.08f * scale, TARGET_FADE_OUT)
                    .SetEase(Ease.InQuad))
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _targetSequence = null;
                    attackAnchor.gameObject.SetActive(false);
                    completed();
                });
        }

        public void PlayMovement(Vector2 screenPosition, bool isLoose, Action completed)
        {
            StopMovement();
            SetPosition(movementAnchor, screenPosition);
            movementAnchor.gameObject.SetActive(true);
            Color waveColor = isLoose ? looseColor : formationColor;
            SetColor(movementWave, waveColor, 0f);
            SetColor(movementEcho, waveColor, 0f);
            movementWave.rectTransform.localScale = Vector3.one * 0.28f;
            movementEcho.rectTransform.localScale = Vector3.one * 0.28f;
            _movementSequence = DOTween.Sequence();
            AddWave(_movementSequence, movementWave, 0f, 0.9f);
            AddWave(_movementSequence, movementEcho, ECHO_DELAY, 0.5f);
            _movementSequence.SetUpdate(true).OnComplete(() =>
            {
                _movementSequence = null;
                movementAnchor.gameObject.SetActive(false);
                completed();
            });
        }

        public void SetTargetOrderPosition(Vector2 screenPosition) => SetPosition(attackAnchor, screenPosition);

        public void SetMovementPosition(Vector2 screenPosition) => SetPosition(movementAnchor, screenPosition);

        public void StopTargetOrder()
        {
            if (_targetSequence != null)
            {
                _targetSequence.Kill();
                _targetSequence = null;
            }
            attackAnchor.gameObject.SetActive(false);
        }

        /// <param name="actionIcon">Icon for <see cref="OrderCursor.AbilityTarget"/> and <see cref="OrderCursor.SuperWeapon"/>.</param>
        public void SetCursor(OrderCursor cursor, Vector2 screenPosition, Sprite actionIcon)
        {
            SetCursorAnchor(attackCursorAnchor, cursor == OrderCursor.Attack, screenPosition);
            SetCursorAnchor(invalidTargetAnchor, cursor == OrderCursor.Invalid, screenPosition);
            Sprite icon = cursor is OrderCursor.None or OrderCursor.Attack or OrderCursor.Invalid
                ? null : GetIcon(cursor, actionIcon);
            if (icon != null && actionCursorImage.sprite != icon) actionCursorImage.sprite = icon;
            SetCursorAnchor(actionCursorAnchor, icon != null, screenPosition);
        }

        private Sprite GetIcon(OrderCursor action, Sprite actionIcon) => action switch
        {
            OrderCursor.Guard => guardIcon,
            OrderCursor.AttackMove => attackMoveIcon,
            OrderCursor.Waypoint => waypointIcon,
            OrderCursor.AbilityTarget or OrderCursor.SuperWeapon => actionIcon,
            _ => null
        };

        // Friendly orders use the formation blue, hostile ones the attack colour; ability and orbital icons keep their own.
        private Color GetMarkerColor(OrderCursor action) => action switch
        {
            OrderCursor.Guard or OrderCursor.Waypoint => formationColor,
            OrderCursor.AbilityTarget or OrderCursor.SuperWeapon => Color.white,
            _ => _attackColor
        };

        private void OnDestroy() => Dispose();

        private void StopMovement()
        {
            if (_movementSequence != null)
            {
                _movementSequence.Kill();
                _movementSequence = null;
            }
            movementAnchor.gameObject.SetActive(false);
        }

        private void SetPosition(RectTransform anchor, Vector2 screenPosition)
        {
            // The owning DefaultCanvas is Screen Space Overlay.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screenRect, screenPosition, null, out Vector2 position);
            anchor.anchoredPosition = position;
        }

        private void SetCursorAnchor(RectTransform anchor, bool isShown, Vector2 screenPosition)
        {
            if (isShown) SetPosition(anchor, screenPosition);
            anchor.gameObject.SetActive(isShown);
        }

        private static void AddWave(Sequence sequence, Image image, float delay, float opacity)
        {
            sequence.Insert(delay, image.rectTransform.DOScale(1.35f, WAVE_DURATION).SetEase(Ease.OutQuad));
            sequence.Insert(delay, image.DOFade(opacity, 0.08f).SetEase(Ease.OutQuad));
            sequence.Insert(delay + 0.12f, image.DOFade(0f, WAVE_DURATION - 0.12f).SetEase(Ease.InQuad));
        }

        private static void SetColor(Image image, Color color, float alpha)
        {
            color.a = alpha;
            image.color = color;
        }
    }
}
