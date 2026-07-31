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
    }
    
    public class UiService : MonoBehaviour, IUiService
    {
        [SerializeField] private Canvas defaultCanvas;
        [SerializeField] private Canvas dynamicCanvas;
        [SerializeField] private Canvas popupCanvas;

        private UiFacade _uiFacade;

        public Transform DefaultCanvasTransform => GetCanvasTransform(defaultCanvas, nameof(defaultCanvas));
        public Transform DynamicCanvasTransform => GetCanvasTransform(dynamicCanvas, nameof(dynamicCanvas));
        public Transform PopupCanvasTransform => GetCanvasTransform(popupCanvas, nameof(popupCanvas));

        [Inject]
        public void Constructor(UiFacade uiFacade)
        {
            _uiFacade = uiFacade ?? throw new ArgumentNullException(nameof(uiFacade));
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

        private static Transform GetCanvasTransform(Canvas canvas, string fieldName)
        {
            if (canvas == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(UiService)} requires a bound {fieldName} reference.");
            }

            return canvas.transform;
        }
    }
}
