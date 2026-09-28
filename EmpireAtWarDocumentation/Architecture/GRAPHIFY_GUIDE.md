# Graphify Guide for AI Agents and Developers

- Graphify → codebase relationships and dependency queries.
- Agent clients: Claude Code, Cursor, Aider, Antigravity.

## 1. Graphify outputs

- Local AST + semantic parser.
- Inputs: C# source, scene configurations, playbooks.
- Output folder: `graphify-out/`.
- `GRAPH_REPORT.md` → modules, communities, dependency flows.
- `graph.json` → full graph; ≈ 112 MB.
- `graphify-out/memory/` → query caches and Markdown summaries.

## 2. Installation & Setup

- Install via `uv` (Recommended):

```bash
uv tool install graphifyy
```

- Install via `pip`:

```bash
pip install graphifyy
```

## 3. Integrating with AI Agents

- Graph queries locate relationships before targeted source reads.

| Agent / Editor | CLI Integration Command | What it configures |
| --- | --- | --- |
| **Antigravity / Gemini** | `graphify install --platform antigravity` | Configures `.agent/rules` and workspace workflows |
| **Cursor** | `graphify install --platform cursor` | Creates `.cursor/rules/graphify.mdc` |
| **Claude Code** | `graphify install --platform claude` | Updates `CLAUDE.md` and hooks into pre-tool execution |
| **Codex / Aider** | `graphify install --platform codex` | Appends context capabilities to [[../Rules/AGENTS|AGENTS.md]] |

## 4. Rebuilding & Updating the Graph

- Fast Offline Rebuild:

```bash
graphify update .
```

- Full Semantic Re-Extraction:

```bash
graphify extract .
```

## 5. CLI Query Tools

- Ask a question: `graphify query "How does selecting player ships open shipui?"`
- Find path: `graphify path "ShipAIBrain" "ShipController"`
- Explain component: `graphify explain "EconomyController"`

## 6. How it helps in Refactoring

- As outlined in [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]]:
1. Reference `GRAPH_REPORT.md` to inspect the current feature's community.
2. Run `graphify path` or `graphify query` to locate affected models and views.
3. Verify that your refactor did not break dependency rules.
