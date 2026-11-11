#!/bin/bash
cd "$(dirname "$0")/../.." || exit 1
export PATH="$HOME/flutter/bin:/opt/homebrew/bin:/usr/local/bin:/usr/local/share/dotnet:$PATH"
exec python3 "tools/dev-launcher/launcher.py"
