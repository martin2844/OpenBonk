# Guida all'installazione della versione beta di OpenBonk

> Traduzione automatica dall'inglese. Si prega di segnalare istruzioni poco chiare.

> OpenBonk è un port comunitario non ufficiale in fase di test. Ogni tester necessita della propria copia Steam compatibile di Megabonk.

## Di cosa hai bisogno

- Un dispositivo Android a 64 bit con Android 8 o versioni successive. La beta è stata testata su un Retroid Pocket 5 e un iPlay 70 mini Pro con Android 15; la compatibilità con altri dispositivi non è ancora stata stabilita.
- La tua installazione Steam compatibile di Megabonk su un computer. Il programma di avvio controlla i file supportati specifici; un aggiornamento del gioco diverso potrebbe non essere compatibile.
- Spazio libero extra per ZIP, preparazione e dati di gioco. I requisiti variano in base al dispositivo. La preparazione può richiedere diversi minuti (circa quattro minuti in un test di iPlay 70 mini Pro).
- `OpenBonk.apk` nel pacchetto beta. Eseguire il backup di eventuali progressi dei test esistenti prima di sostituire un'installazione. Un APK firmato diversamente potrebbe richiedere un'installazione separata.

## Ottieni il pacchetto beta

Scarica `OpenBonk-beta.zip` dalla [versione beta](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.0-beta) ed estrailo sul tuo computer. Contiene `OpenBonk.apk`, una guida all'installazione e una cartella `tools`. Conserva l'intera cartella `tools` se hai bisogno dell'helper ZIP. L'APK è il programma di installazione; devi creare separatamente uno ZIP privato dalla tua copia Steam.

## Crea lo ZIP dei dati di gioco

