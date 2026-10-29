---
type: index
system:
  - documentation
updated: 2026-10-29
---
# Note Flags

## Rules

- Vocabulary, context and removal rules → [[Rules/DOCUMENTATION_ORGANIZATION#Note Flags]]. Flags describe attention needs; they do not establish current policy or plan status.
- Views below use native Obsidian Search; no manually maintained list. Empty views are valid. Topic tags such as `optimization` are separate from `flag/*`.
- Agent discovery: `search_notes` with a literal flag and `searchFrontmatter: true`; inspect returned notes' actual `tags`. Change tags with `manage_tags`, preserving unrelated metadata.

## Implementation

### Important

```query
tag:flag/important
```

### Revisit

```query
tag:flag/revisit
```

### Issues

```query
tag:flag/issue
```

### Broken

```query
tag:flag/broken
```

### Delayed

```query
tag:flag/delayed
```

### All marked notes

```query
tag:flag
```

### Important notes needing review

```query
tag:flag/important tag:flag/revisit
```

### Action needed

```query
tag:flag/revisit OR tag:flag/issue OR tag:flag/broken OR tag:flag/delayed
```

## Files

- [[00_Home]]
- [[TODOs/TODOs]] — execution ownership.
- [Obsidian tags](https://obsidian.md/help/tags) — list property and nested tag matching.
- [Obsidian Search](https://obsidian.md/help/plugins/search) — operators and embedded query blocks.
