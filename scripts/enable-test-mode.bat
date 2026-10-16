@echo off
net session >nul 2>&1
if not %errorlevel%==0 (
  echo Ejecuta este script como Administrador.
  pause
  exit /b 1
)
bcdedit /set testsigning on
echo Modo de pruebas activado. Reinicia la VM.
pause
