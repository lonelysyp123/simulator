@echo off
:: Instance 1
start "ESS_Instance_1" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 1100

:: Instance 2 (adjust args)
start "ESS_Instance_2" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel2.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 1200

:: Instance 3 (adjust args)
start "ESS_Instance_3" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 1300

:: Instance 4 (adjust args)
start "ESS_Instance_4" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 1400

:: Instance 5(adjust args)
start "ESS_Instance_5" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 1500

:: Instance 6 (adjust args)
start "ESS_Instance_6" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 1600

:: Instance 7 (adjust args)
start "ESS_Instance_7" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 1700

:: Instance 8 (adjust args)
start "ESS_Instance_8" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 1800

:: Instance 9 (adjust args)
start "ESS_Instance_9" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 1900

:: Instance 10 (adjust args)
start "ESS_Instance_10" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 2000

:: Instance 11 (adjust args)
start "ESS_Instance_11" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 2100

:: Instance 12 (adjust args)
start "ESS_Instance_12" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 2200

:: Instance 13 (adjust args)
start "ESS_Instance_13" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 2300

:: Instance 14 (adjust args)
start "ESS_Instance_14" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 2400

:: Instance 15 (adjust args)
start "ESS_Instance_15" iedsim.exe ^
    --iedmode1 ./iedmodel/emumodel.cfg ^
    --simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
    --iedmode2 ./iedmodel/bmsmodel.cfg ^
    --simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
    --iedmode3 ./iedmodel/pcsmodel.cfg ^
    --simmodefile3 ./PointMap/CSV/bigEss/pcsgoose.csv ^
    --esscount 1 ^
    --essstartport 2500

timeout /t 5 > nul
echo All instances launched.