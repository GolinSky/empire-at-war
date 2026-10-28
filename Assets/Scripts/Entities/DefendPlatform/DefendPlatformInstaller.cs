using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.SpawnBlocking;
using UnityEngine;
using VContainer;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Saves;

namespace EmpireAtWar
{
    public class DefendPlatformInstaller : DynamicEntityInstaller<DefendPlatform, DefendPlatformData>
    {
        private readonly DefendPlatformType _type;
        private readonly float _buildTime;

        public DefendPlatformInstaller(DefendPlatform view, DefendPlatformData data, ILocalPlayer localPlayer,
            PlayerId owner, long entityId, Vector3 position, DefendPlatformType type, float buildTime = 0f)
            : base(view, data, localPlayer, owner, entityId, position)
        {
            _type = type;
            _buildTime = buildTime;
        }

        protected override void InstallFeatures(IContainerBuilder builder)
        {
            bool isProductionBase = _type == DefendPlatformType.GolanIII || _type == DefendPlatformType.Empress;
            builder.RegisterInstance(Data);
            builder.RegisterInstance(_buildTime);
            EntityFeatureBindings.RegisterComponent(builder, View.Construction);
            builder.RegisterInstance(_type);
            builder.RegisterInstance(Data.ComponentData).AsSelf().As<IRadarData, IFogVisionData, ISpawnBlockerData>();
            builder.Register<DefendPlatformTooltipFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<StructureSaveFacade>(Lifetime.Singleton).AsImplementedInterfaces()
                .WithParameter("kind", StructureKind.DefendPlatform).WithParameter("type", (int)_type);
            if (isProductionBase) builder.Register<ProductionBaseFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            EntityFeatureBindings.RegisterSelection(builder, Bindings, SelectionType.DefendPlatform,
                isProductionBase ? SelectionType.Base : SelectionType.DefendPlatform);
            EntityFeatureBindings.RegisterHighlight(builder, Bindings);
            EntityFeatureBindings.RegisterCombatModifiers(builder);
            EntityFeatureBindings.RegisterHealth(builder, Bindings, Data.ComponentData);
            EntityFeatureBindings.RegisterRadar(builder, Bindings);
            EntityFeatureBindings.RegisterWeapons(builder, Bindings);
            EntityFeatureBindings.RegisterStationaryCombat(builder);
            EntityFeatureBindings.RegisterFog(builder, Bindings, !LocalPlayer.IsFriendly(Owner));
            EntityFeatureBindings.RegisterStructureVision(builder);
        }
    }
}
