# Průvodce instalací beta verze OpenBonk

> Strojový překlad z angličtiny. Nahlašte prosím nejasné pokyny.

> OpenBonk je neoficiální komunitní port v testování. Každý tester potřebuje svou vlastní kompatibilní Steam kopii Megabonku.

## Co potřebujete

- 64bitové zařízení Android se systémem Android 8 nebo novějším. Beta byla testována na Retroid Pocket 5 a iPlay 70 mini Pro s Androidem 15; kompatibilita s jinými zařízeními zatím nebyla stanovena.
- Vaše vlastní kompatibilní Steam instalace Megabonk na počítači. Spouštěč kontroluje konkrétní podporované soubory; jiná aktualizace hry nemusí být kompatibilní.
- Extra volné místo pro ZIP, příprava a herní data. Požadavky se liší podle zařízení. Příprava může trvat několik minut (asi čtyři minuty v jednom testu iPlay 70 mini Pro).
- `OpenBonk.apk` v balíčku beta. Před výměnou instalace zálohujte veškerý stávající průběh testu. Odlišně podepsaný soubor APK může vyžadovat samostatnou instalaci.

## Získejte beta balíček

Stáhněte si `OpenBonk-beta.zip` z [beta verze](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.0-beta) a rozbalte jej do počítače. Obsahuje `OpenBonk.apk`, instalační příručku a složku `tools`. Pokud potřebujete pomocníka ZIP, ponechte si celou složku `tools`. APK je instalační program; musíte samostatně vytvořit soukromý ZIP z vaší vlastní kopie Steam.

## Vytvořte ZIP s daty hry

