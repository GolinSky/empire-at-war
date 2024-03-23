using System.Threading;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Audio;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;
using AudioType = EmpireAtWar.Services.Audio.AudioType;

namespace EmpireAtWar.Components.Audio
{
    public class AudioDialogShipComponent: BaseComponent<AudioShipDialogModel>, IInitializable,
        ILateDisposable
    {
        private const float MinAlarmDelay = 30;
        private const float MaxAlarmDelay = 60f;
        
        private readonly IAudioService audioService;
        private readonly PlayerType playerType;
        private readonly IShipMoveModelObserver shipMoveModelObserver;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly IRadarModelObserver radarModelObserver;
        private readonly ITimer alarmRadarTimer;
        
        public AudioDialogShipComponent(IModel model, IAudioService audioService, PlayerType playerType) : base(model)
        {
            this.audioService = audioService;
            this.playerType = playerType;
            shipMoveModelObserver = model.GetModelObserver<IShipMoveModelObserver>();
            selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            radarModelObserver = model.GetModelObserver<IRadarModelObserver>();
            alarmRadarTimer = TimerFactory.ConstructTimer(Random.Range(MinAlarmDelay, MaxAlarmDelay));
        }
        
        public void Initialize()
        {
            selectionModelObserver.OnSelected += PlaySelectionClip;
            shipMoveModelObserver.OnLookAt += PlayAttackClip;
            shipMoveModelObserver.OnTargetPositionChanged += PlayMoveClip;
            shipMoveModelObserver.OnStop += PlayDamageClip;
            radarModelObserver.OnHitDetected += PlayAlarmSights;
        }

        public void LateDispose()
        {
            selectionModelObserver.OnSelected -= PlaySelectionClip;
            shipMoveModelObserver.OnLookAt -= PlayAttackClip;
            shipMoveModelObserver.OnTargetPositionChanged -= PlayMoveClip;
            shipMoveModelObserver.OnStop -= PlayDamageClip;
            radarModelObserver.OnHitDetected -= PlayAlarmSights;
        }

        private void PlayAlarmSights(RaycastHit[] raycastHits)
        {
            if (alarmRadarTimer.IsComplete)
            {
                alarmRadarTimer.StartTimer();
                Play(Model.GetAlarmSightsClip(playerType));
            }
        }

        private void PlayDamageClip()
        {
            Play(Model.GetDamageClip(playerType));
        }

        private void PlayMoveClip(Vector3 obj)
        {
            Play(Model.GetMoveClip(playerType));
        }
        
        private void PlayAttackClip(Vector3 vector3)
        {
            Play(Model.GetAttackClip(playerType));
        }
        
        private void PlaySelectionClip(bool isSelected)
        {
            if (isSelected)
            {
                Play(Model.GetDialogClip(playerType));
            }
        }

        private void Play(AudioClip audioClip)
        {
            audioService.PlayOneShot(audioClip, AudioType.Dialog);
        }
    }
}