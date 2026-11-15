# OpenBonk-Beta-Installationsanleitung

> Maschinelle Übersetzung aus dem Englischen. Bitte melden Sie unklare Anweisungen.

> OpenBonk ist ein inoffizieller Community-Port im Test. Jeder Tester benötigt seine eigene kompatible Steam-Kopie von Megabonk.

## Was Sie brauchen

- Ein 64-Bit-Android-Gerät mit Android 8 oder höher. Die Beta wurde auf einem Retroid Pocket 5 und einem iPlay 70 mini Pro mit Android 15 getestet; Die Kompatibilität mit anderen Geräten ist noch nicht gewährleistet.
- Ihre eigene kompatible Steam-Installation von Megabonk auf einem Computer. Der Launcher prüft bestimmte unterstützte Dateien; Ein anderes Spielupdate ist möglicherweise nicht kompatibel.
- Extra freier Speicherplatz für die ZIP-, Vorbereitungs- und Spieldaten. Die Anforderungen variieren je nach Gerät. Die Vorbereitung kann mehrere Minuten dauern (etwa vier Minuten in einem iPlay 70 mini Pro-Test).
- Der `OpenBonk.apk` im Beta-Paket. Sichern Sie alle vorhandenen Testfortschritte, bevor Sie eine Installation ersetzen. Eine anders signierte APK erfordert möglicherweise eine separate Installation.

## Holen Sie sich das Beta-Paket

Laden Sie `OpenBonk-beta.zip` von der [Betaversion](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.0-beta) herunter und extrahieren Sie es auf Ihrem Computer. Es enthält `OpenBonk.apk`, eine Installationsanleitung und einen `tools`-Ordner. Behalten Sie den gesamten Ordner `tools`, wenn Sie den ZIP-Helfer benötigen. Das APK ist das Installationsprogramm; Sie müssen separat eine private ZIP-Datei aus Ihrer eigenen Steam-Kopie erstellen.

## Erstellen Sie die ZIP-Datei mit den Spieldaten

