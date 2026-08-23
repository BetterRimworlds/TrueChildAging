#!/bin/bash
set -euo pipefail

dir="./Source"
MOD=$(basename "$PWD")
solutionPath="Source/${MOD}.sln"
configurations=("v1.4" "v1.5" "v1.6")

dotnet restore "$solutionPath"
dotnet test Tests/TrueChildAging.Tests.csproj --nologo

function sync_mod() {
    rsync -a "${MOD}" /rimworld/1.2/Mods/

    cp README.md /rimworld/1.2/Mods/${MOD}
    if command -v unix2dos >/dev/null 2>&1; then
        unix2dos /rimworld/1.2/Mods/${MOD}/README.md
    fi

    rm -rf /rimworld/1.4/Mods/${MOD}
    rm -rf /rimworld/1.5/Mods/${MOD}
    rm -rf /rimworld/1.6/Mods/${MOD}
    rm -rf /rimworld/1.6-steam/Mods/${MOD}

    cp -af /rimworld/1.2/Mods/${MOD} /rimworld/1.4/Mods
    cp -af /rimworld/1.2/Mods/${MOD} /rimworld/1.5/Mods
    cp -af /rimworld/1.2/Mods/${MOD} /rimworld/1.6/Mods
    cp -af /rimworld/1.2/Mods/${MOD} /rimworld/1.6-steam/Mods
}

function build() {
    rm -rf /rimworld/1.2/Mods/${MOD}

    local pids=()
    for config in "${configurations[@]}"; do
        echo "Building for configuration: Release $config"
        dotnet build --no-restore "$solutionPath" --configuration "Release $config" &
        pids+=($!)
    done

    local failed=0
    for pid in "${pids[@]}"; do
        wait "$pid" || { echo "Build failed (PID $pid)"; failed=1; }
    done

    if [[ $failed -eq 1 ]]; then
        echo "One or more builds failed. Aborting sync."
        return 1
    fi

    sync_mod
    echo "All builds completed!"
}

build || exit 1

if [ "${1:-}" == "1" ]; then
    echo "Done"
    exit 0
fi
