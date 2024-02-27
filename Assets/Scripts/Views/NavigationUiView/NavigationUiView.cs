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
        [SerializeField] private Image attackImage;
        [SerializeField] private Canvas canvas;

        private Sequence fadeSequence;
        private Sequence fadeAttackSequence;
        
        protected override void OnInitialize()
        {
            Model.OnTapPositionChanged += UpdateTapPosition;
            Model.OnAttackPositionChanged += UpdateAttackPosition;
            tapImage.DOFade(0, 0);
            attackImage.DOFade(0, 0);
        }
        

        protected override void OnDispose()
        {
            Model.OnTapPositionChanged -= UpdateTapPosition;
            Model.OnAttackPositionChanged -= UpdateAttackPosition;
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
        
        private void UpdateAttackPosition(Vector3 screenPoint)
        {
            attackImage.rectTransform.position = screenPoint;

            fadeAttackSequence.KillIfExist();
            fadeAttackSequence = DOTween.Sequence();
            
            fadeAttackSequence.Append(attackImage.DOFade(1, 0f));
            fadeAttackSequence.Append(attackImage.rectTransform.DOScale(0.3f, 0f));
            
            fadeAttackSequence.Append(attackImage.rectTransform.DOScale(1, 1f));
            fadeAttackSequence.Join(attackImage.DOFade(0f, 2f));
        }
    }
}