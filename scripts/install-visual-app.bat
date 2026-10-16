@echo off
setlocal
set "ROOT=%~dp0.."
set "DEST=%ProgramFiles%\FakeGPS"
if not exist "%DEST%" mkdir "%DEST%"

if not exist "%ROOT%\FakeGPS-Visual.exe" (
  echo Falta FakeGPS-Visual.exe. Construye la aplicacion antes de instalarla.
  exit /b 1
)
copy /y "%ROOT%\FakeGPS-Visual.exe" "%DEST%\FakeGPS-Visual.exe" >nul
if exist "%ROOT%\release\FakeGPS.exe" copy /y "%ROOT%\release\FakeGPS.exe" "%DEST%\FakeGPS.exe" >nul
if exist "%ROOT%\scripts\apply-windows-location.bat" copy /y "%ROOT%\scripts\apply-windows-location.bat" "%DEST%\apply-windows-location.bat" >nul
echo Aplicacion instalada en "%DEST%".
pause
