using DG.Tweening;
using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;
using Utilities.ScriptUtils.Dotween;
using Zenject;

namespace EmpireAtWar.Views.NavigationUiView
{
    public class NavigationUi:BaseUi<INavigationModelObserver>, IInitializable, ILateDisposable
    {
        [SerializeField] private Image tapImage;
        [SerializeField] private Image attackImage;
        [SerializeField] private Canvas canvas;

        private Sequence _fadeSequence;
        private Sequence _fadeAttackSequence;
        
        
        public void Initialize()
        {
            Model.OnTapPositionChanged += UpdateTapPosition;
            Model.OnAttackPositionChanged += UpdateAttackPosition;
            tapImage.DOFade(0, 0);
            attackImage.DOFade(0, 0);
        }

        public void LateDispose()
        {
            Model.OnTapPositionChanged -= UpdateTapPosition;
            Model.OnAttackPositionChanged -= UpdateAttackPosition;
        }
        
        private void UpdateTapPosition(Vector2 tapPosition)
        {
            tapImage.GetComponent<RectTransform>().position = tapPosition;

            _fadeSequence.KillIfExist();
            _fadeSequence = DOTween.Sequence();
            
            _fadeSequence.Append(tapImage.DOFade(1, 0f));
            _fadeSequence.Append(tapImage.rectTransform.DOScale(0.3f, 0f));
            
            _fadeSequence.Append(tapImage.rectTransform.DOScale(1, 1f));
            _fadeSequence.Join(tapImage.DOFade(0f, 2f));
        }
        
        private void UpdateAttackPosition(Vector3 screenPoint)
        {
            attackImage.rectTransform.position = screenPoint;

            _fadeAttackSequence.KillIfExist();
            _fadeAttackSequence = DOTween.Sequence();
            
            _fadeAttackSequence.Append(attackImage.DOFade(1, 0f));
            _fadeAttackSequence.Append(attackImage.rectTransform.DOScale(0.3f, 0f));
            
            _fadeAttackSequence.Append(attackImage.rectTransform.DOScale(1, 1f));
            _fadeAttackSequence.Join(attackImage.DOFade(0f, 2f));
        }
    }
}