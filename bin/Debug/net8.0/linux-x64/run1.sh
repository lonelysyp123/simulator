#!/bin/bash
# Instance 1
/usr/bin/dotnet ./Iedsim.dll \
--iedmode1 ./iedmodel/emumodel.cfg \ 
--simmodefile1 ./PointMap/CSV/bigEss/emu.csv \ 
--iedmode2 ./iedmodel/bmsmodel.cfg  \
--simmodefile2 ./PointMap/CSV/bigEss/bms.csv \   
--simmodefile21 ./PointMap/CSV/bigEss/bms1.csv \ 
--iedmode3 ./iedmodel/PCS012model.cfg  \
--simmodefile3 ./PointMap/CSV/bigEss/pcsgoose1.csv \ 
--iedmode4 ./iedmodel/PCS013model.cfg \
--simmodefile4 ./PointMap/CSV/bigEss/pcsgoose2.csv \ 
--nogui true \
--gooseIf ens16f2 \
--esscount 1 \
--essstartport 1100

wait
echo "All instances completed."