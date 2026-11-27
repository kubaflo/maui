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
  bash: ["which", "env", "copilot", "ls", "cat", "echo", "bash", "sh", "printenv", "cut", "grep", "sed", "head"]
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
    # GPT-6.2 Sol and MAI Code 2 are served only over the Responses API.
    COPILOT_PROVIDER_WIRE_API: responses
    # The candidates run dotnet in parallel worktrees; skip first-run setup and telemetry.
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE: "1"
    DOTNET_NOLOGO: "1"
    DOTNET_CLI_TELEMETRY_OPTOUT: "1"
    COPILOT_GITHUB_TOKEN: ${{ secrets.COPILOT_PAT_0 }}

timeout-minutes: 20
---

Run these bash commands one at a time and report each exact output in your final answer:

1. `which copilot`
2. `printenv | grep -E '^COPILOT_(PROVIDER|MODEL)' | cut -d= -f1`
3. `env COPILOT_PROVIDER_WIRE_API=completions copilot --model claude-opus-5.5 --allow-all-tools -p "Reply with exactly: OPUS-OK"`
4. `env COPILOT_PROVIDER_WIRE_API=completions COPILOT_MODEL=claude-opus-5.5 copilot --allow-all-tools -p "Reply with exactly: OPUS-OK"`

Do not do anything else.
