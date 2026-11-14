using EmpireAtWar.Entities.CoreGameUi;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Mvc;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.Stations;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public class FactionUiController : UiController, IFactionPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute, ITickable, IObserver<BattleState>
    {
        private readonly IPlayerCommandSink _commands;
        private readonly PlayerSlot _owner;
        private readonly IInterfaceSfxService _interfaceAudio;
        private readonly IBattleNotifications _notifications;
        private readonly IPlayerFactionModelObserver _model;
        private readonly IFactionResearchModelObserver _research;
        private readonly IUnitRequestFactory _unitRequestFactory;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private FactionUi _ui;
        private readonly IUiPlacement _placement;
        private readonly IEconomyModelObserver _economy;
        private readonly ICoreGameHudStatus _hud;
        private readonly INotifier<BattleState> _battleState;

        private readonly FactionRoster _roster;
        private readonly IAssetService _assets;
        private readonly ShipsData _shipsData;
        private readonly TooltipRequests _tooltips;
        private readonly ReinforcementModel _reinforcements;
        private readonly SecondStationLevelModel _secondStation;
        private readonly StationLevelData _stationLevels;
        private readonly IProductionAccess _productionAccess;
        private TooltipHoverSubscription _tooltipHover;

        private bool _isTooltipActive;
        private bool _isBattleEnded;

        private bool _isDisposed;

        public FactionUiController(
            IUiService uiService,
            IUiPlacement placement,
            IUiCancelRouter cancelRouter,
            IPlayerCommandSink commands,
            PlayerSlot owner,
            IPlayerFactionModelObserver model,
            IFactionResearchModelObserver research,
            IUnitRequestFactory unitRequestFactory,
            ISkirmishRouteNavigation routeNavigation,
            ITooltipService tooltipService,
            IEconomyModelObserver economy,
            ICoreGameHudStatus hud,
            INotifier<BattleState> battleState,
            FactionRoster roster,
            ReinforcementModel reinforcements,
            IAssetService assets,
            ShipsData shipsData, IInterfaceSfxService interfaceAudio,
            IBattleNotifications notifications,
            SecondStationLevelModel secondStation,
            StationLevelData stationLevels,
            IProductionAccess productionAccess) : base(uiService, cancelRouter)
        {
            _productionAccess = productionAccess;
            _secondStation = secondStation;
            _stationLevels = stationLevels;
            _interfaceAudio = interfaceAudio;
            _notifications = notifications;
            _placement = placement;
            _assets = assets;
            _shipsData = shipsData;
            _commands = commands;
            _owner = owner;
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
            if (_isDisposed) return;
            _isDisposed = true;
            _battleState.RemoveObserver(this);
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.Content,
                this);

            if (_tooltipHover != null) _tooltipHover.Dispose();
            if (_ui != null)
            {
                _ui.Hide();
                _ui.Dispose();
            }
        }

        public void TryPurchaseUnit(UnitRequest unitRequest)
        {
            if (_economy.Money < unitRequest.FactionData.Price)
            {
                _interfaceAudio.PlayRejected();
                _notifications.Post(BattleNotificationPriority.Feedback,
                    "Insufficient credits", "credits");
                return;
            }
            _commands.Submit(PurchaseUnitCommand.From(_owner.Id, unitRequest));
        }

        // Units unlock by the higher of the two station levels; the queue, research and limits are shared.
        private int UnlockedLevel =>
            Math.Max(_model.CurrentLevel, _secondStation.IsBuilt ? _secondStation.CurrentLevel : 1);

        public bool IsUnitAvailable(FactionData data) => UnlockedLevel >= data.AvailableLevel;

        public bool IsUnlockedAtCurrentLevel(FactionData data) => UnlockedLevel == data.AvailableLevel;

        public bool IsCarrier(UnitRequest request) => request is ShipUnitRequest ship &&
            _assets.Load<ShipData>(_shipsData.GetShipDataPath(ship.Key)).HangarBays.Count > 0;

        public bool IsScout(UnitRequest request) => request is SquadronUnitRequest ||
            request is ShipUnitRequest ship &&
            _assets.Load<ShipData>(_shipsData.GetShipDataPath(ship.Key)).ShipClass is
                ShipClass.Fighter or ShipClass.Interceptor or ShipClass.Bomber;

        public int GetUnitSortOrder(UnitRequest request) => request switch
        {
            SquadronUnitRequest squadron => _assets.Load<SquadronData>(squadron.Key + nameof(SquadronData)).ShipClass switch
            {
                ShipClass.Fighter => 0,
                ShipClass.Interceptor => 1,
                ShipClass.Bomber => 2,
                ShipClass.HeroStrikecraft => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            },
            ShipUnitRequest ship => _assets.Load<ShipData>(_shipsData.GetShipDataPath(ship.Key)).ShipClass switch
            {
                ShipClass.Fighter => 4,
                ShipClass.Interceptor => 5,
                ShipClass.Bomber => 6,
                ShipClass.HeroStrikecraft => 7,
                ShipClass.Corvette => 8,
                ShipClass.Frigate => 9,
                ShipClass.Cruiser => 10,
                ShipClass.Capital => 11,
                ShipClass.HeavyCapital => 12,
                ShipClass.Structure => 13,
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            },
            DefendPlatformUnitRequest => 6,
            SecondStationUnitRequest => 6,
            MiningFacilityUnitRequest => 7,
            LevelUnitRequest => -1,
            SecondStationLevelUnitRequest => -1,
            SuperWeaponUnitRequest => 9,
            ResearchUnitRequest => 10,
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        // The main station's upgrade card hides while that station is gone; the second station has its own card.
        public bool IsUnitLimitReached(UnitRequest request) =>
            request is LevelUnitRequest && !_productionAccess.HasHostBase || _model.IsUnitLimitReached(request);

        public void UpdateState(BattleState state)
        {
            _isBattleEnded = state == BattleState.Ended;
        }

        public void Tick()
        {
            _hud.SetProductionStatus(_model.FactionType.ToString(), _model.CurrentLevel);
            if (_ui != null) _ui.RefreshAvailability(_economy.Money, _isBattleEnded);
        }

        public void Activate(bool isActive)
        {
            if (_isDisposed) return;
            _isTooltipActive = isActive;
            if (!isActive) _tooltips.HideAll();
            if (_ui == null)
            {
                _ui = CreateUi<FactionUi>();
                _ui.SetContentLayout(_placement.FactionLayout);
                _ui.SetModel(_model);
                _ui.SetResearch(_research);
                _ui.AssignPresenter(this);
                _ui.SetData(_roster);
                _ui.SetUnitRequestFactory(_unitRequestFactory);
                _ui.SetSecondStation(_secondStation, _stationLevels);
                _ui.Initialize();
                _tooltipHover = new TooltipHoverSubscription(
                    _ui.TooltipHover,
                    HandleTooltipHover, _tooltips);
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
                () => _isTooltipActive && _model.SelectionType == SelectionType.Base,
                () => BuildTooltip(request));
        }

        private TooltipContent BuildTooltip(UnitRequest request)
        {
            FactionData data = request.FactionData;
            var requirements = new System.Collections.Generic.List<TooltipRequirement>();
            requirements.Add(new TooltipRequirement(
                $"Station Level {data.AvailableLevel}", UnlockedLevel >= data.AvailableLevel));
            if (_economy.Money < data.Price)
                requirements.Add(new TooltipRequirement(
                    $"Missing {data.Price - _economy.Money:0} credits", false));
            string status = data.UnitCapacity > _reinforcements.CapacityLeft
                ? "Population limit reached: deployment unavailable" : "Click to add to production";
            if (IsUnitLimitReached(request))
            {
                requirements.Add(new TooltipRequirement("Purchase limit reached", false));
                status = $"{data.Name} has reached its purchase limit.";
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
            if (request is SecondStationLevelUnitRequest)
            {
                stats.Add(new TooltipStat(label: "Current second station level", current: _secondStation.CurrentLevel));
                stats.Add(new TooltipStat(label: "Next second station level", current: _secondStation.CurrentLevel + 1));
                status = "Upgrade the second station; units unlock by your highest station level.";
            }
            if (request is SecondStationUnitRequest)
                status = "A second station, built at level 1. It shares your build queue, research and limits.";
            if (request is ResearchUnitRequest research && _research.TryGetNextTier(research.Key, out ResearchTierData tier))
            {
                foreach (ResearchEffect effect in tier.Effects)
                    stats.Add(new TooltipStat(
                        label: effect.Stat + " · " + (effect.Stat == ResearchStat.Income ? "Mining income" :
                            effect.Stat == ResearchStat.StationIncome ? "Station income" : string.Join(", ", effect.ShipClasses)),
                        current: effect.Multiplier, format: "0.##'×'"));
                status = "Research effects replace the previous tier of this line.";
            }
            foreach (ProductionQueueSnapshot queue in _model.GetProductionQueueSnapshots())
                if (queue.UnitRequest.Id == request.Id)
                {
                    stats.Add(new TooltipStat(label: "Queued", current: queue.Count, max: data.MaxCount));
                    stats.Add(new TooltipStat(label: "Next completion (s)", current: queue.RemainingBuildTime, format: "0"));
                    if (request is ResearchUnitRequest) status = "Already researching";
                }
            if (data.UnitCapacity > _reinforcements.CapacityLeft)
                requirements.Add(new TooltipRequirement("Population capacity for deployment", false));
            return UnitTooltipContent.Build(data, stats, requirements, status);
        }
    }
}
