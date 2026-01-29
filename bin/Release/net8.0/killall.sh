#!/bin/bash

# Script: kill_iedsim.sh
# Description: Kills all iedsim.exe processes gracefully

# Find all iedsim.exe processes and kill them
echo "Searching for iedsim.exe processes..."
pids=$(pgrep -f "iedsim.exe")

if [ -z "$pids" ]; then
    echo "No iedsim.exe processes found."
    exit 0
fi

echo "Found iedsim.exe processes (PIDs): $pids"
echo "Attempting to terminate gracefully..."

# Send SIGTERM first (allows graceful shutdown)
kill -15 $pids 2>/dev/null

# Wait a few seconds for processes to exit
sleep 3

# Check if any processes remain and force-kill if necessary
remaining_pids=$(pgrep -f "iedsim.exe")
if [ -n "$remaining_pids" ]; then
    echo "Some processes did not exit gracefully. Force-killing..."
    kill -9 $remaining_pids 2>/dev/null
fi

# Final check
if pgrep -f "iedsim.exe" >/dev/null; then
    echo "Failed to kill all processes."
else
    echo "All iedsim.exe processes terminated."
fi