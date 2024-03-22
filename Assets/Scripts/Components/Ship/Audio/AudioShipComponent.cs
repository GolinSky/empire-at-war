using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.TimerPoolWrapperService;
using LightWeightFramework.Command;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;
using AudioType = EmpireAtWar.Services.Audio.AudioType;

namespace EmpireAtWar.Components.Audio
{
    public interface IAudioShipComponent : ICommand
    {
    }

    public class AudioShipComponent : BaseComponent<AudioShipModel>, IAudioShipComponent, IInitializable,
        ILateDisposable
    {
        private const float HyperSpaceTimePercentage = 0.8f;
        
        private readonly ITimerPoolWrapperService timerPoolWrapperService;
        private readonly IAudioService audioService;
        private readonly PlayerType playerType;
        private readonly IShipMoveModelObserver shipMoveModelObserver;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly IRadarModelObserver radarModelObserver;
        
        public AudioShipComponent(
            IModel model,
            ITimerPoolWrapperService timerPoolWrapperService,
            IAudioService audioService, 
            PlayerType playerType) : base(model)
        {
            this.timerPoolWrapperService = timerPoolWrapperService;
            this.audioService = audioService;
            this.playerType = playerType;
            shipMoveModelObserver = model.GetModelObserver<IShipMoveModelObserver>();
            selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            radarModelObserver = model.GetModelObserver<IRadarModelObserver>();
        }

        public void Initialize()
        {
            timerPoolWrapperService.Invoke(() => { Model.PlayOneShot(Model.HyperSpaceAudioClip); },
                shipMoveModelObserver.HyperSpaceSpeed * HyperSpaceTimePercentage);
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
            if (raycastHits.Length > 0)
            {
                PlayAudio2d(Model.GetAlarmSightsClip(playerType));
            }
        }

        private void PlayDamageClip()
        {
            PlayAudio2d(Model.GetDamageClip(playerType));
        }

        private void PlayMoveClip(Vector3 obj)
        {
            PlayAudio2d(Model.GetMoveClip(playerType));
        }
        
        private void PlayAttackClip(Vector3 vector3)
        {
            PlayAudio2d(Model.GetAttackClip(playerType));

        }
        
        private void PlaySelectionClip(bool isSelected)
        {
            if (isSelected)
            {
                PlayAudio2d(Model.GetDialogClip(playerType));
            }
        }

        private void PlayAudio2d(AudioClip audioClip)
        {
            audioService.PlayOneShot(audioClip, AudioType.Dialog);
        }
    }
}