# Przewodnik instalacji wersji beta OpenBonk

> Tłumaczenie maszynowe z języka angielskiego. Prosimy o zgłaszanie niejasnych instrukcji.

> OpenBonk to nieoficjalny port społecznościowy do testów. Każdy tester potrzebuje własnej, kompatybilnej kopii Steam Megabonk.

## Czego potrzebujesz

- 64-bitowe urządzenie z systemem Android z systemem Android 8 lub nowszym. Wersja beta została przetestowana na Retroid Pocket 5 i iPlay 70 mini Pro z Androidem 15; kompatybilność z innymi urządzeniami nie została jeszcze ustalona.
- Twoja własna, kompatybilna instalacja Steam Megabonk na komputerze. Program uruchamiający sprawdza określone obsługiwane pliki; inna aktualizacja gry może nie być kompatybilna.
- Dodatkowe wolne miejsce na dane ZIP, przygotowania i gry. Wymagania różnią się w zależności od urządzenia. Przygotowanie może zająć kilka minut (około czterech minut w jednym teście iPlay 70 mini Pro).
- `OpenBonk.apk` w pakiecie beta. Przed wymianą instalacji wykonaj kopię zapasową istniejącego postępu testów. Inaczej podpisany plik APK może wymagać osobnej instalacji.

## Zdobądź pakiet beta

Pobierz `OpenBonk-beta.zip` z [wersji beta](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.0-beta) i rozpakuj go na swoim komputerze. Zawiera `OpenBonk.apk`, instrukcję instalacji i folder `tools`. Zachowaj cały folder `tools`, jeśli potrzebujesz pomocnika ZIP. APK jest instalatorem; musisz osobno utworzyć prywatny plik ZIP z własnej kopii Steam.

## Utwórz plik ZIP z danymi gry

