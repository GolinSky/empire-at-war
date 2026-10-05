using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Ui.Base;
using Zenject;

namespace EmpireAtWar.Entities.Heroes
{
    public sealed class HeroUiController : IHeroPresenter, IInitializable, ITickable,
        ILateDisposable, IObserver<BattleState>
    {
        private readonly IUiService _uiService;
        private readonly IEntityLocator _entities;
        private readonly FactionsData _factions;
        private readonly ILocalPlayer _localPlayer;
        private readonly IVisionService _vision;
        private readonly ICameraService _camera;
        private readonly INotifier<BattleState> _battleState;
        private readonly List<IEntity> _heroes = new List<IEntity>();

        private IHeroUi _ui;
        private bool _isBattleActive;

        public HeroUiController(IUiService uiService, IEntityLocator entities, FactionsData factions,
            ILocalPlayer localPlayer, IVisionService vision, ICameraService camera,
            INotifier<BattleState> battleState)
        {
            _uiService = uiService;
            _entities = entities;
            _factions = factions;
            _localPlayer = localPlayer;
            _vision = vision;
            _camera = camera;
            _battleState = battleState;
        }

        public void Initialize()
        {
            _ui = (IHeroUi)_uiService.CreateUi(UiType.Hero);
            _ui.SetPresenter(this);
            _ui.Initialize();
            _entities.EntityAdded += AddHero;
            _entities.EntityRemoved += RemoveHero;
            foreach (IEntity entity in _entities.Entities)
                AddHero(entity);
            _battleState.AddObserver(this);
        }

        public void UpdateState(BattleState state)
        {
            _isBattleActive = state == BattleState.Running || state == BattleState.Paused;
            RefreshVisibility();
        }

        public void Tick()
        {
            for (int i = _heroes.Count - 1; i >= 0; i--)
            {
                IEntity hero = _heroes[i];
                if (hero.HealthModel.IsDestroyed)
                    RemoveHero(hero);
                else
                    _ui.SetFocusable(hero.Id, CanFocus(hero));
            }
        }

        public void FocusHero(long entityId)
        {
            if (!_isBattleActive || !_entities.TryGetEntity(entityId, out IEntity hero) ||
                !_heroes.Contains(hero) || hero.HealthModel.IsDestroyed || !CanFocus(hero))
                return;

            _camera.MoveTo(hero.GetFacade<IEntityTransformFacade>().Transform.position);
        }

        private void AddHero(IEntity entity)
        {
            if (!entity.TryGetFacade(out IUnitTypeFacade unit) || entity.HealthModel.IsDestroyed ||
                (!_localPlayer.IsFriendly(entity.Owner) && !_localPlayer.IsHostile(entity.Owner)))
                return;

            UnitTypeId type = unit.UnitTypeId;
            FactionData data = type.IsShip
                ? _factions.GetShipFactionData(type.ShipType)
                : _factions.GetSquadronFactionData(type.SquadronType);
            if (!data.IsHero)
                return;

            _heroes.Add(entity);
            _ui.AddHero(entity.Id, data.Icon, _localPlayer.IsFriendly(entity.Owner), CanFocus(entity));
            RefreshVisibility();
        }

        private void RemoveHero(IEntity entity)
        {
            if (!_heroes.Remove(entity))
                return;

            _ui.RemoveHero(entity.Id);
            RefreshVisibility();
        }

        private bool CanFocus(IEntity hero) => _localPlayer.IsFriendly(hero.Owner) ||
            _vision.IsVisible(_localPlayer.Id, hero.GetFacade<IEntityTransformFacade>().Transform.position);

        private void RefreshVisibility()
        {
            if (_isBattleActive && _heroes.Count > 0)
                _ui.Show();
            else
                _ui.Hide();
        }

        public void LateDispose()
        {
            _entities.EntityAdded -= AddHero;
            _entities.EntityRemoved -= RemoveHero;
            _battleState.RemoveObserver(this);
            _heroes.Clear();
            _ui.Dispose();
        }
    }
}
