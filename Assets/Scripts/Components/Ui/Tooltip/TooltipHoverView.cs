using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Tooltip;
using UnityEngine;

namespace EmpireAtWar.Components.Ui.Tooltip
{
    public sealed class TooltipHoverView : MonoBehaviour
    {
        [SerializeField] private TooltipTrigger[] triggers = Array.Empty<TooltipTrigger>();
        private readonly List<TooltipTrigger> _registered = new List<TooltipTrigger>();
        public event Action<object, TooltipAnchor, object> HoverStarted;
        public event Action<object> HoverEnded;
        private void Awake()
        {
            foreach (TooltipTrigger trigger in triggers) Register(trigger);
        }
        public void Register(TooltipTrigger trigger)
        {
            if (_registered.Contains(trigger)) return;
            _registered.Add(trigger);
            trigger.HoverStarted += ForwardStarted;
            trigger.HoverEnded += ForwardEnded;
            trigger.Destroyed += Unregister;
        }
        public void Unregister(TooltipTrigger trigger)
        {
            trigger.HoverStarted -= ForwardStarted;
            trigger.HoverEnded -= ForwardEnded;
            trigger.Destroyed -= Unregister;
            _registered.Remove(trigger);
            ForwardEnded(trigger);
        }
        private void ForwardStarted(object key, TooltipAnchor anchor, object source) => HoverStarted?.Invoke(key, anchor, source);
        public void ForwardTo(TooltipHoverView parent, object key)
        {
            foreach (TooltipTrigger trigger in triggers) trigger.SetKey(key);
            foreach (TooltipTrigger trigger in _registered) parent.Register(trigger);
        }
        private void ForwardEnded(object source) => HoverEnded?.Invoke(source);
        private void OnDisable()
        {
            foreach (TooltipTrigger trigger in _registered) ForwardEnded(trigger);
        }
        private void OnDestroy()
        {
            foreach (TooltipTrigger trigger in _registered)
            {
                if (trigger == null) continue;
                trigger.HoverStarted -= ForwardStarted;
                trigger.HoverEnded -= ForwardEnded;
                trigger.Destroyed -= Unregister;
            }
            _registered.Clear();
        }
    }
}
