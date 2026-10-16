# Construcción

## GUI visual

La app visual es un proyecto WinForms legacy. Abre `src/FakeGPS.Visual/FakeGPS.Visual.csproj` con Visual Studio que tenga instalado .NET Framework 4.5.2 Developer Pack y compílala en `Release`.

## CLI y NMEA

Compila `src/FakeGPS/FakeGPS.csproj` y `src/NmeaGPS.cs` con una herramienta compatible con .NET Framework. El driver requiere Visual Studio + Windows SDK/WDK y sus componentes UMDF.

No subas los resultados de `bin`, `obj` ni `release`; están excluidos por diseño.

## Verificación mínima

```powershell
git diff --check
rg -n -i "password|secret|token|api[_-]?key|BEGIN .*PRIVATE KEY|C:\\Users|OneDrive" .
```

Prueba siempre dentro de una VM limpia y conserva los binarios únicamente fuera del repositorio público.
