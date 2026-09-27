---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: proposed
scope: read-only source review
---
# Cleanup — Unused candidates

[[TODOs/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

## L1 — CollectionUtility has no located callers
**Priority:** P3 · **Confidence:** unused candidate, not proven universally dead.

**Evidence:** [Assets/Scripts/Components/Utils/Collections/CollectionUtility.cs:10](file:///F:/Private/empire-at-war/Assets/Scripts/Components/Utils/Collections/CollectionUtility.cs#L10) implements in-place shuffle, then reverses the result. Repository C# text searches found only its declaration; Serena reference lookup returned no references.

**Solution:** remove it in a later cleanup if reflection/external consumers are ruled out. If it is actually needed, name its in-place behavior explicitly, accept a random source when determinism matters, and remove the redundant reversal unless a caller requires that exact seeded permutation.

**Do not:** invent a consumer or move this unused method into a bigger shared library merely to preserve it.

**Future verification:** confirm no reflection/dynamic consumer; review any deterministic-output contract before changing the algorithm.

## L2 — DependencyBuilder exposes unused custom-path state
**Priority:** P3 · **Confidence:** confirmed no located source callers; cleanup candidate.

**Evidence:** [Assets/Scripts/Services/Extensions/DependencyBuilder.cs:19](file:///F:/Private/empire-at-war/Assets/Scripts/Services/Extensions/DependencyBuilder.cs#L19) exposes `BuildPathToFile`, but repository C# search and Serena found no callers. `ConstructName<T>` caches its first computed name in mutable `PathToFile`; current located callers construct a fresh builder for each binding.

**Solution:** remove the unused custom-name branch once external consumers are excluded. Derive the path locally for each bind where practical, instead of retaining a hidden “first type wins” cache. Keep the working prefix/postfix convention and DI binding behavior.

**Why it matters:** the current reuse hazard is latent, not a confirmed runtime bug. No need to replace the entire builder family.

**Future verification:** existing installers resolve identical keys and injection IDs; any newly supported builder reuse must resolve each requested type correctly.

## What is not deprecated
`SceneReference` is actively used by `SceneData`. Its two `[Obsolete(..., true)]` annotations protect editor/serialization callbacks from runtime calls; they do not deprecate the class. `KillExt`, random-value wrappers, Visitor and ChainOfResponsibility all have located callers. A “v2” suffix is not evidence of dead code.
