using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
using EmpireAtWar.Lifecycle;
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
            public float Damage;
            public HarmonicBombView Marker;
        }

        private readonly IEntityLocator _entities;
        private readonly IPlayerRelations _relations;
        private readonly IVisionService _vision;
        private readonly HarmonicBombEffects _effects;
        private readonly List<Bomb> _bombs = new List<Bomb>();
        private readonly List<(IEntity Entity, ISquadronMembersFacade Squadron, IHardPointModel Member,
            int Index, IHealthFacade Health, float Damage)> _hits = new();

        public HarmonicBombService(IEntityLocator entities, IPlayerRelations relations, IVisionService vision,
            HarmonicBombEffects effects)
        {
            _entities = entities;
            _relations = relations;
            _vision = vision;
            _effects = effects;
        }

        public void Drop(PlayerId owner, Vector3 position, float fuse, float radius, float damage,
            HarmonicBombView prefab)
        {
            _bombs.Add(new Bomb
            {
                Owner = owner, Position = position, Fuse = fuse, Radius = radius, Damage = damage,
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
            // A lethal hit may remove a squadron from the locator or recycle it into a new formation.
            foreach (IEntity entity in _entities.Entities)
            {
                if (!TryGetHostileSquadron(bomb.Owner, entity, out ISquadronMembersFacade squadron)) continue;
                IHealthFacade health = entity.GetFacade<IHealthFacade>();
                IReadOnlyList<IHardPointModel> members = squadron.Members;
                for (int i = 0; i < members.Count; i++)
                {
                    IHardPointModel member = members[i];
                    if (member.IsDestroyed) continue;
                    float distance = PlanarGeometry.Distance(member.Position, bomb.Position);
                    if (distance > bomb.Radius) continue;
                    float multiplier = distance <= bomb.Radius / 3f ? 1f :
                        distance <= 2f * bomb.Radius / 3f ? 0.6f : 0.25f;
                    _hits.Add((entity, squadron, member, i, health, bomb.Damage * multiplier));
                }
            }

            foreach (var hit in _hits)
            {
                if (!_entities.TryGetEntity(hit.Entity.Id, out IEntity current) || current != hit.Entity ||
                    hit.Member.IsDestroyed || hit.Index >= hit.Squadron.Members.Count ||
                    !ReferenceEquals(hit.Squadron.Members[hit.Index], hit.Member)) continue;
                hit.Health.ApplyDamage(hit.Damage, DamageType.HypervelocityGun, hit.Member.Id);
            }
            _hits.Clear();
        }

        private bool TryGetHostileSquadron(PlayerId owner, IEntity entity, out ISquadronMembersFacade squadron)
        {
            squadron = null;
            return _relations.IsHostile(owner, entity.Owner) && !entity.HealthModel.IsDestroyed &&
                   entity.TryGetFacade(out squadron);
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
