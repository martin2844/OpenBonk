# OpenBonk Beta Tester Guide

> OpenBonk is an unofficial community port in beta. Each tester needs their own compatible Steam copy of Megabonk.

## What you need

- A 64-bit Android device running Android 8 or later. OpenBonk has been tested on a Retroid Pocket 5 and an iPlay 70 mini Pro running Android 15; compatibility with other devices remains limited.
- Your own Steam installation of Megabonk on a PC. The launcher recognizes only tested file versions; other updates may not be compatible.
- Additional free storage for the ZIP, preparation and game data. The amount depends on the device. Preparation can take several minutes; it took about four minutes in one iPlay 70 mini Pro test.
- The `OpenBonk.apk` included in this package. If you already have a test installation with progress, back it up before replacing it: an APK signed with a different key may require a separate installation.

## Prepare the OpenBonk files

On your PC, extract the beta package to a folder. It contains `OpenBonk.apk`, this guide and a `tools` folder. Keep the entire `tools` folder if you need the ZIP helper. The APK is the installer; you still need to create a separate private ZIP from your Steam game files.

## Prepare the game ZIP

1. In Steam, open Megabonk's properties and select **Installed Files → Browse**.
2. Find `Megabonk_Data` in the game folder. On Windows, right-click that folder and choose **Send to → Compressed (zipped) folder**; on Windows 11, this may be under **Show more options**. On Linux, use your file manager's compress option and select ZIP. Compress the entire folder, not just its contents: when you open the ZIP, it should contain a `Megabonk_Data` folder with its subfolders. [Microsoft's ZIP instructions](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Make sure the ZIP is no larger than **1 GiB**. Connect and unlock your Android device, select **File transfer** in the USB notification, and copy both the Steam ZIP and the beta package's `OpenBonk.apk` to Downloads. Keep the Steam ZIP for personal use: it contains files from your game copy. [Android USB file transfer instructions](https://support.google.com/android/answer/9064445?hl=en).

A ZIP of a complete Linux installation was tested on Retroid and iPlay. A smaller ZIP containing the 39 compatible Windows files was also imported on iPlay and reached Forest. Preparation on a native Windows PC and import of a full Windows game folder ZIP have not yet been tested.

If the full ZIP exceeds 1 GiB, the package's `tools` folder includes a helper that validates the 39 required files and creates a smaller ZIP. On Windows, open PowerShell in the folder containing `tools` and run the following command, replacing the first path with your actual game folder:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\path\to\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

On Linux, from that same folder and with Python 3.9 or later:

```sh
python3 tools/create-launcher-steam-zip.py "/path/to/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

On Windows, if you already have Python 3.9 or later, you can also run the Python helper from PowerShell:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\path\to\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Copy the smaller ZIP created by the helper to Downloads on Android and select it instead of the full ZIP. The helpers do not overwrite an existing ZIP and reject files from unrecognized versions. The Python helper was run using Windows Python under Wine, and its ZIP was imported on iPlay. The PowerShell helper was tested on Linux with PowerShell 7. Neither helper has yet been tested on a native Windows PC.

## Install and play

1. Open `OpenBonk.apk` from your Android file manager. If file details are displayed, look for **Open with → Package installer**. If Android asks for permission to install from that app, review the prompt and follow its instructions. You can disable the permission again after installation.
2. Open OpenBonk. In the launcher, choose **Controller** or **Touch**, depending on how you want to play.
3. Tap **Megabonk game files ZIP**, find the ZIP in Downloads and select it. **Import prepared Android data** is for a previously prepared Android data package, not a Steam ZIP. Keep the app open and wait for **Data ready**. The status text changes between stages, but it does not show a percentage.
4. Tap **Play**. On the first launch, text and textures are imported before the menu appears.

A Steam profile is optional. To import one, close the game on your PC and keep a backup. Find your account folder under `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` on Linux or try `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` on Windows (the Windows path has not yet been verified). ZIP `progression.json` and `stats.json` together from the same account, copy the ZIP to Android and choose **Import profile ZIP**. When the game starts, it replaces the current Android account's progress, so keep your original backup. Import is a one-time operation and does not synchronize PC and Android. The profile ZIP is private: do not include it in a report.

To back up Android progress, close the game completely, open the launcher and choose **Export profile**. Select the profile and save the ZIP somewhere private. You can restore it with **Import profile ZIP**. It contains progress and statistics, but not controls or mods. Export was tested on iPlay with a test profile; it does not replace your original Steam backup.

**Android backups:** this version does not disable Auto Backup. Depending on your device settings, Android may include app data, such as saves or a profile pending import, in your account backup and device-to-device transfers. The launcher uses the internet for updates and the mod catalog. If you do not want to use that backup, review your Android backup settings. Uninstalling the app may delete its local data, so back up your progress first. [How Auto Backup works](https://developer.android.com/identity/data/autobackup).

## Troubleshooting

- **Incompatible ZIP or unrecognized `sharedassets1.assets`:** the file in the ZIP does not match the data supported by this beta. It may be a different game version, modified files or a damaged ZIP. If you can access the Steam installation, verify its files and create a new ZIP from its `Megabonk_Data` folder; back up any PC mods before verification. The smaller ZIP helper in this guide also validates files before you copy them to your phone. If you only have Android or the error persists, report the game version, whether the copy is for Windows or Linux, and how you obtained the ZIP. Do not share the ZIP or your save.
- **Not enough storage:** free up space and try again. The launcher does not yet calculate the total space needed in advance.
- **Text import fails at startup:** reopen the game to retry while the pending ZIP remains in the cache. If Android has cleared the cache, select your ZIP again.
- **The APK will not install over another version:** the signature or application ID may differ. Do not uninstall an app containing progress you want to keep without backing it up first.
- **Play Protect asks to scan the APK:** on an iPlay, Android offered to send the file to Google for analysis before installation. Read the prompt and decide whether to send it. The scan result and complete installation through the system installer have not yet been verified.

## Report a problem

Include your device model, Android version, whether you used a controller or touch controls, what you did, what you expected and what happened. Attach a screenshot of the error if helpful. You do not need to share your Steam ZIP, profile or account information for an ordinary report.
