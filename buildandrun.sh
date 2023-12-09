#!/usr/bin/env bash
cd "$(dirname "$(readlink -fn "$0")")"
dotnet build
dotnet ./bin/Debug/net8.0/Library-Stardust.dll $@
