---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: implemented
scope: read-only source review
updated: "2026-09-27\r"
category: Refactoring
---

# Structure — Placement and type files

> [!info] Implementation update
> L3 and L4 implemented: SelectionType relocated/namespace updated, empty NavigationService removed, vendor code moved with metadata/license material, listed types split.
> See [[TODOs/Refactoring/Codebase Audit 2026-09-27/09 Implementation Results|implementation results and verification]]. Evidence/line numbers below describe the original audit snapshot unless marked implemented.

- [[TODOs/Refactoring/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

> [!warning] Follow-up 2026-09-28
> L3 left `Components/Utils/Scenes/` empty, with its `.meta` still tracked. L4 covered only the files it listed; 79 files still declare more than one top-level type. See [[TODOs/Refactoring/Codebase Audit 2026-09-27/10 Follow-up Sweep 2026-09-28|the follow-up sweep]].

## L3 — Domain placement and vendor ownership are unclear

- **Priority:** P3 · **Confidence:** confirmed placement mismatch; no obsolete API claim.

- **Evidence:** [Assets/Scripts/Services/NavigationService/SelectionType.cs:1](file:///F:/Private/empire-at-war/Assets/Scripts/Services/NavigationService/SelectionType.cs#L1) defines selection state while active selection code lives under Services/Selection.
- [Assets/Scripts/Components/Utils/Scenes/SceneReference.cs:1](file:///F:/Private/empire-at-war/Assets/Scripts/Components/Utils/Scenes/SceneReference.cs#L1) identifies itself as MIT-licensed third-party code.
- The vendored toolbar extender sits inside project Editor scripts and accesses Unity internals through reflection ([Assets/Scripts/Editor/ToolbarTimeScale/UnityToolbarExtender/Editor/ToolbarCallback.cs:16](file:///F:/Private/empire-at-war/Assets/Scripts/Editor/ToolbarTimeScale/UnityToolbarExtender/Editor/ToolbarCallback.cs#L16)).

- **Solution:** place SelectionType with selection contracts; identify vendor-owned sources as such and isolate them under the existing third-party policy.
- Preserve licenses, namespaces where needed, assembly/editor boundaries, GUIDs and serialized enum values.
- Keep project-specific adapters separate from vendor source.

- **Compatibility note:** the toolbar README advertises old-version testing and warns about reflection fragility.
- This is an upgrade-audit candidate, not proof it fails in the installed Unity version.
- No Unity APIs were executed or online deprecation claims made.

- **Future verification:** before implementation re-read [[Architecture/PROJECT_ORGANIZATION]], resolve its stale template terminology against live project rules, and use Unity-aware moves with explicit import/save verification.
- No assets or folders were moved during this audit.

## L4 — Multiple top-level types remain in single files

- **Priority:** P3 · **Confidence:** confirmed policy mismatch.

- **Evidence:** [Assets/Scripts/Services/Selection/SelectionContext.cs:10](file:///F:/Private/empire-at-war/Assets/Scripts/Services/Selection/SelectionContext.cs#L10) contains ISelectionSubject, ISelectionContext, SelectionContext and SelectionEntry.
- [Assets/Scripts/Services/Selection/SelectionService.cs:16](file:///F:/Private/empire-at-war/Assets/Scripts/Services/Selection/SelectionService.cs#L16) contains its interface and implementation.
- [Assets/Scripts/Entities/Faction/Controller/IPurchaseProcessor.cs:5](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/Faction/Controller/IPurchaseProcessor.cs#L5) contains both purchase interfaces.
- RandomVector3.cs also contains RandomFloat; RandomFromArray.cs contains RandomAudioClips.

- **Solution:** split project-owned top-level types into matching files, preserving their namespaces and public contracts.
- This is navigation/ownership cleanup; no new abstractions are required.
- Avoid rewriting vendor files to satisfy project-owned style rules.

- **Future verification:** account for Unity script metadata and serialized custom types, compile after an authorized change, and preserve existing asset references.
- Nested private implementation records do not automatically need separate files.
