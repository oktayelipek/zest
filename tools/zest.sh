#!/usr/bin/env bash
# Zest quick launcher. Builds the Debug config and opens the game in Godot.
# Called by the `zest` shell function installed in ~/.zshrc.
set -e

REPO="/Users/btcyz456/Desktop/Zest/repo"
GODOT="/Users/btcyz456/Downloads/Godot_mono.app/Contents/MacOS/Godot"
export PATH="/usr/local/share/dotnet:$PATH"

case "${1:-run}" in
  run)
    dotnet build "$REPO/Zest.sln" --nologo >/dev/null
    exec "$GODOT" --path "$REPO/game"
    ;;
  headless)
    dotnet build "$REPO/Zest.sln" --nologo >/dev/null
    exec "$GODOT" --path "$REPO/game" --headless --quit-after "${2:-10}"
    ;;
  test)
    exec dotnet test "$REPO/Zest.sln" --nologo -c Debug
    ;;
  build)
    exec dotnet build "$REPO/Zest.sln" --nologo
    ;;
  *)
    echo "usage: zest [run|headless [sec]|test|build]"
    exit 1
    ;;
esac
