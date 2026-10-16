# Preparación para GitHub

## Nunca publicar

- discos virtuales, instantáneas, ISOs o credenciales;
- `.exe`, `.dll`, `.sys`, `.cat`, `.zip` o instaladores de terceros sin revisar licencia;
- logs, dumps, capturas y resultados de diagnóstico;
- rutas con nombres de usuario, carpetas privadas o unidades compartidas;
- certificados, claves privadas, tokens y archivos de configuración locales.

## Revisión antes del primer commit

```powershell
git status --short
git diff --check
rg -n -i "password|secret|token|api[_-]?key|BEGIN .*PRIVATE KEY|C:\\Users|OneDrive|Z:\\" .
```

Las rutas genéricas como `\\VBOXSVR\\fakeGPS` son intencionales; no deben contener nombres de usuario ni hosts reales.

## Flujo recomendado

1. Revisar licencia del upstream y dependencias.
2. Revisar el diff completo.
3. Crear commit inicial solo con fuente y documentación.
4. Hacer una revisión externa del contenido.
5. Crear el repositorio privado primero.
6. Publicarlo cuando la revisión de secretos y licencias esté aprobada.
