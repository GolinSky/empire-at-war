using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.SpawnBlocking;
using UnityEngine;
using VContainer;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Saves;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.SpaceStation
{
    public sealed class SpaceStationInstaller : DynamicEntityInstaller<SpaceStationEntity, SpaceStationData>
    {
        private readonly FactionType _factionType;

        public SpaceStationInstaller(SpaceStationEntity view, SpaceStationData data, ILocalPlayer localPlayer,
            PlayerId owner, long entityId, Vector3 position, FactionType factionType)
            : base(view, data, localPlayer, owner, entityId, position) => _factionType = factionType;

        protected override void InstallFeatures(IContainerBuilder builder)
        {
            builder.RegisterInstance(Data);
            builder.RegisterInstance(_factionType);
            builder.RegisterInstance(Data.ComponentData).AsSelf().As<IRadarData, IFogVisionData, ISpawnBlockerData>();
            builder.Register<SpaceStationTooltipFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<PlayerBaseFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ProductionBaseFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<StationFootprintFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<StructureSaveFacade>(Lifetime.Singleton).AsImplementedInterfaces()
                .WithParameter("kind", StructureKind.SpaceStation).WithParameter("type", (int)_factionType);
            EntityFeatureBindings.RegisterSelection(builder, Bindings, SelectionType.Base, SelectionType.Base);
            EntityFeatureBindings.RegisterCombatModifiers(builder);
            EntityFeatureBindings.RegisterHealth(builder, Bindings, Data.ComponentData);
            EntityFeatureBindings.RegisterRadar(builder, Bindings);
            EntityFeatureBindings.RegisterWeapons(builder, Bindings);
            EntityFeatureBindings.RegisterStationaryCombat(builder);
            EntityFeatureBindings.RegisterFog(builder, Bindings, !LocalPlayer.IsFriendly(Owner));
            builder.Register<StationHangarData>(Lifetime.Singleton).As<IHangarData>();
            builder.Register<HangarModel>(Lifetime.Singleton);
            EntityFeatureBindings.RegisterComponent(builder, Bindings.Hangar);
        }
    }
}
