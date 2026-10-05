using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Factions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Presenters.Factions
{
    public class FactionUiController : UiController, IFactionPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute, ITickable, IObserver<BattleState>
    {
        private readonly IFactionService _factionService;
        private readonly IPlayerFactionModelObserver _model;
        private readonly IFactionResearchModelObserver _research;
        private readonly IUnitRequestFactory _unitRequestFactory;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private IFactionUi _ui;
        private readonly EmpireAtWar.Models.Economy.IEconomyModelObserver _economy;
        private readonly EmpireAtWar.Views.Game.ICoreGameHudStatus _hud;
        private readonly INotifier<BattleState> _battleState;

        private readonly FactionRoster _roster;
        private readonly TooltipRequests _tooltips;
        private readonly EmpireAtWar.Models.Reinforcement.ReinforcementModel _reinforcements;
        private TooltipHoverSubscription _tooltipHover;

        private bool _isTooltipActive;
        private bool _isBattleEnded;

        public FactionUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IFactionService factionService,
            IPlayerFactionModelObserver model,
            IFactionResearchModelObserver research,
            IUnitRequestFactory unitRequestFactory,
            ISkirmishRouteNavigation routeNavigation,
            ITooltipService tooltipService,
            EmpireAtWar.Models.Economy.IEconomyModelObserver economy,
            EmpireAtWar.Views.Game.ICoreGameHudStatus hud,
            INotifier<BattleState> battleState,
            FactionRoster roster,
            EmpireAtWar.Models.Reinforcement.ReinforcementModel reinforcements) : base(uiService, cancelRouter)
        {
            _factionService = factionService;
            _model = model;
            _research = research;
            _roster = roster;
            _unitRequestFactory = unitRequestFactory;
            _routeNavigation = routeNavigation;
            _tooltips = new TooltipRequests(tooltipService);
            _economy = economy;
            _reinforcements = reinforcements;
            _hud = hud;
            _battleState = battleState;
        }

        public void Initialize()
        {
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.Content,
                this);
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.Content,
                this);

            if (_ui != null)
            {
                _tooltipHover.Dispose();
                _ui.Dispose();
            }
        }

        public void TryPurchaseUnit(UnitRequest unitRequest)
        {
            _factionService.TryPurchaseUnit(unitRequest);
        }

        public bool IsUnitAvailable(FactionData data) => _model.CurrentLevel >= data.AvailableLevel;

        public bool IsUnitLimitReached(UnitRequest request) =>
            request is ShipUnitRequest ship && ship.Key == ShipType.Resolute &&
            _model.IsResoluteReserved;

        public void UpdateState(BattleState state)
        {
            _isBattleEnded = state == BattleState.Ended;
        }

        public void Tick()
        {
            _hud.SetProductionStatus(_model.FactionType.ToString(), _model.CurrentLevel);
            if (_ui != null) _ui.RefreshAvailability(_economy.Money, _isBattleEnded);
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            _isTooltipActive = isActive;
            if (!isActive) _tooltips.HideAll();
            if (_ui == null)
            {
                BaseUi ui = UiService.CreateUi(UiType.Faction, parentTransform);
                _ui = ui as IFactionUi
                    ?? throw new InvalidOperationException(
                        "The faction prefab does not implement IFactionUi.");

                _ui.SetParent(parentTransform);
                _ui.SetModel(_model);
                _ui.SetResearch(_research);
                _ui.SetPresenter(this);
                _ui.SetData(_roster);
                _ui.SetUnitRequestFactory(_unitRequestFactory);
                _ui.Initialize();
                _tooltipHover = new TooltipHoverSubscription(
                    ((ITooltipHoverView)_ui).TooltipHover,
                    HandleTooltipHover, _tooltips);
            }
            else
            {
                _ui.SetParent(parentTransform);
            }

            if (isActive)
            {
                _ui.Show();
            }
            else
            {
                _ui.Hide();
            }
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source)
        {
            UnitRequest request = (UnitRequest)key;
            _tooltips.Show(source, request.Id, anchor,
                () => _isTooltipActive && _model.SelectionType == EmpireAtWar.Services.Selection.SelectionType.Base,
                () => BuildTooltip(request));
        }

        private TooltipContent BuildTooltip(UnitRequest request)
        {
            FactionData data = request.FactionData;
            var requirements = new System.Collections.Generic.List<TooltipRequirement>();
            requirements.Add(new TooltipRequirement(
                $"Station Level {data.AvailableLevel}", _model.CurrentLevel >= data.AvailableLevel));
            if (_economy.Money < data.Price)
                requirements.Add(new TooltipRequirement(
                    $"Missing {data.Price - _economy.Money:0} credits", false));
            string status = data.UnitCapacity > _reinforcements.CapacityLeft
                ? "Population limit reached: deployment unavailable" : "Click to add to production";
            if (IsUnitLimitReached(request))
            {
                requirements.Add(new TooltipRequirement("Only one Resolute at a time", false));
                status = "Resolute is already queued, in reserve or deployed.";
            }
            var stats = new System.Collections.Generic.List<TooltipStat>
            {
                new TooltipStat(label: "Cost (credits)", current: data.Price),
                new TooltipStat(label: "Build time (s)", current: data.BuildTime),
                new TooltipStat(label: "Population on deployment", current: data.UnitCapacity)
            };
            if (request is LevelUnitRequest)
            {
                stats.Add(new TooltipStat(label: "Current station level", current: _model.CurrentLevel));
                stats.Add(new TooltipStat(label: "Next station level", current: _model.CurrentLevel + 1));
                status = "Upgrade the station to unlock higher-level roster cards.";
            }
            if (request is ResearchUnitRequest research && _research.TryGetNextTier(research.Key, out ResearchTierData tier))
            {
                foreach (ResearchEffect effect in tier.Effects)
                    stats.Add(new TooltipStat(
                        label: effect.Stat + " · " + (effect.Stat == ResearchStat.Income ? "Faction income" : string.Join(", ", effect.ShipClasses)),
                        current: effect.Multiplier, format: "0.##'×'"));
                status = "Research effects replace the previous tier of this line.";
            }
            foreach (ProductionQueueSnapshot queue in _model.GetProductionQueueSnapshots())
                if (queue.UnitRequest.Id == request.Id)
                {
                    stats.Add(new TooltipStat(label: "Queued", current: queue.Count, max: data.MaxCount));
                    stats.Add(new TooltipStat(label: "Next completion (s)", current: queue.RemainingBuildTime));
                    if (request is ResearchUnitRequest) status = "Already researching";
                }
            if (data.UnitCapacity > _reinforcements.CapacityLeft)
                requirements.Add(new TooltipRequirement("Population capacity for deployment", false));
            return UnitTooltipContent.Build(data, stats, requirements, status);
        }
    }
}
