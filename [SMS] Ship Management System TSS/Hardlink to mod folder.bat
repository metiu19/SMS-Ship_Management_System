@ECHO off

SET modFolder=%%APPDATA%%\SpaceEngineers\Mods\[SMS] Ship Management System\Data\Scripts\SMS

for %%f in (.\*.cs) do (
cmd /c mklink /H "%modFolder%\%%~nxf" "%%~ff"
)

for /f "tokens=*" %%a in ('dir .\ /b /a /ad ^|find ^"bin^" /v /i ^|find ^"obj^" /v /i ^|find ^"properties^" /v /i') do (
cmd /c mkdir "%modFolder%\%%a"
for %%f in (%%a\*.cs) do (
cmd /c mklink /H "%modFolder%\%%a\%%~nxf" "%%~ff"
)
)