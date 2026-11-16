# OpenBonk ベータ版インストール ガイド

> 英語からの機械翻訳。不明瞭な指示を報告してください。

> OpenBonk は、テスト用の非公式コミュニティ ポートです。各テスターに​​は、互換性のある Megabonk の独自の Steam コピーが必要です。

## 必要なもの

- Android 8 以降を実行する 64 ビット Android デバイス。ベータ版は、Retroid Pocket 5 と Android 15 を搭載した iPlay 70 mini Pro でテストされました。他のデバイスとの互換性はまだ確立されていません。
- Megabonk を互換性のある Steam でコンピュータにインストールします。ランチャーは、サポートされている特定のファイルをチェックします。別のゲームのアップデートには互換性がない可能性があります。
- ZIP、準備データ、ゲームデータ用の追加の空き容量。要件はデバイスによって異なります。準備には数分かかる場合があります (1 回の iPlay 70 mini Pro テストで約 4 分)。
- ベータ パッケージの `OpenBonk.apk`。インストールを置き換える前に、既存のテストの進行状況をバックアップしてください。別の方法で署名された APK は、別個のインストールが必要になる場合があります。

## ベータ版パッケージを入手する

[ベータ リリース](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.1-beta) から `OpenBonk-beta.zip` をダウンロードし、コンピューターに解凍します。これには、`OpenBonk.apk`、インストール ガイド、および `tools` フォルダーが含まれています。 ZIP ヘルパーが必要な場合は、`tools` フォルダー全体を保存してください。 APK はインストーラーです。自分の Steam コピーからプライベート ZIP を別途作成する必要があります。

## ゲームデータのZIPを作成する

