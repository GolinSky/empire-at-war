using EmpireAtWar.Mvc;
using UnityEngine;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public static class AssetRegistrations
    {
        public static void RegisterShared<T>(IContainerBuilder builder, IAssetService assets) where T : Object
        {
            builder.RegisterInstance(assets.Load<T>(typeof(T).Name)).AsImplementedInterfaces().AsSelf();
        }
    }
}
