using System;
using EmpireAtWar.Utils.Random;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
{
    public interface IAudioShipModelObserver : IModelObserver
    {
        event Action<AudioClip> OnOneShotPlayed;
        AudioClip AmbientClip { get; }
    }

    [CreateAssetMenu(fileName = "AudioShipModel", menuName = "Model/Audio/AudioShipModel")]
    public class AudioShipModel : Model, IAudioShipModelObserver, ILateDisposable
    {
        public event Action<AudioClip> OnOneShotPlayed;

        [SerializeField] private AssetReferenceT<AudioClip> hyperSpaceAudioReference;
        [SerializeField] private RandomAudioClips alarmRandomClips;
        [SerializeField] private RandomAudioClips backgroundClips;
        private AudioClip _hyperSpaceAudioClip;


        public AudioClip HyperSpaceAudioClip
        {
            get
            {
                if (_hyperSpaceAudioClip == null)
                {
                    _hyperSpaceAudioClip = hyperSpaceAudioReference.LoadAssetAsync().WaitForCompletion();
                }

                return _hyperSpaceAudioClip;
            }
        }

        public AudioClip AmbientClip => backgroundClips.GetRandom();
        [field:SerializeField] public RandomFloat AlarmDelay { get; private set; }

        
        public void LateDispose()
        {
            hyperSpaceAudioReference.ReleaseAsset();
        }

        public void PlayHyperSpace()
        {
            PlayOneShot(HyperSpaceAudioClip);
        }

        public void PlayAlarm()
        {
            PlayOneShot(alarmRandomClips.GetRandom());
        }

        private void PlayOneShot(AudioClip audioClip)
        {
            OnOneShotPlayed?.Invoke(audioClip);
        }
    }
}