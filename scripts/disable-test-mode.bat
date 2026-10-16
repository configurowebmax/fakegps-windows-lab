@echo off
net session >nul 2>&1
if not %errorlevel%==0 (
  echo Ejecuta este script como Administrador.
  pause
  exit /b 1
)
bcdedit /set testsigning off
echo Modo de pruebas desactivado. Reinicia la VM.
pause
