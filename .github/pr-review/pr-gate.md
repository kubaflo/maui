# PR Gate — Test Verification

> **⛔ This phase MUST pass before continuing to Try-Fix. If it fails, stop and inform user.**

> 🚨 Gate verification MUST run via task agent — never inline.

---

## Prerequisites

- Pre-Flight phase must be ✅ COMPLETE before starting
- Platform must be selected (affected by bug AND available on host)

### Platform Selection

Choose a platform that is BOTH affected by the bug AND available on the current host:

| Host OS | Available Platforms |
|---------|---------------------|
| Windows | Android, Windows |
| macOS | Android, iOS, MacCatalyst |

⚠️ Do NOT test on a platform unaffected by the bug — the test will pass regardless.

---

## Steps

1. **Check if tests exist** using the shared detection script:
   ```bash
   pwsh .github/scripts/shared/Detect-TestsInDiff.ps1 -PRNumber XXXXX
   ```
   If NO tests detected → inform user, suggest `write-tests-agent`. Gate is ⚠️ SKIPPED.
   
   The script auto-detects all test types: UI tests, device tests, unit tests, XAML tests.

2. **Select platform** — must be affected by bug AND available on host (see Platform Selection above).
   Note: Unit tests and XAML tests don't require a platform.

3. **Run verification via task agent** (MUST use task agent — never inline):
   ```
   Invoke the `task` agent with this prompt:

   "Invoke the verify-tests-fail-without-fix skill for this PR:
   - Platform: {platform}  (omit for unit/XAML tests)
   - RequireFullVerification: true

   Report back: Did tests FAIL without fix? Did tests PASS with fix? Final status?"
   ```

**Why task agent?** Running inline allows substituting commands and fabricating results. Task agent runs in isolation.

---

## Expected Result

```
╔═══════════════════════════════════════════════════════════╗
║              VERIFICATION PASSED ✅                       ║
╠═══════════════════════════════════════════════════════════╣
║  - FAIL without fix (as expected)                         ║
║  - PASS with fix (as expected)                            ║
╚═══════════════════════════════════════════════════════════╝
```

---

## If Gate Fails

- **Tests PASS without fix** → Tests don't catch the bug. Inform user, suggest `write-tests-agent`.
- **Tests FAIL with fix** → PR's fix doesn't work. Skip Try-Fix, proceed to Report with ⚠️ REQUEST CHANGES.

---

## Output File

```bash
mkdir -p CustomAgentLogsTmp/PRState/{PRNumber}/PRAgent/gate
```

Write `content.md`:
```markdown
### Gate Result: {✅ PASSED / ❌ FAILED / ⚠️ SKIPPED}

**Platform:** {platform}
**Mode:** Full Verification

- Tests FAIL without fix: {✅/❌}
- Tests PASS with fix: {✅/❌}
```

---

## Common Mistakes

- ❌ Running inline — MUST use task agent
- ❌ Using `BuildAndRunHostApp.ps1` — that runs ONE direction; the skill does TWO
- ❌ Claiming results from a single test run — script does TWO runs automatically
