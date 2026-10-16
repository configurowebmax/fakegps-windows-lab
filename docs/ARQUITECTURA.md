# Arquitectura técnica

## Componentes

- `FakeGPS.Driver`: sensor geográfico UMDF que expone datos de latitud y longitud.
- `FakeGPS.Common`: conversión de coordenadas, acceso al Registro y `GeoCoordinateWatcher`.
- `FakeGPS`: CLI legacy.
- `FakeGPS Visual`: WinForms; mantiene el estado de la demo y emite NMEA.
- `NmeaGPS`: generador de sentencias GGA/RMC/GSA/GSV.

## Dos rutas de datos

La ruta del sensor Windows y la ruta NMEA son independientes. Cambiar una no garantiza cambiar la otra. La aplicación debe indicar cuál está activa y la capacitación debe verificar la API real del consumidor.

## Riesgos conocidos

- La solución usa versiones antiguas de .NET, xUnit y herramientas de build.
- Algunas rutas del código original apuntan a una instancia fija del Registro.
- El modo de pruebas no debe habilitarse en un host productivo.
- Los navegadores pueden conservar permisos, caché o una ubicación previa.
