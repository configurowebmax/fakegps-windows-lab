@echo off
setlocal
set "NMEA=%~dp0..\release\NmeaGPS.exe"
if not exist "%NMEA%" (
  echo Falta release\NmeaGPS.exe. Construye la herramienta antes de usarla.
  exit /b 1
)
set /p LAT=Latitud: 
set /p LON=Longitud: 
"%NMEA%" --port COM10 --lat %LAT% --lon %LON% --baud 4800