1. W Steam otwórz właściwości Megabonk i wybierz **Zainstalowane pliki → Przeglądaj**.
2. Znajdź folder `Megabonk_Data`. W systemie Windows kliknij go prawym przyciskiem myszy i wybierz **Wyślij do → Folder skompresowany (zipowany)**; w systemie Windows 11 może to znajdować się w obszarze **Pokaż więcej opcji**. W systemie Linux użyj opcji kompresji menedżera plików i wybierz ZIP. Skompresuj cały folder, a nie tylko jego zawartość: plik ZIP powinien zawierać folder `Megabonk_Data` wraz z podfolderami. [Microsoft: spakuj i rozpakuj pliki](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Upewnij się, że plik ZIP nie jest większy niż **1 GiB**. Podłącz i odblokuj swoje urządzenie z Androidem, wybierz **Transfer plików** w powiadomieniu USB i skopiuj zarówno ZIP Steam, jak i `OpenBonk.apk` wersji beta do Pobranych. Zachowaj prywatność Steam ZIP: zawiera pliki z twojej kopii gry. [Android: przesyłaj pliki przez USB](https://support.google.com/android/answer/9064445?hl=en).

Pełna instalacja ZIP Linuksa została przetestowana na Retroid i iPlay. Mniejszy ZIP z 39 obsługiwanymi plikami Windows również został zaimportowany do iPlay i dotarł do Forest. Przygotowanie pliku ZIP na fizycznym komputerze z systemem Windows i importowanie pełnego folderu instalacyjnego systemu Windows nie zostało jeszcze zweryfikowane.

Jeśli pełny rozmiar ZIP przekracza 1 GiB, folder `tools` pakietu zawiera pomocnika, który sprawdza poprawność 39 wymaganych plików i tworzy mniejszy plik ZIP. W systemie Windows otwórz PowerShell w folderze zawierającym `tools` i uruchom to polecenie, zastępując pierwszą ścieżkę folderem gry:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

W systemie Linux z tego samego folderu i w języku Python 3.9 lub nowszym:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

W systemie Windows, jeśli zainstalowany jest Python 3.9 lub nowszy, możesz także uruchomić pomocnika Pythona z PowerShell:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Skopiuj plik ZIP pomocnika do Pobrane na Androida i wybierz go zamiast pełnego pliku ZIP. Pomocnicy nie nadpisują istniejącego pliku ZIP i odrzucają nierozpoznane wersje plików. Pomocnik Pythona został uruchomiony przy użyciu Windows Python w Wine, a jego plik ZIP został zaimportowany do iPlay. Pomocnik programu PowerShell został przetestowany w programie PowerShell 7 w systemie Linux. Żaden z pomocników nie został przetestowany na natywnym komputerze z systemem Windows.

## Zainstaluj i graj

1. Otwórz `OpenBonk.apk` w menedżerze plików Androida. Jeśli widzisz szczegóły pliku, poszukaj **Otwórz za pomocą → Instalator pakietu**. Jeśli system Android poprosi o pozwolenie na instalację z tej aplikacji, przeczytaj monit i postępuj zgodnie z zawartymi w nim instrukcjami. Możesz później wyłączyć to uprawnienie.
2. Otwórz OpenBonka. W launcherze wybierz **Controller** lub **Touch**.
3. Wybierz **Import Steam game**, znajdź plik ZIP w sekcji Pobrane i wybierz go. **Import prepared Android data** dotyczy wcześniej przygotowanego pakietu Androida, a nie Steam ZIP. Pozostaw aplikację otwartą i poczekaj na **Data ready**. Komunikat zmienia się pomiędzy etapami, ale nie jest wyświetlana wartość procentowa.
4. Wybierz **Play**. Przy pierwszym uruchomieniu tekst i tekstury są importowane, zanim pojawi się menu gry.

## Importuj i twórz kopie zapasowe postępu

Importowanie profilu Steam jest opcjonalne. Najpierw zamknij grę na swoim komputerze i zachowaj kopię zapasową. Znajdź folder konta pod adresem `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` w systemie Linux lub wypróbuj `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` w systemie Windows (ścieżka systemu Windows nie została zweryfikowana). ZIP `progression.json` i `stats.json` z tego samego konta razem, skopiuj ZIP do Androida i wybierz **Import profile ZIP**. Uruchomienie gry zastępuje postęp bieżącego konta Android, dlatego zachowaj oryginalną kopię zapasową. Jest to jednorazowy import, a nie synchronizacja między komputerem a systemem Android. Profil ZIP jest prywatny; nie dołączaj go do raportu.

Aby wykonać kopię zapasową postępu Androida, zamknij całkowicie grę, otwórz program uruchamiający i wybierz **Export progress**. Wybierz profil i zapisz plik ZIP w prywatnym miejscu. Możesz go przywrócić za pomocą **Import profile ZIP**. ZIP zawiera postęp i statystyki, ale nie zawiera elementów sterujących ani modów. Eksport został przetestowany na iPlay z profilem testowym; zachowaj także oryginalną kopię zapasową Steam.

**Kopie zapasowe Androida:** ta wersja nie wyłącza Automatycznej kopii zapasowej. W zależności od ustawień urządzenia Android może uwzględniać dane aplikacji, takie jak zapisane dane lub oczekujący import profilu, w kopiach zapasowych kont i transferach urządzeń. Plik APK nie żąda pozwolenia na Internet, ale Android może zarządzać własnymi kopiami zapasowymi. Przejrzyj ustawienia kopii zapasowych Androida, jeśli tego nie chcesz. Odinstalowanie aplikacji może spowodować usunięcie danych lokalnych; najpierw wykonaj kopię zapasową postępu. [Automatyczna kopia zapasowa Androida](https://developer.android.com/identity/data/autobackup).

## Rozwiązywanie problemów

- **Niekompatybilny ZIP:** wybierz `Megabonk_Data` z pełnej instalacji i obsługiwanej wersji gry. Aktualizacje Steam mogą zmieniać pliki sprawdzane przez program uruchamiający.
- **Za mało miejsca:** zwolnij miejsce i spróbuj ponownie. Program uruchamiający nie oblicza jeszcze z góry całkowitego zapotrzebowania na miejsce.
- **Import tekstu nie powiódł się przy uruchomieniu:** otwórz ponownie grę, aby ponowić próbę, podczas gdy oczekujący plik ZIP pozostaje w pamięci podręcznej. Jeśli Android wyczyścił pamięć podręczną, wybierz ponownie ZIP.
- **APK nie zostanie zainstalowany na innej wersji:** jego podpis lub identyfikator aplikacji może się różnić. Przed odinstalowaniem istniejącej aplikacji wykonaj kopię zapasową wszelkich postępów, które chcesz zachować.
- **Play Protect prosi o zeskanowanie pliku APK:** w jednym z urządzeń iPlay system Android zaproponował wysłanie pliku do Google w celu analizy przed instalacją. Przeczytaj monit i zdecyduj, czy go wysłać. Wynik tego skanowania i pełnej instalacji za pomocą instalatora systemowego nie zostały jeszcze zweryfikowane.

## Zgłoś problem

Podaj model urządzenia, wersję Androida, czy korzystałeś z kontrolera czy dotyku, jakie kroki wykonałeś, czego oczekiwałeś i co się stało. Jeśli jest to przydatne, załącz zrzut ekranu błędu. Nie dołączaj swojego ZIP-u Steam, profilu ani informacji o koncie do zwykłego raportu.
