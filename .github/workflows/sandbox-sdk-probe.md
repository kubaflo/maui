---
description: Sandbox probe - is the repository SDK visible to the agent?
on:
  workflow_dispatch:
permissions:
  contents: read
imports:
  - shared/gpt-6.2-sol.md
environment: copilot-pat-pool
model: gpt-6.2-sol
models:
  allowed: ["gpt-6.2-sol", "copilot/gpt-6.2-sol", "openai/gpt-6.2-sol", "github-copilot/gpt-6.2-sol",
            "mai-code-2", "copilot/mai-code-2", "github-copilot/mai-code-2", "MAI Code 2"]
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

timeout-minutes: 30
tools:
  bash: ["dotnet", "ls", "cat", "pwd", "mkdir", "cp", "echo"]
steps:
  - name: Write main's global.json
    run: |
      cat > global.json <<'JSON'
      {
        "tools": {
          "dotnet": "12.0.100-alpha.1.26523.103"
        },
        "sdk": {
          "paths": [
            ".dotnet",
            "$host$"
          ]
        },
        "msbuild-sdks": {
          "MSBuild.Sdk.Extras": "3.0.44",
          "Microsoft.Build.NoTargets": "3.7.0",
          "Microsoft.DotNet.Arcade.Sdk": "11.0.0-beta.26566.116",
          "Microsoft.DotNet.Helix.Sdk": "11.0.0-beta.26566.116"
        }
      }
      JSON
  - name: Install the repository's .NET SDK
    run: |
      set -euo pipefail
      VERSION="$(jq -r '.tools.dotnet' global.json)"
      DIR="${RUNNER_TOOL_CACHE}/maui-dotnet/x64/bin"
      curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
      bash /tmp/dotnet-install.sh --version "${VERSION}" --install-dir "${DIR}" --no-path
      "${DIR}/dotnet" --list-sdks
      mkdir -p /tmp/gh-aw/autopilot/wt && cp global.json /tmp/gh-aw/autopilot/wt/
network:
  allowed: [defaults]
---

Run each command and report its exact output: `dotnet --version` (in the workspace), `dotnet --list-sdks`, and
`cd /tmp/gh-aw/autopilot/wt && dotnet --version`. Do nothing else.
