#!/usr/bin/env bash
cd "$(dirname "$(readlink -fn "$0")")" || exit
dotnet build
cd "$(dirname "$(readlink -fn "$0")")/bin/Debug/net8.0/" || exit
dotnet ./Library-Stardust.dll "$@" -dir "$(dirname "$(readlink -fn "$0")")"
