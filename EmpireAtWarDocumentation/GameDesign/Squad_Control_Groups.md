# Squad Control Groups

## Rules

- Ten fixed physical top-row slots: 1–9, then 0; numpad excluded. Keys hidden from rebinding and reserved from other actions.
- Hold Ctrl for preview; modifier + digit assigns; plain digit replaces selection. Mac settings optionally use Command.
- Owned mobile ships/heroes/support and selectable squadrons qualify; one squadron = one member. Mixed and overlapping groups allowed.
- Empty assignment preserves slot; identical membership skips popup. Different occupied membership requires shared Replace/Cancel confirmation.
- Confirmation keeps a candidate snapshot and gameplay lock, continues simulation, survives Alt-tab and revalidates at acceptance.
- Empty/unavailable recall preserves selection and reports through battle notifier. No camera movement, orders or separate Clear operation.
- Save committed groups with battle/owner; old saves/new battles empty. Death/despawn removes members; never add replacement lives.
- Preview is transparent and compact; 1–2 distinct icons, heroes first, then class strength, stable type tie-break. Counts never rank icons.

## Implementation

- Model/service: `Services/ControlGroups`; UI: `Entities/ControlGroups`; save: `ControlGroupSaveSource`.
- Use `ISelectionService.SelectEntities` for atomic ordinary selection lifecycle; never loop resetting `SelectEntity`.
- Store `long` IDs, resolve via `IEntityLocator`, prune removed/dead/nonlocal entities.
- Pooled squadrons can reuse IDs. Pending snapshots must record removal events, excluding that ID even if it resolves again before acceptance.
- Battle notification feed groups retain initial text: use distinct group keys for distinct feedback messages.
- Generic popup caller owns its lock, version token and teardown. Focus loss hides preview but leaves confirmation intact.
- Delivery/verification: [[Done/Features/Squad_Control_Groups_Plan]]. In-game acceptance remains in [[TODOs/User_Acceptance_Queue]].
