#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [[ -z "$VERSION" ]]; then
  echo "Usage: $0 <version>  (e.g. 1.202.261430)" >&2
  exit 1
fi

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

# AssemblyVersion / FileVersion need 4 parts
VERSION4="${VERSION}.0"

PROJECTS=(
  "ForgeRunner/ForgeRunner.csproj"
  "SMLCore/SMLCore.csproj"
  "SMSCore/SMSCore.csproj"
)

for proj in "${PROJECTS[@]}"; do
  FILE="$REPO_ROOT/$proj"
  sed -i '' \
    -e "s|<Version>.*</Version>|<Version>${VERSION}</Version>|" \
    -e "s|<AssemblyVersion>.*</AssemblyVersion>|<AssemblyVersion>${VERSION4}</AssemblyVersion>|" \
    -e "s|<FileVersion>.*</FileVersion>|<FileVersion>${VERSION4}</FileVersion>|" \
    -e "s|<InformationalVersion>.*</InformationalVersion>|<InformationalVersion>${VERSION}</InformationalVersion>|" \
    "$FILE"
  echo "  $proj -> $VERSION"
done
