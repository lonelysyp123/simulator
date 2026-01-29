#!/bin/bash

#Instance 1
/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS001model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS002model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --nogui false \
    --esscount 1 \
    --essstartport 1100 &

# Instance 2 (adjust args)
/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS003model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS004model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --nogui false \
    --esscount 1 \
    --essstartport 1200 &
# Instance 3 (adjust args)
/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS005model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS006model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --nogui false \
    --esscount 1 \
    --essstartport 1300 &

# Instance 4 (adjust args)
/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS007model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS008model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --nogui false \
    --esscount 1 \
    --essstartport 1400 &

# Instance 5 (adjust args)
/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS009model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS010model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --nogui  false \
    --esscount 1 \
    --essstartport 1500 &

/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS011model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS012model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --nogui false \
    --esscount 1 \
    --essstartport 1600 &

# Instance 2 (adjust args)
/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS013model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS014model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --nogui false \
    --esscount 1 \
    --essstartport 1700 &

/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS015model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS016model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --ngui false \
    --esscount 1 \
    --essstartport 1800 &

/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS017model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS018model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --ngui false \
    --esscount 1 \
    --essstartport 1900 &

/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS019model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS020model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --ngui false \
    --esscount 1 \
    --essstartport 2000 &

/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS021model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS022model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --ngui false \
    --esscount 1 \
    --essstartport 2100 &

/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS023model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS024model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --ngui false \
    --esscount 1 \
    --essstartport 2200 &

/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS025model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS026model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --ngui false \
    --esscount 1 \
    --essstartport 2300 &

/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS027model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS028model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --ngui false \
    --esscount 1 \
    --essstartport 2400 &

/usr/bin/dotnet ./Iedsim.dll \
    --iedmode1 ./iedmodel/emumodel.cfg \
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv \
    --iedmode2 ./iedmodel/bmsmodel.cfg \
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv \
    --simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \
    --iedmode3 ./iedmodel/PCS029model.cfg         \
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \
    --iedmode4 ./iedmodel/PCS030model.cfg         \
    --simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \
    --gooseIf ens16f2 \
    --ngui false \
    --esscount 1 \
    --essstartport 2500 &


wait
echo "All instances completed."
