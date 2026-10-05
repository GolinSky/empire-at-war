using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEngine;
using Zenject;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Components.Squadrons.Flight
{
    /// <summary>
    /// Moves every fighter along its simulated flight path. The squadron root is kept on the centroid of
    /// the live fighters so selection, radar, minimap and targeting treat the squadron as one unit.
    /// </summary>
    public sealed class SquadronFlightComponent : MonoComponent<SquadronFlightModel>, ISquadronFlightComponent,
        IInitializable, ILateDisposable
    {
        private IVisionService _visionService;
        private IFogVisionData _visionData;
        private EmpireAtWar.Components.Combat.CombatModifiers _modifiers;

        [SerializeField] private List<FighterView> fighters;
        private readonly List<NumericsVector3> _spawnPositions = new List<NumericsVector3>();

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private PlayerId _owner;

        private bool _isReleased;

        public IFighterFlightData Data => Model.Data;
        public int Count => fighters.Count;
        public Vector3 Centroid => transform.position;
        public Vector3 Heading => Model.GetHeading().ToUnity();

        [Inject]
        private void Construct(IVisionService visionService, IFogVisionData visionData,
            SquadronFlightModel model, Vector3 startPosition, Quaternion startRotation, PlayerId owner,
            EmpireAtWar.Components.Combat.CombatModifiers modifiers)
        {
            SetModel(model);
            _startPosition = startPosition;
            _startRotation = startRotation;
            _owner = owner;
            _visionService = visionService;
            _visionData = visionData;
            _modifiers = modifiers;
        }

        public void Initialize()
        {
            Vector3 forward = _startRotation * Vector3.forward;
            forward.y = 0f;
            // Near-vertical launch points can flatten to a vector Unity normalizes to zero.
            forward.Normalize();
            forward = forward.sqrMagnitude > 0f ? forward : Vector3.forward;
            NumericsVector3 heading = forward.ToNumerics();
            NumericsVector3 origin = _startPosition.ToNumerics();
            for (int i = 0; i < fighters.Count; i++)
            {
                NumericsVector3 slot = SquadronFormation.GetSlot(i, Model.Data.FormationSpacing);
                _spawnPositions.Add(origin + SquadronFormation.ToWorld(slot, heading));
            }

            Model.Spawn(_spawnPositions, heading);
            ApplyPoses();
            foreach (FighterView fighter in fighters)
            {
                fighter.ClearTrails();
            }

            _modifiers.Changed += RefreshVision;
            _visionService.Register(_owner, transform, VisionRange);
        }

        public void LateDispose() => Release();

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            _modifiers.Changed -= RefreshVision;
            _visionService.Unregister(transform);
        }

        public bool IsAlive(int index) => Model.IsAlive(index);

        private float VisionRange => _visionData.VisionRange * _modifiers.VisionMultiplier;

        private void RefreshVision() => _visionService.Register(_owner, transform, VisionRange);

        public Vector3 GetPosition(int index) => Model.Get(index).Position.ToUnity();

        public Vector3 GetForward(int index) => Model.Get(index).Forward.ToUnity();

        public void Steer(int index, Vector3 target, float speed) =>
            Model.SetSteering(index, target.ToNumerics(), speed);

        public void Step(float deltaTime)
        {
            for (int i = 0; i < fighters.Count; i++)
            {
                if (fighters[i].IsDestroyed && Model.IsAlive(i))
                {
                    Model.Kill(i);
                }
            }

            Model.Step(deltaTime);
            ApplyPoses();
        }

        private void ApplyPoses()
        {
            // The root moves first: dead fighters keep their last world pose instead of drifting with it.
            transform.position = Model.GetCentroid().ToUnity();
            for (int i = 0; i < fighters.Count; i++)
            {
                FighterKinematics fighter = Model.Get(i);
                fighters[i].ApplyPose(fighter.Position.ToUnity(), fighter.Forward.ToUnity(), fighter.Bank);
            }
        }
    }
}
