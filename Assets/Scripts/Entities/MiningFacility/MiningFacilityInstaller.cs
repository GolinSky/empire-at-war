using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.SpawnBlocking;
using UnityEngine;
using VContainer;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Saves;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.MiningFacility
{
    public class MiningFacilityInstaller : DynamicEntityInstaller<MiningFacilityEntity, MiningFacilityData>
    {
        private readonly MiningFacilityType _type;

        public MiningFacilityInstaller(MiningFacilityEntity view, MiningFacilityData data, ILocalPlayer localPlayer,
            PlayerId owner, long entityId, Vector3 position, MiningFacilityType type)
            : base(view, data, localPlayer, owner, entityId, position) => _type = type;

        protected override void InstallFeatures(IContainerBuilder builder)
        {
            builder.RegisterInstance(Data);
            builder.RegisterInstance(_type);
            builder.RegisterInstance(Data.ComponentData).AsSelf().As<IRadarData, IFogVisionData, ISpawnBlockerData>();
            builder.Register<MiningFacilityModel>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces()
                .WithParameter("baseIncome", Data.Income);
            builder.Register<MiningFacilityTooltipFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<StructureSaveFacade>(Lifetime.Singleton).AsImplementedInterfaces()
                .WithParameter("kind", StructureKind.MiningFacility).WithParameter("type", (int)_type);
            EntityFeatureBindings.RegisterSelection(builder, Bindings, SelectionType.MiningFacility, SelectionType.MiningFacility);
            EntityFeatureBindings.RegisterHighlight(builder, Bindings);
            EntityFeatureBindings.RegisterCombatModifiers(builder);
            EntityFeatureBindings.RegisterHealth(builder, Bindings, Data.ComponentData);
            EntityFeatureBindings.RegisterRadar(builder, Bindings);
            EntityFeatureBindings.RegisterFog(builder, Bindings, !LocalPlayer.IsFriendly(Owner));
            EntityFeatureBindings.RegisterStructureVision(builder);
        }
    }
}
