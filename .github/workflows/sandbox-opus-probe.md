---
description: Sandbox probe - can the agent run a nested Copilot CLI with Claude over chat completions?
on:
  workflow_dispatch:
permissions:
  contents: read
imports:
  - shared/gpt-6.2-sol.md
environment: copilot-pat-pool
tools:
  bash: ["echo"]
network:
  allowed: [defaults, github]
model: gpt-6.2-sol
models:
  allowed: ["gpt-6.2-sol", "copilot/gpt-6.2-sol", "openai/gpt-6.2-sol", "github-copilot/gpt-6.2-sol",
            "mai-code-2", "copilot/mai-code-2", "github-copilot/mai-code-2", "MAI Code 2",
            "claude-opus-5.5", "copilot/claude-opus-5.5", "github-copilot/claude-opus-5.5", "Claude Opus 5.5"]
  # The sandbox catalog does not list the sub-agent models; without these entries the fixer sub-agents
  # cannot resolve their model ids. Costs are used only for run accounting.
  providers:
    github-copilot:
      models:
        mai-code-2:
          cost:
            input: "2e-06"
            output: "1e-05"
            cache_read: "1e-07"
            cache_write: "2.5e-06"
        claude-opus-5.5:
          cost:
            input: "5e-06"
            output: "2.5e-05"
            cache_read: "5e-07"
            cache_write: "6.25e-06"
engine:
  id: copilot
  env:
    # The candidates run dotnet in parallel worktrees; skip first-run setup and telemetry.
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE: "1"
    DOTNET_NOLOGO: "1"
    DOTNET_CLI_TELEMETRY_OPTOUT: "1"
    COPILOT_GITHUB_TOKEN: ${{ secrets.COPILOT_PAT_0 }}

timeout-minutes: 20
---

Delegate to the `probe-mai` agent, then to the `probe-opus` agent. Report each agent's exact reply, or its exact
error, in your final answer. Then reply yourself with `SOL-OK`. Do nothing else.

## agent: `probe-mai`
---
description: MAI probe
model: mai-code-2
---

Reply with exactly: MAI-OK

## agent: `probe-opus`
---
description: Opus probe
model: claude-opus-5.5
---

Reply with exactly: OPUS-OK
