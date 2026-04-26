@echo off
cd /d "C:\Users\Administrator\Downloads\Autoplay\SFSMod\SFSControl"
dotnet build -c Release
if %ERRORLEVEL% NEQ 0 exit
move /Y ".\bin\Release\net48\SFSControl.dll" "C:\Program Files (x86)\Steam\steamapps\common\Spaceflight Simulator\Spaceflight Simulator Game\Mods\SFSControl.dll"
start "" "C:\Program Files (x86)\Steam\steamapps\common\Spaceflight Simulator\Spaceflight Simulator Game\Spaceflight Simulator.exe"