#!/usr/bin/env bash

set -euo pipefail

target_arch="$1"
watney_ver="1.2.3"
scriptDir="$(dirname "$0")"
valid_archs=("linux-x64" "linux-arm64" "linux-arm" "osx-x64" "win-x64")

if [[ ! " ${valid_archs[*]} " =~ [[:space:]]${target_arch}[[:space:]] ]]; then
  echo "Expected one argument, the target architecture which is one of: linux-x64, linux-arm64, linux-arm, osx-x64, win-x64"
  exit 1;
fi

curl -L -sS -o "/tmp/watney_${watney_ver}.tgz" "https://github.com/Jusas/WatneyAstrometry/releases/download/v${watney_ver}/watney-solve-cli-${watney_ver}-linux-x64.tar.gz"
tar xf "/tmp/watney_${watney_ver}.tgz" -C "${scriptDir}"

# Make a config for v3 qdb
sed -i "s,^quadDbPath.*,quadDbPath: './../../../quad_dbs/v3/qdb_files'," "${scriptDir}/watney-cli/watney-solve-config.yml"
cp "${scriptDir}/watney-cli/watney-solve-config.yml" "${scriptDir}/watney-cli/config-qdb-v3.yml"
