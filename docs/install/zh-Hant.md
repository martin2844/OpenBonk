# OpenBonk 測試版安裝指南

> 英文機器翻譯。請報告不清楚的指示。

> OpenBonk 是一個正在測試的非官方社群連接埠。每個測試者都需要自己相容的 Megabonk Steam 副本。

## 你需要什麼

- 運行 Android 8 或更高版本的 64 位元 Android 裝置。此 Beta 版已在搭載 Android 15 的 Retroid Pocket 5 和 iPlay 70 mini Pro 上進行了測試；與其他裝置的兼容性尚未確定。
- 您自己在電腦上相容的 Steam 安裝的 Megabonk。啟動器檢查特定支援的檔案；不同的遊戲更新可能不相容。
- 用於 ZIP、準備和遊戲資料的額外可用空間。要求因設備而異。準備工作可能需要幾分鐘（iPlay 70 mini Pro 測試中大約需要四分鐘）。
- 測試包中的`OpenBonk.apk`。在替換安裝之前備份任何現有的測試進度。不同簽名的 APK 可能需要單獨安裝。

## 取得測試版包

從 [測試版](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.1-beta) 下載 `OpenBonk-beta.zip` 並將其解壓縮到您的電腦上。它包含 `OpenBonk.apk`、安裝指南和 `tools` 資料夾。如果您需要 ZIP 幫助程序，請保留整個 `tools` 資料夾。 APK是安裝程式；您必須從您自己的 Steam 副本中單獨建立一個私人 ZIP。

## 創建遊戲資料 ZIP

1. 在 Steam 中，開啟 Megabonk 的屬性並選擇 **已安裝檔案 → 瀏覽**。
2. 找到 `Megabonk_Data` 資料夾。在 Windows 上，右鍵單擊它並選擇 **發送到 → 壓縮（zipped）資料夾**；在 Windows 11 上，這可能位於 **顯示更多選項** 下。在 Linux 上，使用檔案總管的壓縮選項並選擇 ZIP。壓縮整個資料夾，而不僅僅是其內容：ZIP 應包含 `Megabonk_Data` 資料夾及其子資料夾。 [Microsoft：壓縮和解壓縮檔案](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files)。
3. 確保 ZIP 不大於 **1 GiB**。連接並解鎖您的 Android 設備，在 USB 通知中選擇 **檔案傳輸**，然後將 Steam ZIP 和測試版的 `OpenBonk.apk` 複製到「下載」。確保 Steam ZIP 的隱私：它包含您的遊戲副本中的檔案。 [Android：透過USB傳輸檔案](https://support.google.com/android/answer/9064445?hl=en)。

完整的 Linux 安裝 ZIP 在 Retroid 和 iPlay 上進行了測試。包含 39 個受支援的 Windows 檔案的較小 ZIP 也已匯入 iPlay 並到達 Forest。在實體 Windows PC 上準備 ZIP 並導入完整的 Windows 安裝資料夾尚未得到驗證。

如果完整 ZIP 超過 1 GiB，則套件的 `tools` 資料夾包含一個幫助程序，用於驗證 39 個所需檔案並建立較小的 ZIP。在 Windows 上，在包含 `tools` 的資料夾中開啟 PowerShell 並執行此命令，將第一個路徑替換為您的遊戲資料夾：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

在 Linux 上，從同一資料夾並使用 Python 3.9 或更高版本：

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

在 Windows 上，如果安裝了 Python 3.9 或更高版本，您也可以從 PowerShell 執行 Python 幫助程式：

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

將助手的 ZIP 複製到 Android 上的「下載」並選擇它而不是完整的 ZIP。幫助程式不會覆蓋現有 ZIP 並拒絕無法辨識的檔案版本。 Python 幫助程式在 Wine 下使用 Windows Python 運行，其 ZIP 已在 iPlay 上匯入。 PowerShell 幫助程式在 Linux 上使用 PowerShell 7 進行了測試。這兩個幫助程式均未在本機 Windows PC 上進行測試。

## 安裝並播放

1. 在 Android 的檔案管理器中開啟 `OpenBonk.apk`。如果您看到文件詳細信息，請尋找 **開啟方式 → 軟體包安裝程式**。如果 Android 請求從該應用程式安裝的權限，請閱讀提示並按照其說明進行操作。您可以稍後關閉該權限。
2. 打開 OpenBonk。在啟動器中選擇 **Controller** 或 **Touch**。
3. 選擇**Megabonk game files ZIP**，在下載中找到ZIP並選擇它。 **Import prepared Android data** 適用於先前準備好的 Android 套裝包，而不是 Steam ZIP。保持應用程式開啟並等待 **Data ready**。訊息在階段之間發生變化，但不顯示百分比。
4. 選擇 **Play**。首次啟動時，文字和紋理會在遊戲選單出現之前導入。

## 導入並備份進度

導入 Steam 設定檔是可選的。首先關閉電腦上的遊戲並保留備份。在 Linux 上找到位於 `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` 的帳戶資料夾，或在 Windows 上嘗試 `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\`（Windows 路徑尚未驗證）。來自同一帳戶的 `progression.json` 和 `stats.json` 的 ZIP 一起，將 ZIP 複製到 Android 並選擇 **Import profile ZIP**。開始遊戲會替換目前 Android 帳戶的進度，因此請保留原始備份。這是一次性導入，而不是 PC 到 Android 同步。設定檔 ZIP 是私有的；不要將其附加到報告中。

若要備份 Android 進度，請完全關閉遊戲，開啟啟動器並選擇 **Export profile**。選擇一個設定檔並將 ZIP 保存在私人位置。您可以使用 **Import profile ZIP** 恢復它。 ZIP 包含進度和統計數據，但不包含控製或模組。匯出已在 iPlay 上使用測試設定檔進行了測試；也保留原始的 Steam 備份。

**Android 備份：** 此版本不會停用自動備份。根據裝置設置，Android 可能會在帳戶備份和裝置傳輸中包含應用程式數據，例如儲存或待處理的設定檔匯入。 啟動器使用網路取得更新與模組目錄。如果您不希望這樣做，請檢查 Android 的備份設定。卸載應用程式可以刪除本機資料；首先備份進度。 [Android 自動備份](https://developer.android.com/identity/data/autobackup)。

## 故障排除

- **不相容的 ZIP：** 從完整安裝和支援的遊戲版本中選擇 `Megabonk_Data`。 Steam 更新可能會更改啟動器檢查的檔案。
- **空間不足：**釋放空間並重試。發射器尚未提前計算總空間需求。
- **啟動時文字導入失敗：**重新開啟遊戲以重試，同時待處理的 ZIP 仍被快取。如果 Android 清除了緩存，請再次選擇 ZIP。
- **APK 不會安裝在其他版本上：**其簽章或應用程式 ID 可能不同。在卸載現有應用程式之前，請備份您想要保留的所有進度。
- **Play Protect 要求在一台 iPlay 上掃描 APK：**，Android 會在安裝前將檔案傳送給 Google 進行分析。閱讀提示並決定是否要發送。此掃描的結果以及通過系統安裝程序的完整安裝尚未得到驗證。

## 報告問題

包括設備型號、Android 版本、您是否使用控制器或觸控、您採取的步驟、您的期望和發生的情況。如果有用，請附上錯誤螢幕截圖。請勿在普通報告中包含您的 Steam ZIP、個人資料或帳戶資訊。
