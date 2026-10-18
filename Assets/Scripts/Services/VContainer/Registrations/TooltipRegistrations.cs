using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Tooltip;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public static class TooltipRegistrations
    {
        public static void Register(IContainerBuilder builder, IAssetService assets)
        {
            TooltipSettings tooltipSettings = assets.Load<TooltipSettings>(nameof(TooltipSettings));
            builder.RegisterInstance(tooltipSettings).AsImplementedInterfaces().AsSelf();
            AssetRegistrations.RegisterShared<TooltipIconData>(builder, assets);
            builder.RegisterInstance(tooltipSettings.Timing);
            builder.Register<TooltipClock>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<TooltipModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<TooltipService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<TooltipUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
