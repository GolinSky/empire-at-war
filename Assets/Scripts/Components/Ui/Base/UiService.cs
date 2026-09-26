using System;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ui.Base
{
    public interface IUiService
    {
        Transform DefaultCanvasTransform { get; }
        Transform DynamicCanvasTransform { get; }
        Transform PopupCanvasTransform { get; }

        BaseUi CreateUi(UiType uiType);
        BaseUi CreateUi(UiType uiType, Transform parent);
        void SetHudVisible(bool isVisible);
    }
    
    public class UiService : MonoBehaviour, IUiService
    {
        [SerializeField] private Canvas defaultCanvas;
        [SerializeField] private Canvas dynamicCanvas;
        [SerializeField] private Canvas popupCanvas;

        private UiFactory _uiFacade;

        public Transform DefaultCanvasTransform => defaultCanvas.transform;
        public Transform DynamicCanvasTransform => dynamicCanvas.transform;
        public Transform PopupCanvasTransform => popupCanvas.transform;

        [Inject]
        public void Constructor(UiFactory uiFacade)
        {
            _uiFacade = uiFacade;
        }
        
        public BaseUi CreateUi(UiType uiType)
        {
            return CreateUi(uiType, DynamicCanvasTransform);
        }

        public BaseUi CreateUi(UiType uiType, Transform parent)
        {
            if (_uiFacade == null)
            {
                throw new InvalidOperationException($"{nameof(UiService)} has not been initialized.");
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            return _uiFacade.Create(uiType, parent);
        }

        public void SetHudVisible(bool isVisible)
        {
            defaultCanvas.enabled = isVisible;
            dynamicCanvas.enabled = isVisible;
        }

    }
}
