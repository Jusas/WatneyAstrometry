#!/usr/bin/env bash

set -euo pipefail

qdb_ver="3"
scriptDir="$(dirname "$0")"
qdb_urls=("https://github.com/Jusas/WatneyAstrometry/releases/download/watneyqdb${qdb_ver}/watneyqdb-00-07-20-v${qdb_ver}.zip"
          "https://github.com/Jusas/WatneyAstrometry/releases/download/watneyqdb${qdb_ver}/watneyqdb-08-09-20-v${qdb_ver}.zip"
          "https://github.com/Jusas/WatneyAstrometry/releases/download/watneyqdb${qdb_ver}/watneyqdb-10-11-20-v${qdb_ver}.zip"
          "https://github.com/Jusas/WatneyAstrometry/releases/download/watneyqdb${qdb_ver}/watneyqdb-12-13-20-v${qdb_ver}.zip")

for url in "${qdb_urls[@]}"; do
  curl -L -sS -o "/tmp/qdb_${qdb_ver}_tmp.zip" "${url}"
  mkdir -p "${scriptDir}/qdb_files"
  unzip "/tmp/qdb_${qdb_ver}_tmp.zip" -d "${scriptDir}/qdb_files"  
done

