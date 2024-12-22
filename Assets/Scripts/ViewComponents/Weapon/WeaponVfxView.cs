using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace EmpireAtWar
{
    [Obsolete]
    public class WeaponVfxView : MonoBehaviour, IObserver<float>
    {
        [SerializeField] private ParticleSystem explosionVfxPrefab;
        private ParticleSystem explosionVfx;

        private INotifier<float> notifier;
        private void Start()
        {
            notifier = gameObject.GetComponent<INotifier<float>>();
            if (notifier != null)
            {
                notifier.AddObserver(this);
            }
            Assert.IsNotNull(notifier, "Notifier is null");
            
            //explosionVfxPrefab.transform.SetParent(transform.parent);// todo: refactor
        }

        private void OnDestroy()
        {
            if (notifier != null)
            {
                notifier.RemoveObserver(this);
            }
        }

        public void UpdateState(float value)
        {
            if (value <= 0)
            {
                explosionVfx = Instantiate(explosionVfxPrefab, transform.parent);
                explosionVfx.Play();
            }
        }
    }
}
