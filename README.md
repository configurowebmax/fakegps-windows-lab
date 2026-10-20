# FakeGPS para Windows

## Presentación interactiva

[Ver FakeGPS Windows Lab en GitHub Pages](https://configurowebmax.github.io/fakegps-windows-lab/fakegps-windows-lab.html)

## Vista previa

![FakeGPS para Windows](docs/fakegps-windows-lab.jpeg)

Driver UMDF y herramientas de laboratorio para probar ubicaciones GPS simuladas en Windows 10/11.

> Uso previsto: laboratorio, capacitación y pruebas controladas. No usar para evadir controles de ubicación, identidad o seguridad.

## Qué contiene

- `src/FakeGPS.Driver`: driver de sensor geográfico UMDF.
- `src/FakeGPS`: CLI original para consultar y establecer coordenadas.
- `src/FakeGPS.Common`: validación, Registro y lectura de la API de ubicación.
- `src/FakeGPS_GUI.cs`: panel visual para introducir coordenadas y emitir NMEA.
- `src/NmeaGPS.cs`: emulador NMEA por puerto serie.
- `scripts`: instalación, configuración y verificación sin rutas personales.
- `docs`: instalación de la VM, arquitectura, operación y capacitación.

## Inicio rápido

Lee primero [`docs/INSTALACION-VM.md`](docs/INSTALACION-VM.md). El flujo de demostración es:

1. Crear una VM Windows 11 x64 con Guest Additions.
2. Activar modo de pruebas solo dentro de la VM.
3. Instalar el driver y el par de puertos virtuales.
4. Construir o proporcionar los binarios localmente; no se guardan binarios en este repositorio.
5. Ejecutar la app visual como administrador.
6. Elegir una coordenada, pulsar **Aplicar ubicación** y aceptar UAC.
7. Confirmar con `scripts/test-windows-location.bat` y después probar Chrome/Edge.

## Dependencias externas

Este repositorio no incluye `gpscomplete.exe`, `com0com`, WDK, SDK ni binarios del driver. Consulta [`third-party/README.md`](third-party/README.md) y verifica licencias, procedencia y hashes antes de usarlos.

## Estado del proyecto

El código original es antiguo y requiere mantenimiento. La solución principal usa .NET Framework 4.5.2 y un proyecto UMDF/WDK legacy. Antes de publicar una versión nueva, compila en una VM limpia y conserva evidencias de prueba sin incluir datos personales.

## Licencia

El repositorio original no incluía un archivo de licencia. Confirma la licencia del upstream y de cada dependencia antes de publicar un fork o redistribuir binarios.
