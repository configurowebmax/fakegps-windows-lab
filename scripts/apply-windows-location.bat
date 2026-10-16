@echo off
setlocal
if "%~2"=="" (
  echo Uso: apply-windows-location.bat LATITUD LONGITUD
  exit /b 2
)
set "CLI=%~dp0FakeGPS.exe"
if not exist "%CLI%" set "CLI=%ProgramFiles%\FakeGPS\FakeGPS.exe"
if not exist "%CLI%" (
  echo No se encontro FakeGPS.exe.
  exit /b 3
)
"%CLI%" -s "%~1,%~2"
if errorlevel 1 exit /b 4
powershell -NoProfile -ExecutionPolicy Bypass -Command "$dev=Get-PnpDevice -FriendlyName '*FakeGPS*' -ErrorAction SilentlyContinue; if ($dev) { Disable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false; Start-Sleep -Seconds 2; Enable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false }"
echo Ubicacion aplicada: %~1,%~2
