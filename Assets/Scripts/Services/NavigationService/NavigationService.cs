using EmpireAtWar.Services.Input;
using UnityEngine;
using WorkShop.LightWeightFramework.Game;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.NavigationService
{
    public interface INavigationService : IService
    {
        void UpdateSelectable(ISelectable selectable);
    }

    public class NavigationService : Service, INavigationService, IInitializable, ILateDisposable
    {
        private ISelectable selectable;
        private readonly IInputService inputService;

        public NavigationService(IInputService inputService)
        {
            this.inputService = inputService;
        }

        public void Initialize()
        {
            inputService.OnInput += HandleInput;
            inputService.OnSelect += ResetSelectable;
        }

        public void LateDispose()
        {
            inputService.OnInput -= HandleInput;
            inputService.OnSelect -= ResetSelectable;
        }

        private void ResetSelectable()
        {
           // selectable?.SetActive(false);
          //  selectable = null;
        }
        private void HandleInput(Vector2 screenPosition)
        {
            if (selectable == null) return;

            if (selectable.CanMove)
            {
                selectable.MoveToPosition(screenPosition);
            }
        }

        public void UpdateSelectable(ISelectable selectable)
        {
            if (this.selectable == null)
            {
                this.selectable = selectable;
                selectable.SetActive(true);
                return;
            }
            if (this.selectable == selectable)
            {
                this.selectable.SetActive(false);
                this.selectable = null;
            }
            else
            {
             
            }
            return;
            if (this.selectable != null)
            {
                this.selectable.SetActive(false);
            }
     
        }

        protected override void OnInit(IGameObserver gameObserver)
        {
        }

        protected override void Release()
        {
        }
    }
}