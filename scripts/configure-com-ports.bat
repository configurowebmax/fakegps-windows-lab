@echo off
setlocal
set "SETUPC=%ProgramFiles%\com0com\setupc.exe"
if not exist "%SETUPC%" set "SETUPC=%ProgramFiles(x86)%\com0com\setupc.exe"
if not exist "%SETUPC%" (
  echo No se encontro setupc.exe. Instala com0com desde una fuente verificada.
  exit /b 1
)
"%SETUPC%" list
"%SETUPC%" change CNCA0 PortName=COM10
"%SETUPC%" change CNCB0 PortName=COM11
"%SETUPC%" install PortName=COM10 PortName=COM11
"%SETUPC%" list
pause
