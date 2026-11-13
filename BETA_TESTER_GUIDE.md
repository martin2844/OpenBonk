# OpenBonk: guía de instalación para testers

> OpenBonk es un port comunitario no oficial en fase de prueba. Cada tester necesita su propia copia compatible de Megabonk de Steam.

## Qué necesitás

- Un dispositivo Android de 64 bits con Android 8 o posterior. Se hicieron pruebas en Retroid Pocket 5 y en una iPlay 70 mini Pro con Android 15; la compatibilidad con otros dispositivos todavía es limitada.
- Una copia propia de Megabonk instalada desde Steam en una PC. El launcher reconoce únicamente las versiones de archivos que se probaron; una actualización diferente puede no ser compatible.
- Espacio libre adicional para el ZIP, la preparación y los datos del juego. La cantidad exacta depende del dispositivo; la preparación puede tardar varios minutos (alrededor de cuatro en una prueba con iPlay 70 mini Pro).
- La APK `OpenBonk.apk` incluida en este paquete. Si ya tenés una instalación de prueba con progreso, conservá un respaldo antes de reemplazarla: una APK con otra firma puede requerir una instalación separada.

## Preparar los archivos de OpenBonk

En la PC, descomprimí el paquete de la beta en una carpeta. Adentro están `OpenBonk.apk`, esta guía y la carpeta `tools`. Dejá `tools` completa si necesitás usar el asistente de ZIP. La APK es el instalador; todavía falta crear por separado el ZIP privado de tu copia de Steam.

## Preparar el ZIP del juego

