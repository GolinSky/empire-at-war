using DG.Tweening;
using EmpireAtWar.Models.Navigation;
using EmpireAtWar.ScriptUtils.Dotween;
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

            fadeSequence.KillIfExist();
            fadeSequence = DOTween.Sequence();
            
            fadeSequence.Append(tapImage.DOFade(1, 0f));
            fadeSequence.Append(tapImage.rectTransform.DOScale(0.3f, 0f));
            
            fadeSequence.Append(tapImage.rectTransform.DOScale(1, 1f));
            fadeSequence.Join(tapImage.DOFade(0f, 2f));
        }
    }
}