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
            public float FighterDamageMultiplier;
            public DamageType DamageType;
            public HarmonicBombView Marker;
        }

        private readonly IEntityLocator _entities;
        private readonly IPlayerRelations _relations;
        private readonly IVisionService _vision;
        private readonly HarmonicBombEffects _effects;
        private readonly List<Bomb> _bombs = new();
        private readonly Stack<Bomb> _idleBombs = new();
        private readonly List<(IEntity Entity, IHealthFacade Health, IHardPointModel Member, int Generation, int Id, float Damage)> _hits = new();

        public HarmonicBombService(IEntityLocator entities, IPlayerRelations relations, IVisionService vision,
            HarmonicBombEffects effects)
        {
            _entities = entities;
            _relations = relations;
            _vision = vision;
            _effects = effects;
        }

        public void Drop(PlayerId owner, Vector3 position, float fuse, HarmonicBombSettings settings)
        {
            Bomb bomb = _idleBombs.Count > 0 ? _idleBombs.Pop() : new Bomb();
            bomb.Owner = owner;
            bomb.Position = position;
            bomb.Fuse = fuse;
            bomb.Radius = settings.Radius;
            bomb.Damage = settings.Damage;
            bomb.FighterDamageMultiplier = settings.FighterDamageMultiplier;
            bomb.DamageType = settings.DamageType;
            bomb.Marker = _effects.Drop(position, settings.ViewPrefab);
            _bombs.Add(bomb);
        }

        public bool HasEnoughTargets(PlayerId owner, Vector3 position, float radius, int minimum)
        {
            int count = 0;
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_relations.IsHostile(owner, entity.Owner) || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out ISquadronMembersFacade squadron) || entity.IsCloaked()) continue;
                IReadOnlyList<IHardPointModel> members = squadron.Members;
                for (int i = 0; i < members.Count; i++)
                {
                    IHardPointModel member = members[i];
                    if (member.IsDestroyed) continue;
                    Vector3 memberPosition = member.Position;
                    if ((memberPosition - position).sqrMagnitude > radius * radius ||
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
                ApplyBlast(bomb);
                _effects.Detonate(bomb.Marker, bomb.Radius, bomb.Owner, bomb.Position);
                bomb.Marker = null;
                _idleBombs.Push(bomb);
            }
        }

        private void ApplyBlast(Bomb bomb)
        {
            // Snapshot before damage callbacks can remove or pool entities and their members.
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_relations.IsHostile(bomb.Owner, entity.Owner) || entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits || !entity.TryGetFacade(out IHealthFacade health)) continue;
                if (entity.TryGetFacade(out ISquadronMembersFacade squadron))
                {
                    foreach (IHardPointModel member in squadron.Members)
                    {
                        if (member.IsDestroyed ||
                            (member.Position - bomb.Position).sqrMagnitude > bomb.Radius * bomb.Radius) continue;
                        _hits.Add((entity, health, member, member.Generation, member.Id,
                            bomb.Damage * bomb.FighterDamageMultiplier));
                    }
                }
                else if ((entity.GetFacade<IEntityTransformFacade>().Transform.position - bomb.Position).sqrMagnitude
                         <= bomb.Radius * bomb.Radius)
                {
                    IReadOnlyList<IReadOnlyHardPointModel> hardPoints = entity.HealthModel.HardPointModels;
                    int id = 0;
                    for (int i = 0; i < hardPoints.Count; i++)
                    {
                        if (hardPoints[i].IsDestroyed) continue;
                        id = i;
                        break;
                    }
                    _hits.Add((entity, health, null, 0, id, bomb.Damage));
                }
            }

            foreach (var hit in _hits)
            {
                if (!_entities.TryGetEntity(hit.Entity.Id, out IEntity current) || current != hit.Entity ||
                    current.HealthModel.IsDestroyed) continue;
                if (hit.Member != null &&
                    (hit.Member.IsDestroyed || hit.Member.Generation != hit.Generation ||
                     current.GetFacade<ISquadronMembersFacade>().Members[hit.Id] != hit.Member)) continue;
                hit.Health.ApplyDamage(hit.Damage, bomb.DamageType, hit.Id);
            }
            _hits.Clear();
        }

        public void Clear()
        {
            _bombs.Clear();
            _idleBombs.Clear();
            _hits.Clear();
            _effects.Clear();
        }

        public void LateDispose() => Clear();
    }
}
