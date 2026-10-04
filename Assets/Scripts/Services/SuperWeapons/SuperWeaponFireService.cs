using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;
using Zenject;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.SuperWeapons
{
    /// <summary>
    /// Fires superweapons from the scene's origin (the planet): plays each shot of the salvo, applies its damage on impact,
    /// then the area damage and the disable effect of the weapon.
    /// </summary>
    public sealed class SuperWeaponFireService : ISuperWeaponFireService, ITickable, ILateDisposable
    {
        private readonly IPlayerRelations _relations;
        private readonly IEntityLocator _entities;
        private readonly ISuperWeaponOrigin _origin;

        private readonly SuperWeaponData _data;
        private readonly ImpactEffectPresenter _impactPresenter;
        private readonly IAudioService _audioService;
        private readonly SfxProfile _hypervelocitySfx;
        private readonly AudioSource _hypervelocitySource;
        private readonly List<SuperWeaponSalvo> _salvos = new List<SuperWeaponSalvo>();
        private readonly List<SuperWeaponStun> _stuns = new List<SuperWeaponStun>();
        private readonly List<IEntity> _areaTargets = new List<IEntity>();

        public SuperWeaponFireService(IEntityLocator entities, ISuperWeaponOrigin origin,
            IPlayerRelations relations, SuperWeaponData data,
            ImpactEffectPresenter impactPresenter, IAudioService audioService, ShipSfxData sfxData,
            ShipSfxSources sfxSources)
        {
            _relations = relations;
            _data = data;
            _impactPresenter = impactPresenter;
            _entities = entities;
            _origin = origin;
            _audioService = audioService;
            _hypervelocitySfx = sfxData.GetWeapon(WeaponType.HypervelocityGun);
            _hypervelocitySource = Object.Instantiate(sfxSources.Sfx[0]);
            _hypervelocitySource.volume = 1f;
        }

        public void LateDispose()
        {
            foreach (SuperWeaponSalvo salvo in _salvos)
            {
                // Scene unload can destroy the origin before the service is disposed.
                if (salvo.Origin != null) Object.Destroy(salvo.Origin.gameObject);
            }

            _salvos.Clear();
            foreach (SuperWeaponStun stun in _stuns)
            {
                stun.Modifiers.Remove(stun.Modifier);
                stun.Modifiers.SetIonDisabled(false);
            }
            _stuns.Clear();
            if (_hypervelocitySource != null) Object.Destroy(_hypervelocitySource.gameObject);
        }

        public bool CanTarget(PlayerId owner, IEntity target)
        {
            return target != null && _relations.IsHostile(owner, target.Owner) &&
                   !target.HealthModel.IsDestroyed && target.HealthModel.HasUnits &&
                   !target.HealthModel.ShipClass.IsStrikecraft() &&
                   target.TryGetFacade(out IHealthFacade _);
        }

        public void Fire(SuperWeaponType type, IEntity target)
        {
            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            Vector3 originPosition = _origin.GetFirePosition(targetPosition);
            GameObject origin = new GameObject($"{type}Origin");
            origin.transform.SetPositionAndRotation(originPosition,
                Quaternion.LookRotation(targetPosition - originPosition));
            _salvos.Add(new SuperWeaponSalvo(profile: _data.GetProfile(type), target: target, origin: origin.transform));
        }

        public void Tick()
        {
            float deltaTime = Time.deltaTime;
            for (int i = _salvos.Count - 1; i >= 0; i--)
            {
                SuperWeaponSalvo salvo = _salvos[i];
                AdvanceSalvo(salvo, deltaTime);
                if (!salvo.IsComplete) continue;
                Object.Destroy(salvo.Origin.gameObject);
                _salvos.RemoveAt(i);
            }

            for (int i = _stuns.Count - 1; i >= 0; i--)
            {
                SuperWeaponStun stun = _stuns[i];
                stun.TimeLeft -= deltaTime;
                if (stun.TimeLeft > 0f) continue;
                stun.Modifiers.Remove(stun.Modifier);
                stun.Modifiers.SetIonDisabled(false);
                _stuns.RemoveAt(i);
            }
        }

        private void AdvanceSalvo(SuperWeaponSalvo salvo, float deltaTime)
        {
            List<float> impactTimes = salvo.ImpactTimes;
            for (int i = impactTimes.Count - 1; i >= 0; i--)
            {
                impactTimes[i] -= deltaTime;
                if (impactTimes[i] > 0f) continue;
                impactTimes.RemoveAt(i);
                ApplyHit(salvo);
            }

            int shotCount = salvo.Profile.Weapon.ShotsPerSalvo;
            if (salvo.ShotsFired >= shotCount) return;
            if (salvo.Target.HealthModel.IsDestroyed)
            {
                salvo.ShotsFired = shotCount;
                return;
            }

            salvo.NextShotTime -= deltaTime;
            while (salvo.NextShotTime <= 0f && salvo.ShotsFired < shotCount)
            {
                FireShot(salvo);
                salvo.NextShotTime += salvo.Profile.Weapon.ShotInterval;
            }
        }

        private void FireShot(SuperWeaponSalvo salvo)
        {
            SuperWeaponProfile profile = salvo.Profile;
            if (profile.Weapon.WeaponType == WeaponType.HypervelocityGun)
                _audioService.PlayOneShot(_hypervelocitySource, _hypervelocitySfx.GetClip(), _hypervelocitySfx.Volume);
            ShotEffect shot = Object.Instantiate(profile.Weapon.ShotPrefab);
            shot.PrepareImpact(_impactPresenter, salvo.Target.HealthModel, profile.Weapon.DamageType,
                profile.ImpactSize, true);
            float travelTime = shot.Fire(salvo.Origin, salvo.Target.GetFacade<IEntityTransformFacade>().Transform, Vector3.zero,
                profile.Weapon);
            shot.RetireAfterCompletion();
            salvo.ImpactTimes.Add(travelTime);
            salvo.ShotsFired++;
        }

        private void ApplyHit(SuperWeaponSalvo salvo)
        {
            SuperWeaponProfile profile = salvo.Profile;
            IEntity target = salvo.Target;
            if (target.HealthModel.IsDestroyed) return;

            Vector3 impactPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            ApplyDamage(target, profile.Weapon.Damage, profile);
            if (!target.HealthModel.IsDestroyed && profile.StunDuration > 0f &&
                target.TryGetFacade(out ICombatModifiersFacade combat))
            {
                ApplyStun(combat.Modifiers, profile);
            }

            if (profile.AreaDamage > 0f)
            {
                ApplyAreaDamage(target, impactPosition, profile);
            }
        }

        private void ApplyAreaDamage(IEntity target, Vector3 center, SuperWeaponProfile profile)
        {
            // Damage can release entities, so the locator must not be enumerated while it is applied.
            _areaTargets.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                // The blast hits the target's whole team, never the shooter's side.
                if (entity == target || !_relations.IsAllied(entity.Owner, target.Owner) ||
                    entity.HealthModel.IsDestroyed || !entity.HealthModel.HasUnits ||
                    PlanarGeometry.Distance(center, entity.GetFacade<IEntityTransformFacade>().Transform.position) > profile.AreaRadius)
                    continue;
                _areaTargets.Add(entity);
            }

            foreach (IEntity entity in _areaTargets)
            {
                if (!entity.HealthModel.IsDestroyed) ApplyDamage(entity, profile.AreaDamage, profile);
            }
        }

        private static void ApplyDamage(IEntity entity, float damage, SuperWeaponProfile profile)
        {
            if (!entity.TryGetFacade(out IHealthFacade health)) return;
            health.ApplyDamage(damage, profile.Weapon.DamageType, PickHardPoint(entity.HealthModel));
        }

        /// <summary>A random live hardpoint; once all are destroyed the wrecks still carry hull damage.</summary>
        private static int PickHardPoint(IHealthModelObserver health)
        {
            HardPointModel[] hardPoints = health.HardPointModels;
            int liveCount = 0;
            foreach (HardPointModel hardPoint in hardPoints)
            {
                if (!hardPoint.IsDestroyed) liveCount++;
            }

            if (liveCount == 0) return Random.Range(0, hardPoints.Length);

            int pick = Random.Range(0, liveCount);
            for (int i = 0; i < hardPoints.Length; i++)
            {
                if (hardPoints[i].IsDestroyed) continue;
                if (pick == 0) return i;
                pick--;
            }

            return 0;
        }

        private void ApplyStun(CombatModifiers modifiers, SuperWeaponProfile profile)
        {
            // A repeated hit refreshes the running disable instead of stacking it.
            foreach (SuperWeaponStun stun in _stuns)
            {
                if (stun.Modifiers != modifiers) continue;
                stun.TimeLeft = Mathf.Max(stun.TimeLeft, profile.StunDuration);
                return;
            }

            modifiers.Add(profile.StunModifier);
            modifiers.SetIonDisabled(true);
            _stuns.Add(new SuperWeaponStun(modifiers, profile.StunModifier, profile.StunDuration));
        }
    }
}
