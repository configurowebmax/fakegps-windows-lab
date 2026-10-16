# Instalación reproducible de la máquina virtual

## 1. Preparación del host

Instala VirtualBox y el Extension Pack desde el sitio oficial. Usa una versión compatible y documenta la versión exacta en el registro de la capacitación.

Recomendación para la VM:

- Windows 11 Pro x64, ISO oficial.
- 4 vCPU y 8 GB de RAM como punto de partida.
- Disco virtual dinámico de 64 GB o más.
- Red NAT para el laboratorio.
- Instantánea limpia antes de instalar drivers.

No guardes en el repositorio la ISO, el disco virtual, instantáneas, credenciales ni exportaciones `.ova`.

## 2. Instalación de Windows

1. Instala Windows normalmente y crea un usuario local de laboratorio.
2. Ejecuta Windows Update.
3. Instala Guest Additions desde el menú de VirtualBox.
4. Reinicia y confirma que la resolución y el portapapeles funcionan.
5. Crea una instantánea llamada `Windows limpio`.

## 3. Carpeta compartida

Comparte la carpeta del proyecto con el nombre `fakeGPS`. Dentro de Windows puede aparecer como una unidad o como:

```text
\\VBOXSVR\fakeGPS
```

Los scripts deben funcionar con `%~dp0`; no dependas de una letra fija como `Z:`.

## 4. Modo de pruebas

El driver legacy puede requerir modo de pruebas:

```bat
scripts\enable-test-mode.bat
```

Reinicia la VM. Verifica que aparezca la marca de modo de pruebas. Este modo reduce las garantías de firma de drivers y debe permanecer confinado a la VM.

Para restaurar la configuración:

```bat
scripts\disable-test-mode.bat
```

Reinicia y confirma que el host no fue modificado.

## 5. Driver y puertos

Instala el driver desde el Administrador de dispositivos usando el INF correspondiente. Instala `com0com` únicamente desde una fuente confiable y crea un par, por ejemplo `COM10`/`COM11`.

La aplicación emisora escribe en `COM10`; el receptor debe escuchar en `COM11`. No abras el mismo puerto desde dos procesos.

## 6. Validación

Ejecuta:

```bat
scripts\test-windows-location.bat
```

Comprueba estado, latitud, longitud y precisión. Si el valor no cambia, deshabilita y vuelve a habilitar el dispositivo FakeGPS y repite la prueba.
