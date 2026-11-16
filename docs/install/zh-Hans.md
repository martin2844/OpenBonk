# OpenBonk 测试版安装指南

> 英文机器翻译。请报告不清楚的指示。

> OpenBonk 是一个正在测试的非官方社区端口。每个测试者都需要自己兼容的 Megabonk Steam 副本。

## 你需要什么

- 运行 Android 8 或更高版本的 64 位 Android 设备。该 Beta 版已在搭载 Android 15 的 Retroid Pocket 5 和 iPlay 70 mini Pro 上进行了测试；与其他设备的兼容性尚未确定。
- 您自己在计算机上兼容的 Steam 安装的 Megabonk。启动器检查特定支持的文件；不同的游戏更新可能不兼容。
- 用于 ZIP、准备和游戏数据的额外可用空间。要求因设备而异。准备工作可能需要几分钟（iPlay 70 mini Pro 测试中大约需要四分钟）。
- 测试包中的`OpenBonk.apk`。在替换安装之前备份任何现有的测试进度。不同签名的 APK 可能需要单独安装。

## 获取测试版包

从 [测试版](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.1-beta) 下载 `OpenBonk-beta.zip` 并将其解压到您的计算机上。它包含 `OpenBonk.apk`、安装指南和 `tools` 文件夹。如果您需要 ZIP 帮助程序，请保留整个 `tools` 文件夹。 APK是安装程序；您必须从您自己的 Steam 副本中单独创建一个私人 ZIP。

## 创建游戏数据 ZIP

1. 在 Steam 中，打开 Megabonk 的属性并选择 **已安装文件 → 浏览**。
2. 找到 `Megabonk_Data` 文件夹。在 Windows 上，右键单击它并选择 **发送到 → 压缩（zipped）文件夹**；在 Windows 11 上，这可能位于 **显示更多选项** 下。在 Linux 上，使用文件管理器的压缩选项并选择 ZIP。压缩整个文件夹，而不仅仅是其内容：ZIP 应包含 `Megabonk_Data` 文件夹及其子文件夹。 [Microsoft：压缩和解压缩文件](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files)。
3. 确保 ZIP 不大于 **1 GiB**。连接并解锁您的 Android 设备，在 USB 通知中选择 **文件传输**，然后将 Steam ZIP 和测试版的 `OpenBonk.apk` 复制到“下载”。确保 Steam ZIP 的私密性：它包含您的游戏副本中的文件。 [Android：通过USB传输文件](https://support.google.com/android/answer/9064445?hl=en)。

完整的 Linux 安装 ZIP 在 Retroid 和 iPlay 上进行了测试。包含 39 个受支持的 Windows 文件的较小 ZIP 也已导入 iPlay 并到达 Forest。在物理 Windows PC 上准备 ZIP 并导入完整的 Windows 安装文件夹尚未得到验证。

如果完整 ZIP 超过 1 GiB，则包的 `tools` 文件夹包含一个帮助程序，用于验证 39 个所需文件并创建较小的 ZIP。在 Windows 上，在包含 `tools` 的文件夹中打开 PowerShell 并运行此命令，将第一个路径替换为您的游戏文件夹：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

在 Linux 上，从同一文件夹并使用 Python 3.9 或更高版本：

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

在 Windows 上，如果安装了 Python 3.9 或更高版本，您还可以从 PowerShell 运行 Python 帮助程序：

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

将助手的 ZIP 复制到 Android 上的“下载”并选择它而不是完整的 ZIP。帮助程序不会覆盖现有 ZIP 并拒绝无法识别的文件版本。 Python 帮助程序在 Wine 下使用 Windows Python 运行，其 ZIP 已在 iPlay 上导入。 PowerShell 帮助程序在 Linux 上使用 PowerShell 7 进行了测试。这两个帮助程序均未在本机 Windows PC 上进行过测试。

## 安装并播放

1. 在 Android 的文件管理器中打开 `OpenBonk.apk`。如果您看到文件详细信息，请查找 **打开方式 → 软件包安装程序**。如果 Android 请求从该应用程序安装的权限，请阅读提示并按照其说明进行操作。您可以稍后关闭该权限。
2. 打开 OpenBonk。在启动器中选择 **Controller** 或 **Touch**。
3. 选择**Megabonk game files ZIP**，在下载中找到ZIP并选择它。 **Import prepared Android data** 适用于之前准备的 Android 捆绑包，而不是 Steam ZIP。保持应用程序打开并等待 **Data ready**。消息在阶段之间发生变化，但不显示百分比。
4. 选择 **Play**。首次启动时，文本和纹理会在游戏菜单出现之前导入。

## 导入并备份进度

导入 Steam 配置文件是可选的。首先关闭电脑上的游戏并保留备份。在 Linux 上找到位于 `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` 的帐户文件夹，或在 Windows 上尝试 `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\`（Windows 路径尚未验证）。来自同一帐户的 `progression.json` 和 `stats.json` 的 ZIP 一起，将 ZIP 复制到 Android 并选择 **Import profile ZIP**。开始游戏会替换当前 Android 帐户的进度，因此请保留原始备份。这是一次性导入，而不是 PC 到 Android 同步。配置文件 ZIP 是私有的；不要将其附加到报告中。

要备份 Android 进度，请完全关闭游戏，打开启动器并选择 **Export profile**。选择一个配置文件并将 ZIP 保存在私人位置。您可以使用 **Import profile ZIP** 恢复它。 ZIP 包含进度和统计数据，但不包含控件或模组。导出已在 iPlay 上使用测试配置文件进行了测试；也保留原始的 Steam 备份。

**Android 备份：** 此版本不会禁用自动备份。根据设备设置，Android 可能会在帐户备份和设备传输中包含应用数据，例如保存或待处理的配置文件导入。 启动器使用互联网获取更新和模组目录。如果您不希望这样做，请检查 Android 的备份设置。卸载应用程序可以删除本地数据；首先备份进度。 [Android 自动备份](https://developer.android.com/identity/data/autobackup)。

## 故障排除

- **不兼容的 ZIP：** 从完整安装和支持的游戏版本中选择 `Megabonk_Data`。 Steam 更新可能会更改启动器检查的文件。
- **空间不足：**释放空间并重试。发射器尚未提前计算总空间需求。
- **启动时文本导入失败：**重新打开游戏以重试，同时待处理的 ZIP 仍被缓存。如果 Android 清除了缓存，请再次选择 ZIP。
- **APK 不会安装在其他版本上：**其签名或应用程序 ID 可能不同。在卸载现有应用程序之前，备份您想要保留的所有进度。
- **Play Protect 要求在一台 iPlay 上扫描 APK：**，Android 会在安装前将文件发送给 Google 进行分析。阅读提示并决定是否发送。该扫描的结果以及通过系统安装程序的完整安装尚未得到验证。

## 报告问题

包括设备型号、Android 版本、您是否使用控制器或触摸、您采取的步骤、您的期望和发生的情况。如果有用，请附上错误屏幕截图。请勿在普通报告中包含您的 Steam ZIP、个人资料或帐户信息。
