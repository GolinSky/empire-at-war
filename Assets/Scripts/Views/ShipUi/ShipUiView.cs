using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views
{
    public class ShipUiView: View<IShipUiModelObserver, IShipUiCommand>
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image shipIconImage;
        [SerializeField] private Button disableSelectionButton;
        [SerializeField] private Transform rootTransform;
        
        protected override void OnInitialize()
        {
            Model.OnSelectionChanged += HandleChangedSelection;
            disableSelectionButton.onClick.AddListener(CloseSelection);
        }
        
        protected override void OnDispose()
        {
            Model.OnSelectionChanged -= HandleChangedSelection;
            disableSelectionButton.onClick.RemoveListener(CloseSelection);
        }
        
        private void CloseSelection()
        {
            Command.CloseSelection();
        }
        
        private void HandleChangedSelection(SelectionType selectionType)
        {
            canvas.enabled = selectionType == SelectionType.Ship;
            if (selectionType == SelectionType.Ship)
            {
                ShipInfoUi shipInfoUi = Model.ShipInfoUi;
                shipInfoUi.SetParent(rootTransform);
                
                //shipIconImage.sprite = Model.ShipIcon;
            }
        }
    }
}