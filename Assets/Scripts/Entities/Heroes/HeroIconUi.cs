using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Heroes
{
    public sealed class HeroIconUi : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Button focusButton;
        [SerializeField] private CanvasGroup canvasGroup;

        private IHeroPresenter _presenter;
        private long _entityId;
        private bool? _canFocus;

        public void Initialize(long entityId, Sprite icon, IHeroPresenter presenter, bool canFocus)
        {
            _entityId = entityId;
            _presenter = presenter;
            iconImage.sprite = icon;
            SetFocusable(canFocus);
            focusButton.onClick.AddListener(HandleClick);
        }

        public void SetFocusable(bool canFocus)
        {
            if (_canFocus == canFocus) return;
            _canFocus = canFocus;
            focusButton.interactable = canFocus;
            canvasGroup.alpha = canFocus ? 1f : 0.35f;
        }

        public void Dispose()
        {
            focusButton.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick() => _presenter.FocusHero(_entityId);

        private void OnDestroy() => Dispose();
    }
}
