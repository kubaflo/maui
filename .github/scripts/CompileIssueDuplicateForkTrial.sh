#!/usr/bin/env bash
set -euo pipefail

compiler_commit=c35393777e5604a63721d09512263b1383301d4f
compiler_version=v0.89.21+maui-duplicate-fork-trial
patch_sha256=4a9eed5b4fe472cb0150b0af7778c9195ea04ca757196945470643c5d6c4d265
runtime_commit=924af5fdc64061cfbf66fb584c8b07e2ac230c60
cache_directory="${XDG_CACHE_HOME:-$HOME/.cache}/maui/gh-aw/$compiler_commit-$patch_sha256"
source_directory="$cache_directory/source"
compiler_binary="$cache_directory/gh-aw"

if [[ $# != 0 ]]; then
  echo "This helper only compiles the fixed, strict fork trial." >&2
  exit 2
fi
if ! command -v go >/dev/null 2>&1; then
  echo "Compiling the fork trial requires Go 1.26.8 or Go automatic toolchain selection." >&2
  exit 127
fi

cd "$(git rev-parse --show-toplevel)"
source .github/scripts/IssueDuplicateBuildEnvironment.sh
compiler_patch="$PWD/.github/scripts/gh-aw-0.89.21-fork-trial.patch"
if [[ ! -f "$compiler_patch" || -L "$compiler_patch" ||
      "$(shasum -a 256 "$compiler_patch" | cut -d ' ' -f 1)" != "$patch_sha256" ]]; then
  echo "The reviewed compiler patch is missing or modified; refusing to compile." >&2
  exit 1
fi
if [[ -L "$cache_directory" || -L "$source_directory" || -L "$compiler_binary" ]]; then
  echo "Refusing to use a symbolic-link compiler cache." >&2
  exit 1
fi
mkdir -p "$cache_directory"
if [[ ! -d "$source_directory/.git" ]]; then
  if [[ -e "$source_directory" ]]; then
    echo "Refusing to replace an existing non-repository compiler cache." >&2
    exit 1
  fi
  run_without_tokens git init --quiet "$source_directory"
fi
if ! git -C "$source_directory" rev-parse --verify HEAD >/dev/null 2>&1; then
  run_without_tokens git -C "$source_directory" fetch --quiet --depth 1 \
    https://github.com/github/gh-aw.git "$compiler_commit"
  run_without_tokens git -C "$source_directory" checkout --quiet --detach FETCH_HEAD
fi
if [[ "$(git -C "$source_directory" rev-parse HEAD)" != "$compiler_commit" ]]; then
  echo "The compiler cache is not at the pinned official source commit." >&2
  exit 1
fi
if [[ -z "$(git -C "$source_directory" status --porcelain --untracked-files=all --ignored)" ]]; then
  run_without_tokens git -C "$source_directory" apply --check --unidiff-zero \
    --whitespace=error-all "$compiler_patch"
  run_without_tokens git -C "$source_directory" apply --unidiff-zero \
    --whitespace=error-all "$compiler_patch"
fi

# Verify the complete source delta, not just whether patched hunks are present.
source_patch_sha256="$(
  git -C "$source_directory" -c core.quotePath=false --no-pager diff \
    --full-index --binary --no-renames --no-ext-diff --no-textconv --no-color \
    --src-prefix=a/ --dst-prefix=b/ --diff-algorithm=myers --unified=0 HEAD |
    shasum -a 256 | cut -d ' ' -f 1
)"
if [[ "$source_patch_sha256" != "$patch_sha256" ||
      -n "$(git -C "$source_directory" ls-files --others)" ]]; then
  echo "The compiler source differs from the reviewed patch; refusing to compile." >&2
  exit 1
fi

(
  cd "$source_directory"
  run_without_tokens GOWORK=off go build -mod=readonly -p 4 \
    -ldflags "-X main.version=$compiler_version -X main.isRelease=true" \
    -o "$compiler_binary" ./cmd/gh-aw
)

run_without_tokens "$compiler_binary" compile daily-repo-status \
  --strict --validate --no-check-update \
  --action-mode action --action-tag "$runtime_commit"