1. Steam で、Megabonk のプロパティを開き、**インストールされたファイル → 参照** を選択します。
2. `Megabonk_Data` フォルダーを見つけます。 Windows では、ファイルを右クリックし、**送信先 → 圧縮 (zip 形式) フォルダー** を選択します。 Windows 11 では、これは **[その他のオプションを表示]** の下にある可能性があります。 Linux では、ファイル マネージャーの圧縮オプションを使用し、ZIP を選択します。コンテンツだけでなく、フォルダー全体を圧縮します。ZIP には、`Megabonk_Data` フォルダーとそのサブフォルダーが含まれている必要があります。 [Microsoft: ファイルの圧縮と解凍](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files)。
3. ZIP が **1 GiB** 以下であることを確認してください。 Android デバイスを接続してロックを解除し、USB 通知で **ファイル転送** を選択し、Steam ZIP とベータ版の `OpenBonk.apk` の両方をダウンロードにコピーします。 Steam ZIP は非公開にしておいてください。ゲーム コピーのファイルが含まれています。 [Android: USB でファイルを転送](https://support.google.com/android/answer/9064445?hl=en)。

完全な Linux インストール ZIP は、Retroid と iPlay でテストされました。サポートされている 39 個の Windows ファイルを含む小さい ZIP も iPlay にインポートされ、Forest に届きました。物理 Windows PC 上での ZIP の準備と完全な Windows インストール フォルダーのインポートはまだ検証されていません。

完全な ZIP が 1 GiB を超える場合、パッケージの `tools` フォルダーには、39 個の必要なファイルを検証し、より小さい ZIP を作成するヘルパーが含まれています。 Windows では、`tools` が含まれるフォルダーで PowerShell を開き、最初のパスをゲーム フォルダーに置き換えて次のコマンドを実行します。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Linux では、同じフォルダーから、Python 3.9 以降を使用して次のようにします。

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

Windows では、Python 3.9 以降がインストールされている場合は、PowerShell から Python ヘルパーを実行することもできます。

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

ヘルパーの ZIP を Android のダウンロードにコピーし、完全な ZIP の代わりにそれを選択します。ヘルパーは既存の ZIP を上書きせず、認識されないファイル バージョンを拒否します。 Python ヘルパーは Wine で Windows Python を使用して実行され、その ZIP は iPlay にインポートされました。 PowerShell ヘルパーは、Linux 上の PowerShell 7 でテストされました。どちらのヘルパーもネイティブ Windows PC ではテストされていません。

## インストールしてプレイする

1. Android のファイル マネージャーで `OpenBonk.apk` を開きます。ファイルの詳細が表示される場合は、**「プログラムから開く」→「パッケージ インストーラー」** を探します。 Android がそのアプリからのインストールの許可を求めた場合は、プロンプトを読み、その指示に従ってください。後でその許可をオフにすることができます。
2. オープンボンクを開きます。ランチャーで **Controller** または **Touch** を選択します。
3. **Megabonk game files ZIP** を選択し、ダウンロードで ZIP を見つけて選択します。 **Import prepared Android data** は、Steam ZIP ではなく、事前に準備された Android バンドル用です。アプリを開いたままにして、**Data ready** を待ちます。メッセージはステージごとに変わりますが、パーセンテージは表示されません。
4. **Play** を選択します。初回起動時は、ゲームメニューが表示される前にテキストとテクスチャがインポートされます。

## インポートとバックアップの進行状況

Steam プロファイルのインポートはオプションです。まずコンピュータ上のゲームを閉じて、バックアップを保存してください。 Linux では `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` でアカウント フォルダーを見つけるか、Windows では `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` を試してください (Windows パスは検証されていません)。同じアカウントから `progression.json` と `stats.json` を一緒に ZIP 化し、その ZIP を Android にコピーして、**Import profile ZIP** を選択します。ゲームを開始すると、現在の Android アカウントの進行状況が置き換えられるため、元のバックアップを保管しておいてください。これは 1 回限りのインポートであり、PC から Android への同期ではありません。プロファイル ZIP は非公開です。レポートには添付しないでください。

Android の進行状況をバックアップするには、ゲームを完全に終了し、ランチャーを開いて **Export profile** を選択します。プロファイルを選択し、ZIP を非公開の場所に保存します。 **Import profile ZIP** を使用して復元できます。 ZIP には進行状況と統計情報が含まれていますが、コントロールや MOD は含まれていません。エクスポートは、テスト プロファイルを使用して iPlay でテストされました。元の Steam バックアップも保持してください。

**Android バックアップ:** このバージョンでは自動バックアップは無効になりません。デバイスの設定によっては、Android ではアカウントのバックアップやデバイス転送に保存や保留中のプロファイルのインポートなどのアプリ データが含まれる場合があります。 ランチャーはアップデートと MOD カタログのためにインターネットを使用します。これを望まない場合は、Android のバックアップ設定を確認してください。アプリをアンインストールすると、ローカル データが削除される可能性があります。まず進行状況をバックアップします。 [Android 自動バックアップ](https://developer.android.com/identity/data/autobackup)。

## トラブルシューティング

- **互換性のない ZIP:** 完全なインストールとサポートされているゲーム バージョンから `Megabonk_Data` を選択してください。 Steam のアップデートにより、ランチャーでチェックされたファイルが変更される場合があります。
- **スペースが不足しています:** スペースを空けて、もう一度お試しください。ランチャーはまだ、事前に総スペース要件を計算していません。
- **起動時にテキストのインポートが失敗します:** 保留中の ZIP がキャッシュされたままになっている間にゲームを再度開き、再試行します。 Android がキャッシュをクリアした場合は、ZIP を再度選択します。
- **APK は別のバージョンに上書きインストールされません。** その署名またはアプリケーション ID は異なる場合があります。既存のアプリをアンインストールする前に、保存しておきたい進行状況をバックアップしてください。
- **Play プロテクトは APK のスキャンを要求します:** ある iPlay では、Android はインストール前に分析のためにファイルを Google に送信することを提案しました。プロンプトを読んで、送信するかどうかを決定します。スキャンの結果とシステム インストーラーによる完全なインストールはまだ検証されていません。

## 問題を報告する

デバイスのモデル、Android のバージョン、コントローラーとタッチのどちらを使用したか、実行した手順、予想したこと、何が起こったかを含めます。役立つ場合は、エラーのスクリーンショットを添付してください。通常のレポートには Steam ZIP、プロフィール、アカウント情報を含めないでください。
