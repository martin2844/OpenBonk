# OpenBonk beta installation guide

> OpenBonk is an unofficial community port in testing. Each tester needs their own compatible Steam copy of Megabonk.

## What you need

- A 64-bit Android device running Android 8 or later. The beta has been tested on a Retroid Pocket 5 and an iPlay 70 mini Pro with Android 15; compatibility with other devices is not yet established.
- Your own compatible Steam installation of Megabonk on a computer. The launcher checks specific supported files; a different game update may not be compatible.
- Extra free space for the ZIP, preparation and game data. Requirements vary by device. Preparation can take several minutes (about four minutes in one iPlay 70 mini Pro test).
- The `OpenBonk.apk` in the beta package. Back up any existing test progress before replacing an installation. A differently signed APK may require a separate installation.

## Get the beta package

Download `OpenBonk-beta.zip` from the [beta release](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.0-beta) and extract it on your computer. It contains `OpenBonk.apk`, an installation guide and a `tools` folder. Keep the whole `tools` folder if you need the ZIP helper. The APK is the installer; you must separately create a private ZIP from your own Steam copy.

## Create the game-data ZIP

1. In Steam, open Megabonk's properties and select **Installed Files → Browse**.
2. Find the `Megabonk_Data` folder. On Windows, right-click it and choose **Send to → Compressed (zipped) folder**; on Windows 11 this may be under **Show more options**. On Linux, use your file manager's compress option and select ZIP. Compress the entire folder, not only its contents: the ZIP should contain a `Megabonk_Data` folder with its subfolders. [Microsoft: zip and unzip files](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Make sure the ZIP is no larger than **1 GiB**. Connect and unlock your Android device, choose **File transfer** in its USB notification, and copy both the Steam ZIP and the beta's `OpenBonk.apk` to Downloads. Keep the Steam ZIP private: it contains files from your game copy. [Android: transfer files by USB](https://support.google.com/android/answer/9064445?hl=en).

A full Linux installation ZIP was tested on Retroid and iPlay. A smaller ZIP with the 39 supported Windows files was also imported on iPlay and reached Forest. Preparing the ZIP on a physical Windows PC and importing a full Windows installation folder have not yet been verified.

If the full ZIP exceeds 1 GiB, the package's `tools` folder contains a helper that validates the 39 required files and creates a smaller ZIP. On Windows, open PowerShell in the folder containing `tools` and run this command, replacing the first path with your game folder:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

On Linux, from the same folder and with Python 3.9 or later:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

On Windows, if Python 3.9 or later is installed, you can also run the Python helper from PowerShell:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Copy the helper's ZIP to Downloads on Android and select it instead of the full ZIP. The helpers do not overwrite an existing ZIP and reject unrecognized file versions. The Python helper was run using Windows Python under Wine and its ZIP was imported on iPlay. The PowerShell helper was tested with PowerShell 7 on Linux. Neither helper has been tested on a native Windows PC.

## Install and play

1. Open `OpenBonk.apk` in Android's file manager. If you see file details, look for **Open with → Package installer**. If Android asks for permission to install from that app, read the prompt and follow its instructions. You can turn that permission off afterward.
2. Open OpenBonk. In the launcher choose **Controller** or **Touch**.
3. Select **Import Steam game**, find the ZIP in Downloads and select it. **Import prepared Android data** is for a previously prepared Android bundle, not the Steam ZIP. Keep the app open and wait for **Data ready**. The message changes between stages, but no percentage is shown.
4. Select **Play**. On the first launch, text and textures are imported before the game menu appears.

## Import and back up progress

Importing a Steam profile is optional. First close the game on your computer and keep a backup. Find the account folder at `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` on Linux or try `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` on Windows (the Windows path has not been verified). ZIP `progression.json` and `stats.json` from the same account together, copy the ZIP to Android and choose **Import profile ZIP**. Starting the game replaces the current Android account's progress, so keep your original backup. This is a one-time import, not PC-to-Android synchronization. The profile ZIP is private; do not attach it to a report.

To back up Android progress, close the game completely, open the launcher and select **Export progress**. Choose a profile and save the ZIP somewhere private. You can restore it using **Import profile ZIP**. The ZIP contains progress and statistics, but not controls or mods. Export was tested on an iPlay with a test profile; keep the original Steam backup as well.

**Android backups:** this version does not disable Auto Backup. Depending on device settings, Android may include app data, such as saves or a pending profile import, in account backups and device transfers. The APK does not request Internet permission, but Android can manage its own backups. Review Android's backup settings if you do not want this. Uninstalling the app can delete local data; back up progress first. [Android Auto Backup](https://developer.android.com/identity/data/autobackup).

## Troubleshooting

- **Incompatible ZIP:** select `Megabonk_Data` from a complete installation and a supported game version. Steam updates may change files checked by the launcher.
- **Not enough space:** free space and try again. The launcher does not yet calculate total space requirements in advance.
- **Text import fails at launch:** reopen the game to retry while the pending ZIP remains cached. If Android cleared the cache, select the ZIP again.
- **APK will not install over another version:** its signature or application ID may differ. Back up any progress you want to keep before uninstalling an existing app.
- **Play Protect asks to scan the APK:** on one iPlay, Android offered to send the file to Google for analysis before installation. Read the prompt and decide whether to send it. The result of that scan and a complete installation through the system installer have not yet been verified.

## Report a problem

Include the device model, Android version, whether you used a controller or touch, the steps you took, what you expected and what happened. Attach an error screenshot if useful. Do not include your Steam ZIP, profile or account information in an ordinary report.
