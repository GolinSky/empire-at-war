using EmpireAtWar.Mvc;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public static class UiRegistrations
    {
        public static void Register(IContainerBuilder builder, IAssetService assets, Transform owner)
        {
            UiService uiService = Object.Instantiate(assets.LoadComponent<UiService>(nameof(UiService)), owner);
            builder.RegisterComponent(uiService).AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(uiService.Placement);
            builder.Register<UiCancelRouter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
