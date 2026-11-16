# OpenBonk: guía de instalación para probadores

> OpenBonk es un port comunitario no oficial en fase de prueba. Cada probador necesita su propia copia compatible de Megabonk de Steam.

## Qué necesitas

- Un dispositivo Android de 64 bits con Android 8 o posterior. Se hicieron pruebas en Retroid Pocket 5 y en una iPlay 70 mini Pro con Android 15; la compatibilidad con otros dispositivos todavía es limitada.
- Una copia propia de Megabonk instalada desde Steam en un PC. El launcher reconoce únicamente las versiones de archivos que se probaron; una actualización diferente puede no ser compatible.
- Espacio libre adicional para el ZIP, la preparación y los datos del juego. La cantidad exacta depende del dispositivo; la preparación puede tardar varios minutos (alrededor de cuatro en una prueba con iPlay 70 mini Pro).
- La APK `OpenBonk.apk` incluida en este paquete. Si ya tienes una instalación de prueba con progreso, conserva un respaldo antes de reemplazarla: una APK con otra firma puede requerir una instalación separada.

## Preparar los archivos de OpenBonk

En el PC, descarga `OpenBonk-beta.zip` desde la [versión beta](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.1-beta) y descomprímelo en una carpeta. Dentro están `OpenBonk.apk`, una guía y la carpeta `tools`. Deja `tools` completa si necesitas usar el asistente de ZIP. La APK es el instalador; todavía falta crear por separado el ZIP privado de tu copia de Steam.

## Preparar el ZIP del juego

