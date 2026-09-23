# Graphify Guide for AI Agents and Developers

This guide explains how to install, configure, and use **Graphify** in this project to navigate its structure and assist AI coding agents (such as Claude Code, Cursor, Aider, and Antigravity) with token-efficient codebase querying.

---

## 1. What is Graphify?
**Graphify** is a local AST and semantic parser that transforms the project directory (C# source files, scene configurations, and playbooks) into a queryable semantic knowledge graph. It outputs its files to `graphify-out/`, including:
* **GRAPH_REPORT.md**: A high-level plain-text overview of the code's modules, communities, and dependency flows.
* **`graph.json`**: The full JSON representation of the graph (approx. 112 MB) used for querying.
* **`graphify-out/memory/`**: Cache files and markdown summaries of queries ran against the graph.

---

## 2. Installation & Setup

Install via `uv` (Recommended):
```bash
uv tool install graphifyy
```

Install via `pip`:
```bash
pip install graphifyy
```

---

## 3. Integrating with AI Agents

AI coding agents use Graphify to query relationships in the codebase without reading hundreds of raw files.

| Agent / Editor | CLI Integration Command | What it configures |
| --- | --- | --- |
| **Antigravity / Gemini** | `graphify install --platform antigravity` | Configures `.agent/rules` and workspace workflows |
| **Cursor** | `graphify install --platform cursor` | Creates `.cursor/rules/graphify.mdc` |
| **Claude Code** | `graphify install --platform claude` | Updates `CLAUDE.md` and hooks into pre-tool execution |
| **Codex / Aider** | `graphify install --platform codex` | Appends context capabilities to [[../Rules/AGENTS|AGENTS.md]] |

---

## 4. Rebuilding & Updating the Graph

Fast Offline Rebuild:
```bash
graphify update .
```

Full Semantic Re-Extraction:
```bash
graphify extract .
```

---

## 5. CLI Query Tools

- Ask a question: `graphify query "How does selecting player ships open shipui?"`
- Find path: `graphify path "ShipAIBrain" "ShipController"`
- Explain component: `graphify explain "EconomyController"`

---

## 6. How it helps in Refactoring
As outlined in [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]]:
1. Reference `GRAPH_REPORT.md` to inspect the current feature's community.
2. Run `graphify path` or `graphify query` to locate affected models and views.
3. Verify that your refactor did not break dependency rules.
