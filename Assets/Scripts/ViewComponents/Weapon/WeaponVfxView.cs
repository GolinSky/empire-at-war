using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace EmpireAtWar
{
    [Obsolete]
    public class WeaponVfxView : MonoBehaviour, IObserver<float>
    {
        [SerializeField] private ParticleSystem explosionVfxPrefab;
        private ParticleSystem _explosionVfx;

        private INotifier<float> _notifier;
        private void Start()
        {
            _notifier = gameObject.GetComponent<INotifier<float>>();
            if (_notifier != null)
            {
                _notifier.AddObserver(this);
            }
            Assert.IsNotNull(_notifier, "Notifier is null");
            
            //explosionVfxPrefab.transform.SetParent(transform.parent);// todo: refactor
        }

        private void OnDestroy()
        {
            if (_notifier != null)
            {
                _notifier.RemoveObserver(this);
            }
        }

        public void UpdateState(float value)
        {
            if (value <= 0)
            {
                _explosionVfx = Instantiate(explosionVfxPrefab, transform.parent);
                _explosionVfx.Play();
            }
        }
    }
}
