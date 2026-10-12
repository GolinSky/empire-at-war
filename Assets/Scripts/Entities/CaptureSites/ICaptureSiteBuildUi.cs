using EmpireAtWar.Components.Ui.Tooltip;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public interface ICaptureSiteBuildUi : ITooltipHoverView
    {
        void SetData(CaptureSiteData data);
        void SetPresenter(ICaptureSiteBuildPresenter presenter);
        void Initialize();
        void Dispose();
        void Show();
        void Hide();
        void SetPosition(Vector2 screenPosition);
        void SetAvailable(SiteFacilityType type, bool available);
    }
}
