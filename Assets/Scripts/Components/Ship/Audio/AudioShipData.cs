using EmpireAtWar.Utils.Random;
using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
{
    [CreateAssetMenu(fileName = "AudioShipData", menuName = "Data/Audio/AudioShipData")]
    public class AudioShipData : Data, ILateDisposable
    {
        [SerializeField] private AssetReferenceT<AudioClip> hyperSpaceAudioReference;
        [SerializeField] private RandomAudioClips alarmRandomClips;
        [SerializeField] private RandomAudioClips backgroundClips;
        private AudioClip _hyperSpaceAudioClip;

        [field:SerializeField] public RandomFloat AlarmDelay { get; private set; }

        public AudioClip GetAmbientClip()
        {
            return backgroundClips.GetRandom();
        }

        public AudioClip GetAlarmClip()
        {
            return alarmRandomClips.GetRandom();
        }

        public AudioClip GetHyperSpaceClip()
        {
            if (_hyperSpaceAudioClip == null)
            {
                _hyperSpaceAudioClip = hyperSpaceAudioReference.LoadAssetAsync().WaitForCompletion();
            }

            return _hyperSpaceAudioClip;
        }

        public void LateDispose()
        {
            hyperSpaceAudioReference.ReleaseAsset();
        }
    }
}
