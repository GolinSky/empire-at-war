using DG.Tweening;
using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.NavigationUiView
{
    public class NavigationUiView:View<INavigationModelObserver>
    {
        [SerializeField] private Image tapImage;
        [SerializeField] private Canvas canvas;

        private Sequence fadeSequence;
        
        protected override void OnInitialize()
        {
            Model.OnTapPositionChanged += UpdateTapPosition;
            tapImage.DOFade(0, 0);
        }

        protected override void OnDispose()
        {
            Model.OnTapPositionChanged -= UpdateTapPosition;
        }
        
        private void UpdateTapPosition(Vector2 tapPosition)
        {
            tapImage.GetComponent<RectTransform>().position = tapPosition;

            if (fadeSequence != null && fadeSequence.IsActive())
            {
                fadeSequence.Kill();
                tapImage.DOFade(0, 0);
            }
            fadeSequence = DOTween.Sequence();
            fadeSequence.Append(tapImage.DOFade(1, 2f));
            fadeSequence.Append(tapImage.DOFade(0, 3f));
        }
    }
}