1. En Steam, abrí las propiedades de Megabonk y elegí **Archivos instalados → Explorar**.
2. En la carpeta del juego, ubicá `Megabonk_Data`. En Windows, hacé clic derecho sobre esa carpeta y elegí **Enviar a → Carpeta comprimida (en zip)**; en Windows 11 puede estar dentro de **Mostrar más opciones**. En Linux, usá la opción de comprimir del administrador de archivos y elegí ZIP. Comprimí la carpeta completa, no solo su contenido: al abrir el ZIP debe verse una carpeta `Megabonk_Data` con sus subcarpetas. [Ayuda de Microsoft para crear un ZIP](https://support.microsoft.com/es-es/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Verificá que el ZIP no supere **1 GiB**. Conectá y desbloqueá el Android; en la notificación USB elegí **Transferencia de archivos** y copiá a Descargas tanto el ZIP de Steam como `OpenBonk.apk` de la carpeta de la beta. Conservá el ZIP de Steam para uso personal: contiene archivos de tu copia del juego. [Ayuda de Android para transferir archivos por USB](https://support.google.com/android/answer/9064445?hl=es).

Se verificó el flujo con un ZIP de instalación Linux completa en Retroid y en iPlay. En iPlay también se importó un ZIP reducido con los 39 archivos Windows compatibles y se inició Forest; la preparación desde una PC Windows real y un ZIP de carpeta Windows completa todavía no se verificó.

Si el ZIP completo supera 1 GiB, la carpeta `tools` de este paquete incluye un asistente que valida los 39 archivos necesarios y crea un ZIP reducido. En Windows, abrí PowerShell en la carpeta que contiene `tools` y ejecutá el siguiente comando, reemplazando la primera ruta por la carpeta real del juego:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

En Linux, desde esa misma carpeta y con Python 3.9 o posterior:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

En Windows, si ya tenés Python 3.9 o posterior, también podés usar el asistente Python desde PowerShell:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Copiá el ZIP reducido que creó el asistente a Descargas en Android y seleccionalo en lugar del ZIP completo. Los asistentes no reemplazan un ZIP que ya exista y rechazan archivos de versiones no reconocidas. El asistente Python se ejecutó con Python de Windows bajo Wine y el ZIP resultante se importó en iPlay. La herramienta PowerShell se probó en Linux con PowerShell 7. Ninguna de las dos se probó todavía en una PC Windows nativa.

## Instalar y jugar

1. Abrí `OpenBonk.apk` desde la aplicación de archivos de Android; si se muestran detalles del archivo, buscá **Abrir con → Instalador de paquetes**. Si el sistema pide permiso para instalar desde esa aplicación, revisá el aviso y seguí las instrucciones de Android. Podés volver a desactivar el permiso después de instalar.
2. Abrí OpenBonk. En el launcher elegí **Controller** o **Touch** según el control que quieras usar.
3. Tocá **Import Steam game**, buscá el ZIP en Descargas y seleccionalo. El botón **Import prepared Android data** es para un paquete Android previamente preparado, no para el ZIP de Steam. Esperá a que aparezca **Data ready**; mantené la app abierta durante la preparación. El texto cambia al pasar de una etapa a otra, pero no muestra un porcentaje.
4. Tocá **Play**. La primera vez se importan textos y texturas antes de llegar al menú.

El perfil de Steam es opcional. Para importarlo, cerrá el juego en la PC y conservá un respaldo. Buscá la carpeta de la cuenta dentro de `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` en Linux o, en Windows, intentá `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` (ruta todavía no verificada en Windows). Comprimí juntos `progression.json` y `stats.json` de la misma cuenta, copialos como ZIP al Android y elegí **Import profile ZIP**. Al iniciar el juego se reemplaza el progreso de la cuenta Android actual; conservá el respaldo original. La importación es puntual y no sincroniza PC y Android. El ZIP del perfil es privado: no lo envíes con un reporte.

Para respaldar el progreso de Android, cerrá completamente el juego, abrí el launcher y elegí **Export progress**. Seleccioná el perfil y guardá el ZIP en un lugar privado. Podés volver a aplicarlo con **Import profile ZIP**; contiene progreso y estadísticas, pero no los controles ni los mods. La exportación se probó en iPlay con un perfil de prueba; no reemplaza el respaldo original de Steam.

**Copias de seguridad de Android:** esta versión no desactiva Auto Backup. Según la configuración de tu dispositivo, Android puede incluir datos de la aplicación, como partidas o un perfil pendiente de importar, en la copia de seguridad de tu cuenta y en transferencias entre dispositivos. La APK no solicita el permiso de Internet, pero eso no impide que el sistema gestione sus propias copias. Si no querés usar ese respaldo, revisá la configuración de copias de seguridad de Android. Al desinstalar la aplicación se pueden borrar sus datos locales: conservá una copia de tu progreso antes de hacerlo. [Cómo funciona Auto Backup](https://developer.android.com/identity/data/autobackup).

## Si algo falla

- **ZIP incompatible:** comprobá que seleccionaste la carpeta `Megabonk_Data` de una instalación completa y una versión admitida. Actualizaciones de Steam pueden cambiar archivos que el launcher valida.
- **Falta de espacio:** liberá espacio e intentá de nuevo. El launcher todavía no calcula por adelantado el espacio total necesario.
- **Falla la importación de textos al iniciar:** volvé a abrir el juego para reintentar mientras el ZIP pendiente siga en la caché. Si Android limpió esa caché, seleccioná de nuevo tu ZIP.
- **La APK no se instala sobre otra versión:** la firma o el identificador pueden diferir. No desinstales una app con progreso que quieras conservar sin haber hecho primero un respaldo.
- **Play Protect pide analizar la APK:** en una iPlay, Android ofreció enviar el archivo a Google para un análisis antes de instalarlo. Leé el aviso y decidí si querés enviarlo; todavía no se verificó el resultado de ese análisis ni la instalación completa desde el instalador del sistema.

## Cómo reportar un problema

Indicá el modelo de dispositivo, versión de Android, si usabas mando o controles táctiles, qué hiciste, qué esperabas y qué pasó. Si sirve, adjuntá una captura del error. No hace falta compartir tu ZIP de Steam, tu perfil ni datos de cuenta para un reporte ordinario.
