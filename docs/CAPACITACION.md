# Capacitación: laboratorio de geolocalización simulada

## Objetivos

Al terminar, la persona podrá:

- explicar la diferencia entre driver de sensor, API de ubicación, NMEA y navegador;
- crear una VM aislada y reproducible;
- instalar el driver y un par COM virtual;
- aplicar coordenadas y verificar el resultado;
- diagnosticar caché, permisos, rutas y proveedores que sobrescriben la ubicación;
- preparar un repositorio público sin secretos ni artefactos personales.

## Módulo 1: arquitectura

```text
Coordenadas
   ├─ CLI/GUI ── Registro + reinicio del dispositivo ── Driver FakeGPS ── API Windows ── Chrome/Edge
   └─ NMEA ── COM10 ── COM11 ── receptor compatible
```

Maps no necesariamente consume NMEA. Cambiar una ruta no garantiza cambiar la otra. La capacitación debe verificar la API real del consumidor.

## Módulo 2: demostración

1. Arranca la VM desde la instantánea limpia.
2. Confirma que el driver esté habilitado.
3. Abre la aplicación visual con permisos de administrador.
4. Introduce `35.676192,139.650311` para Tokio.
5. Pulsa **Aplicar ubicación** y acepta UAC.
6. Espera a que termine el reinicio del dispositivo.
7. Pulsa **Probar Sensor Windows**.
8. Cierra y abre el navegador; pulsa **Tu ubicación**.

## Módulo 3: diagnóstico

Si el navegador muestra una ubicación anterior:

- confirma primero la API Windows, no el mapa;
- revisa que no esté activo otro proveedor NMEA/GPSDirect;
- reinicia el dispositivo FakeGPS;
- reinicia el navegador para evitar caché de geolocalización;
- comprueba que el navegador tenga permiso de ubicación;
- revisa que el receptor esté en `COM11` y el emisor en `COM10`.

Si aparece Brasil u otra ubicación fija, busca scripts o procesos que la escriban automáticamente y no ejecutes presets sin revisar sus coordenadas.

## Módulo 4: ejercicios

1. Cambiar entre Tokio, Bogotá y Madrid.
2. Desconectar COM10 y explicar por qué NMEA falla mientras el sensor Windows puede seguir funcionando.
3. Deshabilitar el driver y documentar el error observado.
4. Crear una instantánea antes y después de la práctica.
5. Ejecutar una revisión de secretos antes de publicar.
