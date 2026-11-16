using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ControlGroups;

namespace EmpireAtWar.Services.Saves
{
    public sealed class ControlGroupSaveSource : IBattleSaveSource
    {
        private readonly ControlGroupService _groups;
        private readonly ILocalPlayer _localPlayer;

        public ControlGroupSaveSource(ControlGroupService groups, ILocalPlayer localPlayer)
        {
            _groups = groups;
            _localPlayer = localPlayer;
        }

        public void Capture(BattleSave save)
        {
            var groups = new List<ControlGroupSave>();
            var members = new List<IEntity>();
            for (int index = 0; index < ControlGroupModel.SLOT_COUNT; index++)
            {
                int slot = (index + 1) % ControlGroupModel.SLOT_COUNT;
                _groups.GetMembers(slot, members);
                if (members.Count == 0) continue;
                var ids = new long[members.Count];
                for (int i = 0; i < members.Count; i++) ids[i] = members[i].Id;
                groups.Add(ControlGroupSave.Create(slot, _localPlayer.Id, ids));
            }
            save.ControlGroups = groups.ToArray();
        }

        public void RestoreBeforeUnits(BattleSave save) => _groups.Clear();

        public void RestoreAfterUnits(BattleRestoreContext context)
        {
            foreach (ControlGroupSave group in context.Save.ControlGroups)
                if (_localPlayer.IsLocal(group.Owner)) _groups.Assign(group.Slot, group.Members);
        }
    }
}
