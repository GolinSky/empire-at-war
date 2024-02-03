using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.Battle;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public class WeaponComponent : BaseComponent<WeaponModel>, IInitializable, ILateDisposable
    {
        private readonly IBattleService battleService;
        private readonly ISelectionModelObserver selectionModelObserver;

        public WeaponComponent(IModel model, IBattleService battleService) : base(model)
        {
            this.battleService = battleService;
            selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
        }

        public void Initialize()
        {
            battleService.OnTargetAdded += OnTargetAdded;
        }

        public void LateDispose()
        {
            battleService.OnTargetAdded -= OnTargetAdded;
        }

        private void OnTargetAdded(IHealthComponent healthComponent)
        {
            if (selectionModelObserver.IsSelected)
            {
                healthComponent.ApplyDamage(Model.GetTotalDamage());
            }
        }
    }
}