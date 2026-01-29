#!/bin/bash
echo "Searching for dotnet processes..."
pids=$(pgrep -f "dotnet")
if [ -z "$pids" ]; then
    echo "No dotnet processes found."
    exit 0
fi

echo "Found dotnet processes (PIDs): $pids"
echo "Attempting to terminate gracefully..."
# Send SIGTERM first (allows graceful shutdown)
kill -15 $pids 2>/dev/null
# Wait a few seconds for processes to exit
sleep 3
# Check if any processes remain and force-kill if necessary
remaining_pids=$(pgrep -f "dotnet")
if [ -n "$remaining_pids" ]; then
    echo "Some processes did not exit gracefully. Force-killing..."
    kill -9 $remaining_pids 2>/dev/null
fi
echo "kill all dotnet"
