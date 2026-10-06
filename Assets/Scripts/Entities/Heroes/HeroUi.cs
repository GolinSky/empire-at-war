using System;
using System.Collections.Generic;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.Heroes
{
    public sealed class HeroUi : BaseUi, IHeroUi
    {
        [SerializeField] private RectTransform friendlyHeroes;
        [SerializeField] private RectTransform enemyHeroes;
        [SerializeField] private HeroIconUi iconPrefab;

        private readonly Dictionary<long, HeroIconUi> _icons = new Dictionary<long, HeroIconUi>();
        private IHeroPresenter _presenter;
        private bool _isDisposed;

        public event Action Disposed;

        public void SetPresenter(IHeroPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            friendlyHeroes.gameObject.SetActive(false);
            enemyHeroes.gameObject.SetActive(false);
            Hide();
        }

        public void AddHero(long entityId, Sprite icon, bool isFriendly, bool canFocus)
        {
            RectTransform row = isFriendly ? friendlyHeroes : enemyHeroes;
            HeroIconUi item = Instantiate(iconPrefab, row);
            item.Initialize(entityId, icon, _presenter, canFocus);
            _icons.Add(entityId, item);
            row.gameObject.SetActive(true);
        }

        public void RemoveHero(long entityId)
        {
            HeroIconUi item = _icons[entityId];
            Transform row = item.transform.parent;
            item.Dispose();
            item.gameObject.SetActive(false);
            item.transform.SetParent(null, false);
            Destroy(item.gameObject);
            _icons.Remove(entityId);
            row.gameObject.SetActive(row.childCount > 0);
        }

        public void SetFocusable(long entityId, bool canFocus) => _icons[entityId].SetFocusable(canFocus);

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            foreach (HeroIconUi icon in _icons.Values)
                icon.Dispose();
            _icons.Clear();
            Disposed?.Invoke();
        }

        private void OnDestroy() => Dispose();
    }
}
