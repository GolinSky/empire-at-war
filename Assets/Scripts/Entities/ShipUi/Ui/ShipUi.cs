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

        public void Initialize()
        {
            Model.OnSelectionChanged += HandleChangedSelection;
            disableSelectionButton.onClick.AddListener(CloseSelection);
        }

        public void LateDispose()
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
            shipIconImage.enabled = selectionType == SelectionType.Ship;
            if (selectionType == SelectionType.Ship)
            {
                shipIconImage.sprite = Model.ShipIcon;
            }
        }
    }
}