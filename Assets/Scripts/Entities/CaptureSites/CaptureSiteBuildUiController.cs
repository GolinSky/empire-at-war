using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.CaptureSites
{
    [EmpireAtWar.Lifecycle.LifecycleOrder(LateDispose = EmpireAtWar.Lifecycle.LifecycleOrders.CAPTURE_SITE_BUILD_UI_LATE_DISPOSE)]
    public sealed class CaptureSiteBuildUiController : UiController, ICaptureSiteBuildPresenter, IInitializable, ITickable, ILateDisposable
    {
        private readonly ICaptureSiteBuildSelection _selection;
        private readonly CaptureSiteData _data;
        private readonly EmpireAtWar.Services.Audio.IInterfaceSfxService _interfaceAudio;
        private readonly TooltipRequests _tooltips;
        private CaptureSiteBuildUi _ui;
        private TooltipHoverSubscription _tooltipHover;

        public CaptureSiteBuildUiController(IUiService uiService, ICaptureSiteBuildSelection selection,
            CaptureSiteData data, ITooltipService tooltipService,
            EmpireAtWar.Services.Audio.IInterfaceSfxService interfaceAudio) : base(uiService, null)
        {
            _interfaceAudio = interfaceAudio;
            _selection = selection;
            _data = data;
            _tooltips = new TooltipRequests(tooltipService);
        }

        public void Initialize()
        {
            _ui = CreateUi<CaptureSiteBuildUi>();
            _ui.SetData(_data);
            _ui.AssignPresenter(this);
            _ui.Initialize();
            _tooltipHover = new TooltipHoverSubscription(_ui.TooltipHover, HandleTooltipHover, _tooltips);
            _selection.SelectionChanged += RefreshSelection;
            RefreshSelection();
        }

        public void LateDispose()
        {
            _selection.SelectionChanged -= RefreshSelection;
            _tooltipHover.Dispose();
            if (_ui != null)
            {
                _ui.Hide();
                _ui.Dispose();
            }
        }

        public void Build(SiteFacilityType type)
        {
            if (!_selection.CanAfford(_data.GetCost(type).Price))
            {
                _interfaceAudio.PlayRejected();
                return;
            }
            _selection.BuildSelected(type);
        }

        private void RefreshSelection()
        {
            _tooltips.HideAll();
            if (_selection.SelectedSite == null) _ui.Hide();
            else
            {
                _ui.SetPosition(_selection.SelectionScreenPosition);
                _ui.SetFacilities(_data.GetAllowedFacilities(_selection.SelectedSite.Kind));
                Tick();
                _ui.Show();
            }
        }

        public void Tick()
        {
            if (_selection.SelectedSite == null) return;
            // Follows the structure while the camera moves.
            _ui.SetPosition(_selection.SelectionScreenPosition);
            foreach (SiteFacilityType type in _data.GetAllowedFacilities(_selection.SelectedSite.Kind))
                _ui.SetAvailable(type, _selection.CanAfford(_data.GetCost(type).Price));
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
