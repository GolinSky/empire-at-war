---
category: Refactoring
status: done
created: 2026-11-06
updated: 2026-11-06
completed: 2026-11-06
acceptance: pending-user
---
# Empty Code Cleanup: SelectionComponent hooks and MenuData

## Implementation

- Removed both empty `OnSelected` hooks, `OnSkipSelection`, the serialized `selectionType` field, and the flag used only by those hooks. The model event still drives the selection ring.
- Removed the squadron prefab builder's field assignment. Unity reserialized all 96 affected prefabs. Each diff deletes one serialized field.
- Removed the empty menu model and observer, the battle preload and registration, the model asset, its mapping key, and its Addressables entry.
- 2026-11-06 review fix: deleted the empty `Assets/Settings/Data/Models/SkirmishMenu` folder and its tracked `.meta` via Unity (`9a8654528` left it behind).
- Updated lane 2a of [[TODOs/Refactoring/Type_File_Split_And_Placement]] to cover the eight remaining data files.

## Verification

- Unity compiled the updated selection component, resolved zero Addressables entries for the deleted GUID, and reported zero Console errors.
- Asset and script search found zero references to the deleted menu types, asset GUID, skip hook, or serialized field.
- Tests were not run under the checkout override. Test and gameplay checks are tracked in [[TODOs/User_Acceptance_Queue]].
- No reusable guidance to add. [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]] already covers Addressables API edits and persistence.
