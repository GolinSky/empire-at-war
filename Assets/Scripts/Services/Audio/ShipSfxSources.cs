using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public sealed class ShipSfxSources : MonoBehaviour
    {
        [SerializeField] private AudioSource[] sfx;
        [SerializeField] private AudioSource voice;

        public AudioSource[] Sfx => sfx;
        public AudioSource Voice => voice;
    }
}
