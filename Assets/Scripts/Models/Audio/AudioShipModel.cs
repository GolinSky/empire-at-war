using System;
using EmpireAtWar.Utils.Random;
using UnityEngine;
using LightWeightFramework.Model;
using UnityEngine.AddressableAssets;
using Zenject;

namespace EmpireAtWar.Models.Audio
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
        private AudioClip hyperSpaceAudioClip;


        public AudioClip HyperSpaceAudioClip
        {
            get
            {
                if (hyperSpaceAudioClip == null)
                {
                    hyperSpaceAudioClip = hyperSpaceAudioReference.LoadAssetAsync().WaitForCompletion();
                }

                return hyperSpaceAudioClip;
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