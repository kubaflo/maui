#!/usr/bin/env bash
set -euo pipefail

gateway_commit=ec7b4d1c3c90743d863d424d92f676719b021a2b
gateway_version=v0.4.30+maui-duplicate-fork-trial
patch_sha256=c8e90077b1dfcbfac3fd69fdafee6dfd57a62a0c7904711857c1a48ac104217e
cache_directory="${XDG_CACHE_HOME:-$HOME/.cache}/maui/gh-aw-mcpg/$gateway_commit-$patch_sha256"
source_directory="$cache_directory/source"
output_directory="${RUNNER_TEMP:?Set RUNNER_TEMP to an absolute build directory.}/issue-duplicate-gateway"
gateway_binary="$output_directory/awmg"

if [[ $# != 0 ]]; then
  echo "This helper only builds the pinned Linux amd64 fork-trial gateway." >&2
  exit 2
fi
if ! command -v go >/dev/null 2>&1; then
  echo "Building the fork-trial gateway requires Go 1.26.4 or automatic toolchain selection." >&2
  exit 127
fi
if [[ "$RUNNER_TEMP" != /* || -L "$output_directory" || -L "$gateway_binary" ||
      -L "$gateway_binary.build" || -L "$cache_directory" || -L "$source_directory" ]]; then
  echo "Refusing a relative output directory or symbolic-link gateway build path." >&2
  exit 1
fi

cd "$(git rev-parse --show-toplevel)"
source .github/scripts/IssueDuplicateBuildEnvironment.sh
gateway_patch="$PWD/.github/scripts/gh-aw-mcpg-0.4.30-fork-trial.patch"
if [[ ! -f "$gateway_patch" || -L "$gateway_patch" ||
      "$(shasum -a 256 "$gateway_patch" | cut -d ' ' -f 1)" != "$patch_sha256" ]]; then
  echo "The reviewed gateway patch is missing or modified; refusing to build." >&2
  exit 1
fi
mkdir -p "$cache_directory" "$output_directory"
if [[ ! -d "$source_directory/.git" ]]; then
  if [[ -e "$source_directory" ]]; then
    echo "Refusing to replace an existing non-repository gateway cache." >&2
    exit 1
  fi
  run_without_tokens git init --quiet "$source_directory"
fi
if ! git -C "$source_directory" rev-parse --verify HEAD >/dev/null 2>&1; then
  run_without_tokens git -C "$source_directory" fetch --quiet --depth 1 \
    https://github.com/github/gh-aw-mcpg.git "$gateway_commit"
  run_without_tokens git -C "$source_directory" checkout --quiet --detach FETCH_HEAD
fi
if [[ "$(git -C "$source_directory" rev-parse HEAD)" != "$gateway_commit" ]]; then
  echo "The gateway cache is not at the pinned official source commit." >&2
  exit 1
fi
if [[ -z "$(git -C "$source_directory" status --porcelain --untracked-files=all --ignored)" ]]; then
  run_without_tokens git -C "$source_directory" apply --check --unidiff-zero \
    --whitespace=error-all "$gateway_patch"
  run_without_tokens git -C "$source_directory" apply --unidiff-zero \
    --whitespace=error-all "$gateway_patch"
fi
source_patch_sha256="$(
  git -C "$source_directory" -c core.quotePath=false --no-pager diff \
    --full-index --binary --no-renames --no-ext-diff --no-textconv --no-color \
    --src-prefix=a/ --dst-prefix=b/ --diff-algorithm=myers --unified=0 HEAD |
    shasum -a 256 | cut -d ' ' -f 1
)"
if [[ "$source_patch_sha256" != "$patch_sha256" ||
      -n "$(git -C "$source_directory" ls-files --others)" ]]; then
  echo "The gateway source differs from the reviewed patch; refusing to build." >&2
  exit 1
fi

(
  cd "$source_directory"
  run_without_tokens GOWORK=off CGO_ENABLED=0 GOOS=linux GOARCH=amd64 \
    go build -trimpath -mod=readonly -p 4 \
    -ldflags "-s -w -X main.Version=$gateway_version" \
    -o "$gateway_binary.build" .
)
chmod 0555 "$gateway_binary.build"
mv -f "$gateway_binary.build" "$gateway_binary"
printf 'Gateway source: %s\nGateway patch: %s\n' "$gateway_commit" "$patch_sha256"
shasum -a 256 "$gateway_binary"
