using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using Zenject;

namespace EmpireAtWar.Services.CaptureSites
{
    public sealed class CaptureSiteBuildUiController : ICaptureSiteBuildPresenter, IInitializable, ITickable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly ICaptureSiteBuildSelection _selection;
        private readonly CaptureSiteData _data;
        private readonly TooltipRequests _tooltips;
        private ICaptureSiteBuildUi _ui;
        private TooltipHoverSubscription _tooltipHover;

        public CaptureSiteBuildUiController(IUiService uiService, ICaptureSiteBuildSelection selection,
            CaptureSiteData data, ITooltipService tooltipService)
        {
            _uiService = uiService;
            _selection = selection;
            _data = data;
            _tooltips = new TooltipRequests(tooltipService);
        }

        public void Initialize()
        {
            _ui = (ICaptureSiteBuildUi)_uiService.CreateUi(UiType.CaptureSiteBuild);
            _ui.SetData(_data);
            _ui.SetPresenter(this);
            _ui.Initialize();
            _tooltipHover = new TooltipHoverSubscription(_ui.TooltipHover, HandleTooltipHover, _tooltips);
            _selection.SelectionChanged += RefreshSelection;
            RefreshSelection();
        }

        public void LateDispose()
        {
            _selection.SelectionChanged -= RefreshSelection;
            _tooltipHover.Dispose();
            _ui.Hide();
            _ui.Dispose();
        }

        public void Build(SiteFacilityType type) => _selection.BuildSelected(type);

        private void RefreshSelection()
        {
            _tooltips.HideAll();
            if (_selection.SelectedSite == null) _ui.Hide();
            else
            {
                _ui.SetPosition(_selection.SelectionScreenPosition);
                Tick();
                _ui.Show();
            }
        }

        public void Tick()
        {
            if (_selection.SelectedSite == null) return;
            _ui.SetAvailable(SiteFacilityType.Mining, _selection.CanAfford(_data.GetCost(SiteFacilityType.Mining).Price));
            _ui.SetAvailable(SiteFacilityType.BattleAsteroid, _selection.CanAfford(_data.GetCost(SiteFacilityType.BattleAsteroid).Price));
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source)
        {
            SiteFacilityType type = (SiteFacilityType)key;
            _tooltips.Show(source, key, anchor, () => _selection.SelectedSite != null, () =>
            {
                SiteFacilityCost cost = _data.GetCost(type);
                return new TooltipContent(title: cost.Name,
                    description: $"Construct {cost.Name} at this friendly capture site.", stats: new[]
                    {
                        new TooltipStat(label: "Cost (credits)", current: cost.Price),
                        new TooltipStat(label: "Build time (s)", current: cost.BuildTime)
                    }, requirements: new[]
                    {
                        new TooltipRequirement("Friendly empty site", _selection.SelectedSite.CanPlayerBuild),
                        new TooltipRequirement($"{cost.Price:0} credits", _selection.CanAfford(cost.Price))
                    });
            });
        }
    }
}
