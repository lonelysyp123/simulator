@echo off
:: Instance 1
start "ESS_Instance_1" Iedsim.exe ^
--iedmode1 ./iedmodel/emumodel.cfg ^
--simmodefile1 ./PointMap/CSV/bigEss/emu.csv ^
--iedmode2 ./iedmodel/bmsmodel.cfg ^
--simmodefile2 ./PointMap/CSV/bigEss/bms.csv ^
--simmodefile21 ./PointMap/CSV/bigEss/bms1.csv ^
--iedmode3 ./iedmodel/PCS012model.cfg ^
--simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv ^
--iedmode4 ./iedmodel/PCS013model.cfg ^
--simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv ^
--nogui true ^
--gooseIf 2 ^
--esscount 1 ^
--essstartport 5600 ^


timeout /t 5 > nul
echo All instances launched.