using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class HarmonicBombService : ITickable, ILateDisposable
    {
        private sealed class Bomb
        {
            public PlayerId Owner;
            public Vector3 Position;
            public float Fuse;
            public float Radius;
            public HarmonicBombView Marker;
        }

        private readonly IEntityLocator _entities;
        private readonly IPlayerRelations _relations;
        private readonly IVisionService _vision;
        private readonly HarmonicBombEffects _effects;
        private readonly FactionCatalog _factions;
        private readonly List<Bomb> _bombs = new List<Bomb>();
        private readonly List<ISquadronMembersFacade> _hits = new();

        public HarmonicBombService(IEntityLocator entities, IPlayerRelations relations, IVisionService vision,
            HarmonicBombEffects effects, FactionCatalog factions)
        {
            _entities = entities;
            _relations = relations;
            _vision = vision;
            _effects = effects;
            _factions = factions;
        }

        public void Drop(PlayerId owner, Vector3 position, float fuse, float radius, HarmonicBombView prefab)
        {
            _bombs.Add(new Bomb
            {
                Owner = owner, Position = position, Fuse = fuse, Radius = radius,
                Marker = _effects.Drop(position, prefab)
            });
        }

        public bool HasEnoughTargets(PlayerId owner, Vector3 position, float radius, int minimum)
        {
            int count = 0;
            foreach (IEntity entity in _entities.Entities)
            {
                if (!TryGetHostileSquadron(owner, entity, out ISquadronMembersFacade squadron) ||
                    entity.IsCloaked()) continue;
                IReadOnlyList<IHardPointModel> members = squadron.Members;
                for (int i = 0; i < members.Count; i++)
                {
                    IHardPointModel member = members[i];
                    if (member.IsDestroyed) continue;
                    Vector3 memberPosition = member.Position;
                    if (!PlanarGeometry.ContainsCircle(memberPosition, position, radius) ||
                        !_vision.IsVisible(owner, memberPosition)) continue;
                    count++;
                    if (count >= minimum) return true;
                }
            }
            return false;
        }

        public void Tick() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            for (int i = _bombs.Count - 1; i >= 0; i--)
            {
                Bomb bomb = _bombs[i];
                bomb.Fuse -= deltaTime;
                if (bomb.Fuse > 0f) continue;
                _bombs.RemoveAt(i);
                _effects.Detonate(bomb.Marker, bomb.Radius, bomb.Owner, bomb.Position);
                ApplyBlast(bomb);
            }
        }

        private void ApplyBlast(Bomb bomb)
        {
            // Collected first: a killed squadron leaves the locator.
            foreach (IEntity entity in _entities.Entities)
            {
                if (TryGetHostileSquadron(bomb.Owner, entity, out ISquadronMembersFacade squadron) &&
                    HasMemberInside(squadron, bomb.Position, bomb.Radius)) _hits.Add(squadron);
            }

            foreach (ISquadronMembersFacade squadron in _hits) squadron.Kill();
            _hits.Clear();
        }

        private static bool HasMemberInside(ISquadronMembersFacade squadron, Vector3 position, float radius)
        {
            IReadOnlyList<IHardPointModel> members = squadron.Members;
            for (int i = 0; i < members.Count; i++)
            {
                IHardPointModel member = members[i];
                if (!member.IsDestroyed && PlanarGeometry.ContainsCircle(member.Position, position, radius)) return true;
            }
            return false;
        }

        // Hero squadrons are immune to the bomb.
        private bool TryGetHostileSquadron(PlayerId owner, IEntity entity, out ISquadronMembersFacade squadron)
        {
            squadron = null;
            return _relations.IsHostile(owner, entity.Owner) && !entity.HealthModel.IsDestroyed &&
                   entity.TryGetFacade(out squadron) &&
                   !_factions.GetSquadronFactionData(entity.GetFacade<IUnitTypeFacade>().UnitTypeId.SquadronType).IsHero;
        }

        public void Clear()
        {
            _bombs.Clear();
            _hits.Clear();
            _effects.Clear();
        }

        public void LateDispose() => Clear();
    }
}