1. Ve službě Steam otevřete vlastnosti Megabonku a vyberte **Nainstalované soubory → Procházet**.
2. Najděte složku `Megabonk_Data`. V systému Windows na něj klikněte pravým tlačítkem a vyberte **Odeslat do → Komprimovaná (zazipovaná) složka**; ve Windows 11 to může být pod **Zobrazit další možnosti**. V systému Linux použijte možnost komprese správce souborů a vyberte ZIP. Zkomprimujte celou složku, nejen její obsah: ZIP by měl obsahovat složku `Megabonk_Data` s jejími podsložkami. [Microsoft: zip a rozbalte soubory](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Ujistěte se, že ZIP není větší než **1 GiB**. Připojte a odemkněte své zařízení Android, v oznámení USB zvolte **Přenos souborů** a zkopírujte Steam ZIP a `OpenBonk.apk` beta do složky Stahování. Udržujte Steam ZIP soukromý: obsahuje soubory z vaší kopie hry. [Android: přenos souborů přes USB](https://support.google.com/android/answer/9064445?hl=en).

Kompletní instalační ZIP Linuxu byl testován na Retroid a iPlay. Menší ZIP s 39 podporovanými soubory Windows byl také importován na iPlay a dostal se do Forest. Příprava ZIP na fyzickém počítači s Windows a import úplné instalační složky Windows ještě nebyly ověřeny.

Pokud celý ZIP přesáhne 1 GiB, složka `tools` balíčku obsahuje pomocníka, který ověří 39 požadovaných souborů a vytvoří menší ZIP. V systému Windows otevřete PowerShell ve složce obsahující `tools` a spusťte tento příkaz, přičemž první cestu nahraďte složkou vaší hry:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

V Linuxu ze stejné složky a s Pythonem 3.9 nebo novějším:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

V systému Windows, pokud je nainstalován Python 3.9 nebo novější, můžete také spustit pomocníka Pythonu z PowerShellu:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Zkopírujte soubor ZIP pomocníka do složky Stahování v systému Android a vyberte jej namísto úplného souboru ZIP. Pomocníci nepřepisují existující ZIP a neodmítají nerozpoznané verze souborů. Pomocník Pythonu byl spuštěn pomocí Windows Python pod Wine a jeho ZIP byl importován na iPlay. Pomocník PowerShell byl testován s PowerShell 7 na Linuxu. Žádný pomocník nebyl testován na nativním počítači s Windows.

## Nainstalujte a hrajte

1. Otevřete `OpenBonk.apk` ve správci souborů Android. Pokud vidíte podrobnosti o souboru, vyhledejte **Otevřít pomocí → Instalační program balíčku**. Pokud Android požádá o povolení k instalaci z této aplikace, přečtěte si výzvu a postupujte podle jejích pokynů. Toto oprávnění můžete později vypnout.
2. Otevřete OpenBonk. Ve spouštěči vyberte **Controller** nebo **Touch**.
3. Vyberte **Import Steam game**, vyhledejte ZIP v části Stahování a vyberte jej. **Import prepared Android data** je pro dříve připravený balíček Android, nikoli pro Steam ZIP. Nechte aplikaci otevřenou a počkejte na **Data ready**. Zpráva se mezi jednotlivými fázemi mění, ale nezobrazuje se žádné procento.
4. Vyberte **Play**. Při prvním spuštění se text a textury importují, než se objeví herní menu.

## Importujte a zálohujte průběh

Import profilu Steam je volitelný. Nejprve zavřete hru na počítači a ponechte si zálohu. Najděte složku účtu na adrese `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` v systému Linux nebo vyzkoušejte `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` v systému Windows (cesta systému Windows nebyla ověřena). ZIP `progression.json` a `stats.json` ze stejného účtu společně, zkopírujte ZIP do Androidu a vyberte **Import profile ZIP**. Spuštění hry nahradí postup aktuálního účtu Android, proto si ponechte původní zálohu. Jedná se o jednorázový import, nikoli o synchronizaci mezi PC a Androidem. ZIP profilu je soukromý; nepřipojujte jej ke zprávě.

Chcete-li zálohovat postup Androidu, úplně zavřete hru, otevřete spouštěč a vyberte **Export progress**. Vyberte profil a uložte ZIP někam do soukromí. Můžete jej obnovit pomocí **Import profile ZIP**. ZIP obsahuje pokrok a statistiky, ale ne ovládací prvky nebo mody. Export byl testován na iPlay s testovacím profilem; ponechte si také původní zálohu Steam.

**Zálohy Android:** tato verze nezakazuje automatické zálohování. V závislosti na nastavení zařízení může Android do zálohování účtů a přenosů zařízení zahrnout data aplikací, jako jsou uložení nebo nevyřízený import profilu. Soubor APK nepožaduje povolení k internetu, ale Android může spravovat své vlastní zálohy. Pokud si to nepřejete, zkontrolujte nastavení zálohování systému Android. Odinstalováním aplikace můžete odstranit místní data; nejprve zálohovat pokrok. [Automatické zálohování Android](https://developer.android.com/identity/data/autobackup).

## Odstraňování problémů

- **Nekompatibilní ZIP:** vyberte `Megabonk_Data` z kompletní instalace a podporované verze hry. Aktualizace Steam mohou změnit soubory kontrolované spouštěčem.
- **Nedostatek místa:** uvolněte místo a zkuste to znovu. Odpalovací zařízení zatím předem nepočítá celkové nároky na prostor.
- **Import textu selže při spuštění:** znovu otevřete hru a zkuste to znovu, zatímco čekající ZIP zůstane v mezipaměti. Pokud Android vymazal mezipaměť, vyberte znovu ZIP.
- **Soubor APK se nenainstaluje přes jinou verzi:** jeho podpis nebo ID aplikace se mohou lišit. Před odinstalováním existující aplikace si zálohujte veškerý pokrok, který si chcete zachovat.
- **Play Protect žádá o naskenování souboru APK:** na jednom iPlay nabídl Android před instalací odeslání souboru k analýze do Googlu. Přečtěte si výzvu a rozhodněte se, zda ji odeslat. Výsledek tohoto skenování a kompletní instalace prostřednictvím instalačního programu systému ještě nebyly ověřeny.

## Nahlásit problém

Uveďte model zařízení, verzi Androidu, zda jste použili ovladač nebo dotyk, kroky, které jste podnikli, co jste očekávali a co se stalo. Pokud je to užitečné, připojte snímek obrazovky s chybou. Nezahrnujte svůj Steam ZIP, profil nebo informace o účtu do běžné zprávy.