1. En Steam, abre las propiedades de Megabonk y elige **Archivos instalados → Explorar**.
2. En la carpeta del juego, ubica `Megabonk_Data`. En Windows, haz clic derecho sobre esa carpeta y elige **Enviar a → Carpeta comprimida (en zip)**; en Windows 11 puede estar dentro de **Mostrar más opciones**. En Linux, usa la opción de comprimir del administrador de archivos y elige ZIP. Comprime la carpeta completa, no solo su contenido: al abrir el ZIP debe verse una carpeta `Megabonk_Data` con sus subcarpetas. [Ayuda de Microsoft para crear un ZIP](https://support.microsoft.com/es-es/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Verifica que el ZIP no supere **1 GiB**. Conecta y desbloquea el Android; en la notificación USB elige **Transferencia de archivos** y copia a Descargas tanto el ZIP de Steam como `OpenBonk.apk` de la carpeta de la beta. Conserva el ZIP de Steam para uso personal: contiene archivos de tu copia del juego. [Ayuda de Android para transferir archivos por USB](https://support.google.com/android/answer/9064445?hl=es).

Se verificó el flujo con un ZIP de instalación Linux completa en Retroid y en iPlay. En iPlay también se importó un ZIP reducido con los 39 archivos Windows compatibles y se inició Forest; la preparación desde un PC con Windows real y un ZIP de carpeta Windows completa todavía no se verificó.

Si el ZIP completo supera 1 GiB, la carpeta `tools` de este paquete incluye un asistente que valida los 39 archivos necesarios y crea un ZIP reducido. En Windows, abre PowerShell en la carpeta que contiene `tools` y ejecuta el siguiente comando, reemplazando la primera ruta por la carpeta real del juego:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

En Linux, desde esa misma carpeta y con Python 3.9 o posterior:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

En Windows, si ya tienes Python 3.9 o posterior, también puedes usar el asistente Python desde PowerShell:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Copia el ZIP reducido que creó el asistente a Descargas en Android y selecciónalo en lugar del ZIP completo. Los asistentes no reemplazan un ZIP que ya exista y rechazan archivos de versiones no reconocidas. El asistente Python se ejecutó con Python de Windows bajo Wine y el ZIP resultante se importó en iPlay. La herramienta PowerShell se probó en Linux con PowerShell 7. Ninguna de las dos se probó todavía en un PC con Windows nativo.

## Instalar y jugar

1. Abre `OpenBonk.apk` desde la aplicación de archivos de Android; si se muestran detalles del archivo, busca **Abrir con → Instalador de paquetes**. Si el sistema pide permiso para instalar desde esa aplicación, revisa el aviso y sigue las instrucciones de Android. Puedes volver a desactivar el permiso después de instalar.
2. Abre OpenBonk. En el launcher elige **Controller** o **Touch** según el control que quieras usar.
3. Toca **Megabonk game files ZIP**, busca el ZIP en Descargas y selecciónalo. El botón **Import prepared Android data** es para un paquete Android previamente preparado, no para el ZIP de Steam. Espera a que aparezca **Data ready**; mantén la app abierta durante la preparación. El texto cambia al pasar de una etapa a otra, pero no muestra un porcentaje.
4. Toca **Play**. La primera vez se importan textos y texturas antes de llegar al menú.

El perfil de Steam es opcional. Para importarlo, cierra el juego en el PC y conserva un respaldo. Busca la carpeta de la cuenta dentro de `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` en Linux o, en Windows, prueba `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` (ruta todavía no verificada en Windows). Comprime juntos `progression.json` y `stats.json` de la misma cuenta, cópialos como ZIP al Android y elige **Import profile ZIP**. Al iniciar el juego se reemplaza el progreso de la cuenta Android actual; conserva el respaldo original. La importación es puntual y no sincroniza PC y Android. El ZIP del perfil es privado: no lo envíes con un reporte.

Para respaldar el progreso de Android, cierra completamente el juego, abre el launcher y elige **Export profile**. Selecciona el perfil y guarda el ZIP en un lugar privado. Puedes volver a aplicarlo con **Import profile ZIP**; contiene progreso y estadísticas, pero no los controles ni los mods. La exportación se probó en iPlay con un perfil de prueba; no reemplaza el respaldo original de Steam.

**Copias de seguridad de Android:** esta versión no desactiva Auto Backup. Según la configuración de tu dispositivo, Android puede incluir datos de la aplicación, como partidas o un perfil pendiente de importar, en la copia de seguridad de tu cuenta y en transferencias entre dispositivos. El launcher usa Internet para las actualizaciones y el catálogo de mods. Si no quieres usar ese respaldo, revisa la configuración de copias de seguridad de Android. Al desinstalar la aplicación se pueden borrar sus datos locales: conserva una copia de tu progreso antes de hacerlo. [Cómo funciona Auto Backup](https://developer.android.com/identity/data/autobackup).

## Si algo falla

- **ZIP incompatible:** comprueba que seleccionaste la carpeta `Megabonk_Data` de una instalación completa y una versión admitida. Actualizaciones de Steam pueden cambiar archivos que el launcher valida.
- **Falta de espacio:** libera espacio e prueba de nuevo. El launcher todavía no calcula por adelantado el espacio total necesario.
- **Falla la importación de textos al iniciar:** vuelve a abrir el juego para reintentar mientras el ZIP pendiente siga en la caché. Si Android limpió esa caché, selecciona de nuevo tu ZIP.
- **La APK no se instala sobre otra versión:** la firma o el identificador pueden diferir. No desinstales una app con progreso que quieras conservar sin haber hecho primero un respaldo.
- **Play Protect pide analizar la APK:** en una iPlay, Android ofreció enviar el archivo a Google para un análisis antes de instalarlo. Lee el aviso y decide si quieres enviarlo; todavía no se verificó el resultado de ese análisis ni la instalación completa desde el instalador del sistema.

## Cómo reportar un problema

Indica el modelo de dispositivo, versión de Android, si usabas mando o controles táctiles, qué hiciste, qué esperabas y qué pasó. Si sirve, adjunta una captura del error. No hace falta compartir tu ZIP de Steam, tu perfil ni datos de cuenta para un reporte ordinario.