1. In Steam, apri le proprietà di Megabonk e seleziona **File installati → Sfoglia**.
2. Trova la cartella `Megabonk_Data`. Su Windows, fai clic con il pulsante destro del mouse e scegli **Invia a → Cartella compressa (zippata)**; su Windows 11 potrebbe trovarsi in **Mostra altre opzioni**. Su Linux, utilizza l'opzione di compressione del file manager e seleziona ZIP. Comprimere l'intera cartella, non solo il suo contenuto: lo ZIP dovrebbe contenere una cartella `Megabonk_Data` con le relative sottocartelle. [Microsoft: comprimere e decomprimere file](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Assicurati che il codice ZIP non sia più grande di **1 GiB**. Collega e sblocca il tuo dispositivo Android, scegli **Trasferimento file** nella notifica USB e copia sia lo ZIP di Steam che lo `OpenBonk.apk` della beta in Download. Mantieni privato lo ZIP di Steam: contiene i file della tua copia del gioco. [Android: trasferimento di file tramite USB](https://support.google.com/android/answer/9064445?hl=en).

È stato testato uno ZIP di installazione Linux completo su Retroid e iPlay. Anche un ZIP più piccolo con i 39 file Windows supportati è stato importato su iPlay e ha raggiunto Forest. La preparazione dello ZIP su un PC Windows fisico e l'importazione di una cartella di installazione Windows completa non sono ancora state verificate.

Se lo ZIP completo supera 1 GiB, la cartella `tools` del pacchetto contiene un helper che convalida i 39 file richiesti e crea uno ZIP più piccolo. Su Windows, apri PowerShell nella cartella contenente `tools` ed esegui questo comando, sostituendo il primo percorso con la cartella del gioco:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Su Linux, dalla stessa cartella e con Python 3.9 o successivo:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

Su Windows, se è installato Python 3.9 o versione successiva, puoi anche eseguire l'helper Python da PowerShell:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Copia il codice ZIP dell'helper in Download su Android e selezionalo al posto del codice ZIP completo. Gli helper non sovrascrivono un ZIP esistente e rifiutano versioni di file non riconosciute. L'helper Python è stato eseguito utilizzando Windows Python sotto Wine e il suo ZIP è stato importato su iPlay. L'helper PowerShell è stato testato con PowerShell 7 su Linux. Nessuno dei due helper è stato testato su un PC Windows nativo.

## Installa e gioca

1. Apri `OpenBonk.apk` nel file manager di Android. Se vedi i dettagli del file, cerca **Apri con → Programma di installazione del pacchetto**. Se Android richiede l'autorizzazione per l'installazione da quell'app, leggi la richiesta e segui le sue istruzioni. Potrai disattivare tale autorizzazione in seguito.
2. Apri OpenBonk. Nel programma di avvio scegli **Controller** o **Touch**.
3. Seleziona **Import Steam game**, trova lo ZIP in Download e selezionalo. **Import prepared Android data** è per un bundle Android preparato in precedenza, non per Steam ZIP. Tieni l'app aperta e attendi **Data ready**. Il messaggio cambia tra le fasi, ma non viene mostrata alcuna percentuale.
4. Selezionare **Play**. Al primo avvio, il testo e le texture vengono importati prima che venga visualizzato il menu di gioco.

## Importa ed esegui il backup dei progressi

L'importazione di un profilo Steam è facoltativa. Per prima cosa chiudi il gioco sul tuo computer e conserva un backup. Trova la cartella dell'account in `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` su Linux o prova `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` su Windows (il percorso Windows non è stato verificato). ZIP `progression.json` e `stats.json` dallo stesso account insieme, copia lo ZIP su Android e scegli **Import profile ZIP**. L'avvio del gioco sostituisce i progressi dell'attuale account Android, quindi conserva il backup originale. Si tratta di un'importazione una tantum, non di una sincronizzazione da PC ad Android. Il profilo ZIP è privato; non allegarlo a un rapporto.

Per eseguire il backup dei progressi di Android, chiudi completamente il gioco, apri il launcher e seleziona **Export progress**. Scegli un profilo e salva lo ZIP in un posto privato. Puoi ripristinarlo utilizzando **Import profile ZIP**. Lo ZIP contiene progressi e statistiche, ma non controlli o mod. L'esportazione è stata testata su un iPlay con un profilo di prova; mantieni anche il backup originale di Steam.

**Backup Android:** questa versione non disabilita il Backup automatico. A seconda delle impostazioni del dispositivo, Android potrebbe includere dati delle app, come salvataggi o importazioni di profili in sospeso, nei backup degli account e nei trasferimenti del dispositivo. L'APK non richiede permessi Internet, ma Android può gestire i propri backup. Controlla le impostazioni di backup di Android se non lo desideri. La disinstallazione dell'app può eliminare i dati locali; eseguire prima il backup dei progressi. [Backup automatico Android](https://developer.android.com/identity/data/autobackup).

## Risoluzione dei problemi

- **ZIP incompatibile:** seleziona `Megabonk_Data` da un'installazione completa e una versione del gioco supportata. Gli aggiornamenti di Steam potrebbero modificare i file controllati dal launcher.
- **Spazio insufficiente:** libera spazio e riprova. Il launcher non calcola ancora in anticipo i requisiti di spazio totali.
- **L'importazione del testo non riesce all'avvio:** riapri il gioco per riprovare mentre lo ZIP in sospeso rimane memorizzato nella cache. Se Android ha svuotato la cache, seleziona nuovamente il codice ZIP.
- **L'APK non verrà installato su un'altra versione:** la firma o l'ID dell'applicazione potrebbero differire. Esegui il backup di tutti i progressi che desideri conservare prima di disinstallare un'app esistente.
- **Play Protect chiede di scansionare l'APK:** su un iPlay, Android si è offerto di inviare il file a Google per l'analisi prima dell'installazione. Leggi il messaggio e decidi se inviarlo. Il risultato di tale scansione e dell'installazione completa tramite il programma di installazione del sistema non sono ancora stati verificati.

## Segnala un problema

Includi il modello del dispositivo, la versione di Android, se hai utilizzato un controller o un touch, i passaggi che hai eseguito, cosa ti aspettavi e cosa è successo. Se utile, allega uno screenshot dell'errore. Non includere il codice postale, il profilo o le informazioni dell'account Steam in un rapporto ordinario.
