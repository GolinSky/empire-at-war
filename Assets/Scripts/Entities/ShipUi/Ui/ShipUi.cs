using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views
{
    public class ShipUi: BaseUi<IShipUiModelObserver, IShipUiCommand>, IInitializable, ILateDisposable
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image shipIconImage;
        [SerializeField] private Button disableSelectionButton;
        [SerializeField] private Transform rootTransform;
        [SerializeField] private Canvas moveToPositionCanvas;
        [SerializeField] private Image moveToPositionImage;
        [SerializeField] private Button moveToPositionButton;
        
        public void Initialize()
        {
            Model.OnSelectionChanged += HandleChangedSelection;
            Model.OnTapPositionChanged += UpdateTapPosition;
            Model.OnSkipGoToPositionUi += CloseGoToPositionUi;
            disableSelectionButton.onClick.AddListener(CloseSelection);
            moveToPositionButton.onClick.AddListener(Command.MoveToPosition);
        }
    
        public void LateDispose()
        {
            Model.OnSelectionChanged -= HandleChangedSelection;
            Model.OnTapPositionChanged -= UpdateTapPosition;
            Model.OnSkipGoToPositionUi -= CloseGoToPositionUi;
            disableSelectionButton.onClick.RemoveListener(CloseSelection);
            moveToPositionButton.onClick.RemoveListener(Command.MoveToPosition);
        }
        
        private void CloseGoToPositionUi()
        {
            moveToPositionCanvas.enabled = false;
        }
        
        private void CloseSelection()
        {
            Command.CloseSelection();
        }
        
        private void HandleChangedSelection(bool hasMovableSelection)
        {
            canvas.enabled = hasMovableSelection;
            shipIconImage.enabled = hasMovableSelection && Model.ShipIcon != null;
            if (shipIconImage.enabled)
            {
                shipIconImage.sprite = Model.ShipIcon;
            }
        }
        
        private void UpdateTapPosition(Vector2 tapPosition)
        {
            moveToPositionCanvas.enabled = true;
            
            moveToPositionImage.GetComponent<RectTransform>().position = tapPosition;

            // _fadeSequence.KillIfExist();
            // _fadeSequence = DOTween.Sequence();
            //
            // _fadeSequence.Append(goToImage.DOFade(1, 0f));
            // _fadeSequence.Append(goToImage.rectTransform.DOScale(0.3f, 0f));
            //
            // _fadeSequence.Append(goToImage.rectTransform.DOScale(1, 1f));
            // _fadeSequence.Join(goToImage.DOFade(0f, 2f));
        }
    }
}
