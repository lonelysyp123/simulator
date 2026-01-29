import subprocess
import os
import platform
import time
from concurrent.futures import ThreadPoolExecutor

# Configurations for each instance (customize as needed)
INSTANCES = [
    {
        "name": "ESS_Instance_1",
        "args": {
            "--iedmode1": "./iedmodel/emumodel.cfg",
            "--simmodefile1": "./PointMap/CSV/bigEss/emu.csv",
            "--iedmode2": "./iedmodel/bmsmodel.cfg",
            "--simmodefile2": "./PointMap/CSV/bigEss/bms.csv",
            "--iedmode3": "./iedmodel/pcsmodel.cfg",
            "--simmodefile3": "./PointMap/CSV/bigEss/pcsgoose.csv",
            "--esscount": "1",
            "--essstartport": "1100",
        },
    },
    {
        "name": "ESS_Instance_2",
        "args": {
            "--iedmode1": "./iedmodel/emumodel.cfg",  # Different configs
            "--simmodefile1": "./PointMap/CSV/bigEss/emu.csv",
            "--iedmode2": "./iedmodel/bmsmodel.cfg",
            "--simmodefile2": "./PointMap/CSV/bigEss/bms.csv",
            "--iedmode3": "./iedmodel/pcsmodel.cfg",
            "--simmodefile3": "./PointMap/CSV/bigEss/pcsgoose.csv",
            "--esscount": "1",
            "--essstartport": "1200",  # Different port
        },
    },
    # Add more instances as needed
]

def run_instance(instance):
    """Run a single instance of iedsim.exe with given args."""
    name = instance["name"]
    args = instance["args"]

    # Build the command
    cmd = ["iedsim.exe"] if platform.system() == "Windows" else ["./iedsim.exe"]
    for arg, value in args.items():
        cmd.extend([arg, value])

    print(f"🚀 Starting {name}...")
    print("Command:", " ".join(cmd))

    # Run the process
    process = subprocess.Popen(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )

    # Monitor output (optional)
    while True:
        output = process.stdout.readline()
        if output == "" and process.poll() is not None:
            break
        if output:
            print(f"[{name}] {output.strip()}")
    
    return process.returncode

def main():
    print("🏁 Starting multi-instance simulation...")
    
    # Run instances in parallel (adjust max_workers as needed)
    with ThreadPoolExecutor(max_workers=len(INSTANCES)) as executor:
        futures = [executor.submit(run_instance, instance) for instance in INSTANCES]
        
        # Wait for all instances to complete
        for future in futures:
            future.result()

    print("✅ All instances completed.")

if __name__ == "__main__":
    main()