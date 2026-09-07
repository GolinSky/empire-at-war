# Sol High multi-agent setup

## Recommended role mapping

| Agent | Model | Effort | Purpose |
|---|---|---:|---|
| context_curator | gpt-5.6-luna | low | Narrow retrieval and compression |
| code_explorer | gpt-5.6-luna | medium | Serena-backed code-path and impact mapping |
| unity_architect | gpt-5.6-sol | high | Ambiguous architecture and migration decisions |
| csharp_worker | gpt-5.6-terra | high | Bounded production C# implementation |
| unity_operator | gpt-5.6-terra | medium | Stateful Unity mutations and verification |
| unity_profiler | gpt-5.6-terra | high | Performance evidence and competing hypotheses |
| unity_reviewer | gpt-5.6-terra | high | Lifecycle, async, serialization, and regression review |
| unity_test_runner | gpt-5.6-luna | low | User-authorized, bounded validation work |

## Operational rules

- Use one writer at a time for overlapping Unity/C# scope.
- Run read-only roles in parallel only when their scopes are genuinely independent.
- Keep the parent/orchestrator on GPT-5.6 Sol High as the final decision-maker.
- Each child has `[agents] enabled = false` so it cannot recursively create more subagents.
- Graphify remains disabled by repository policy; `code_explorer` uses Serena and targeted source inspection.
- Automated tests may run only when the user explicitly requests them.