1. Öffnen Sie in Steam die Eigenschaften von Megabonk und wählen Sie **Installierte Dateien → Durchsuchen**.
2. Suchen Sie den Ordner `Megabonk_Data`. Klicken Sie unter Windows mit der rechten Maustaste darauf und wählen Sie **Senden an → Komprimierter (komprimierter) Ordner**; unter Windows 11 befindet sich dies möglicherweise unter **Weitere Optionen anzeigen**. Verwenden Sie unter Linux die Komprimierungsoption Ihres Dateimanagers und wählen Sie ZIP. Komprimieren Sie den gesamten Ordner, nicht nur seinen Inhalt: Die ZIP-Datei sollte einen `Megabonk_Data`-Ordner mit seinen Unterordnern enthalten. [Microsoft: Dateien komprimieren und entpacken](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Stellen Sie sicher, dass die ZIP-Datei nicht größer als **1 GiB** ist. Schließen Sie Ihr Android-Gerät an und entsperren Sie es, wählen Sie in der USB-Benachrichtigung **Dateiübertragung** und kopieren Sie sowohl die Steam-ZIP-Datei als auch die Betaversion `OpenBonk.apk` in den Ordner „Downloads“. Halten Sie die Steam-ZIP-Datei privat: Sie enthält Dateien aus Ihrer Spielkopie. [Android: Dateien per USB übertragen](https://support.google.com/android/answer/9064445?hl=en).

Eine vollständige Linux-Installations-ZIP-Datei wurde auf Retroid und iPlay getestet. Eine kleinere ZIP-Datei mit den 39 unterstützten Windows-Dateien wurde ebenfalls auf iPlay importiert und erreichte Forest. Das Vorbereiten der ZIP-Datei auf einem physischen Windows-PC und das Importieren eines vollständigen Windows-Installationsordners wurden noch nicht überprüft.

Wenn die vollständige ZIP-Datei 1 GiB überschreitet, enthält der Ordner `tools` des Pakets ein Hilfsprogramm, das die 39 erforderlichen Dateien validiert und eine kleinere ZIP-Datei erstellt. Öffnen Sie unter Windows PowerShell im Ordner mit `tools` und führen Sie diesen Befehl aus. Ersetzen Sie dabei den ersten Pfad durch Ihren Spielordner:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Unter Linux aus demselben Ordner und mit Python 3.9 oder höher:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

Wenn unter Windows Python 3.9 oder höher installiert ist, können Sie den Python-Helfer auch über PowerShell ausführen:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Kopieren Sie die ZIP-Datei des Helfers in „Downloads“ auf Android und wählen Sie sie anstelle der vollständigen ZIP-Datei aus. Die Helfer überschreiben keine vorhandene ZIP-Datei und lehnen nicht erkannte Dateiversionen ab. Der Python-Helfer wurde mit Windows Python unter Wine ausgeführt und seine ZIP-Datei wurde auf iPlay importiert. Der PowerShell-Helfer wurde mit PowerShell 7 unter Linux getestet. Keiner der Helfer wurde auf einem nativen Windows-PC getestet.

## Installieren und spielen

1. Öffnen Sie `OpenBonk.apk` im Dateimanager von Android. Wenn Sie Dateidetails sehen, suchen Sie nach **Öffnen mit → Paketinstallationsprogramm**. Wenn Android um Erlaubnis zur Installation dieser App bittet, lesen Sie die Aufforderung und befolgen Sie die Anweisungen. Sie können diese Berechtigung später deaktivieren.
2. Öffnen Sie OpenBonk. Wählen Sie im Launcher **Controller** oder **Touch**.
3. Wählen Sie **Import Steam game**, suchen Sie die ZIP-Datei unter „Downloads“ und wählen Sie sie aus. **Import prepared Android data** ist für ein zuvor vorbereitetes Android-Bundle, nicht für die Steam-ZIP-Datei. Lassen Sie die App geöffnet und warten Sie auf **Data ready**. Die Meldung ändert sich zwischen den Phasen, es wird jedoch kein Prozentsatz angezeigt.
4. Wählen Sie **Play**. Beim ersten Start werden Text und Texturen importiert, bevor das Spielmenü erscheint.

## Importieren und sichern Sie den Fortschritt

Das Importieren eines Steam-Profils ist optional. Schließen Sie zunächst das Spiel auf Ihrem Computer und erstellen Sie ein Backup. Suchen Sie den Kontoordner unter `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` unter Linux oder versuchen Sie es mit `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` unter Windows (der Windows-Pfad wurde nicht überprüft). Zippen Sie `progression.json` und `stats.json` zusammen aus demselben Konto, kopieren Sie die ZIP-Datei auf Android und wählen Sie **Import profile ZIP**. Durch das Starten des Spiels wird der Fortschritt des aktuellen Android-Kontos ersetzt. Behalten Sie daher Ihr ursprüngliches Backup. Dies ist ein einmaliger Import, keine PC-zu-Android-Synchronisierung. Die Profil-ZIP ist privat; Hängen Sie es nicht an einen Bericht an.

Um den Android-Fortschritt zu sichern, schließen Sie das Spiel vollständig, öffnen Sie den Launcher und wählen Sie **Export progress**. Wählen Sie ein Profil und speichern Sie die ZIP-Datei an einem privaten Ort. Sie können es mit **Import profile ZIP** wiederherstellen. Die ZIP-Datei enthält Fortschritte und Statistiken, jedoch keine Steuerelemente oder Mods. Der Export wurde auf einem iPlay mit einem Testprofil getestet; Behalten Sie auch das Original-Steam-Backup.

**Android-Backups:** Diese Version deaktiviert die automatische Sicherung nicht. Abhängig von den Geräteeinstellungen kann Android App-Daten wie Speicherungen oder einen ausstehenden Profilimport in Kontosicherungen und Geräteübertragungen einbeziehen. Das APK fordert keine Internetberechtigung an, aber Android kann seine eigenen Backups verwalten. Überprüfen Sie die Backup-Einstellungen von Android, wenn Sie dies nicht möchten. Durch die Deinstallation der App können lokale Daten gelöscht werden. Sichern Sie zunächst den Fortschritt. [Android Auto Backup](https://developer.android.com/identity/data/autobackup).

## Fehlerbehebung

- **Inkompatible ZIP-Datei:** Wählen Sie `Megabonk_Data` aus einer vollständigen Installation und einer unterstützten Spielversion. Steam-Updates können vom Launcher überprüfte Dateien ändern.
- **Nicht genügend Speicherplatz:** Geben Sie Speicherplatz frei und versuchen Sie es erneut. Der Launcher berechnet den gesamten Platzbedarf noch nicht im Voraus.
- **Textimport schlägt beim Start fehl:** Öffnen Sie das Spiel erneut, um es erneut zu versuchen, während die ausstehende ZIP-Datei im Cache bleibt. Wenn Android den Cache geleert hat, wählen Sie die ZIP-Datei erneut aus.
- **APK lässt sich nicht über eine andere Version installieren:** Die Signatur oder Anwendungs-ID kann unterschiedlich sein. Sichern Sie alle Fortschritte, die Sie behalten möchten, bevor Sie eine vorhandene App deinstallieren.
- **Play Protect bittet darum, die APK zu scannen:** Auf einem iPlay bietet Android an, die Datei vor der Installation zur Analyse an Google zu senden. Lesen Sie die Aufforderung und entscheiden Sie, ob Sie sie senden möchten. Das Ergebnis dieses Scans und eine vollständige Installation durch das Systeminstallationsprogramm wurden noch nicht überprüft.

## Melden Sie ein Problem

Geben Sie das Gerätemodell und die Android-Version an, ob Sie einen Controller oder ein Touch-Gerät verwendet haben, die Schritte, die Sie unternommen haben, was Sie erwartet haben und was passiert ist. Fügen Sie ggf. einen Fehler-Screenshot bei. Geben Sie Ihre Steam-Postleitzahl, Ihr Profil oder Ihre Kontoinformationen nicht in einen gewöhnlichen Bericht ein.
