# Guide d'installation bêta d'OpenBonk

> Traduction automatique de l’anglais. Veuillez signaler des instructions peu claires.

> OpenBonk est un port communautaire non officiel en cours de test. Chaque testeur a besoin de sa propre copie Steam compatible de Megabonk.

## Ce dont vous avez besoin

- Un appareil Android 64 bits exécutant Android 8 ou version ultérieure. La bêta a été testée sur un Retroid Pocket 5 et un iPlay 70 mini Pro avec Android 15 ; la compatibilité avec d'autres appareils n'est pas encore établie.
- Votre propre installation Steam compatible de Megabonk sur un ordinateur. Le lanceur vérifie les fichiers spécifiques pris en charge ; une autre mise à jour du jeu peut ne pas être compatible.
- Espace libre supplémentaire pour le ZIP, la préparation et les données de jeu. Les exigences varient selon l'appareil. La préparation peut prendre plusieurs minutes (environ quatre minutes pour un test iPlay 70 mini Pro).
- Le `OpenBonk.apk` dans le package bêta. Sauvegardez toute progression de test existante avant de remplacer une installation. Un APK signé différemment peut nécessiter une installation distincte.

## Obtenez le package bêta

Téléchargez `OpenBonk-beta.zip` à partir de la [version bêta](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.1-beta) et extrayez-le sur votre ordinateur. Il contient `OpenBonk.apk`, un guide d'installation et un dossier `tools`. Conservez l'intégralité du dossier `tools` si vous avez besoin de l'assistant ZIP. L'APK est le programme d'installation ; vous devez créer séparément un ZIP privé à partir de votre propre copie Steam.

## Créez le ZIP des données du jeu

1. Dans Steam, ouvrez les propriétés de Megabonk et sélectionnez **Fichiers installés → Parcourir**.
2. Recherchez le dossier `Megabonk_Data`. Sous Windows, cliquez dessus avec le bouton droit et choisissez **Envoyer vers → Dossier compressé (zippé)** ; sous Windows 11, cela peut se trouver sous **Afficher plus d'options**. Sous Linux, utilisez l'option de compression de votre gestionnaire de fichiers et sélectionnez ZIP. Compressez l'intégralité du dossier, pas seulement son contenu : le ZIP doit contenir un dossier `Megabonk_Data` avec ses sous-dossiers. [Microsoft : compresser et décompresser les fichiers](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Assurez-vous que le ZIP ne dépasse pas **1 Gio**. Connectez et déverrouillez votre appareil Android, choisissez **Transfert de fichiers** dans sa notification USB et copiez le ZIP Steam et le `OpenBonk.apk` de la version bêta dans Téléchargements. Gardez le ZIP Steam privé : il contient des fichiers de votre copie de jeu. [Android : transférer des fichiers par USB](https://support.google.com/android/answer/9064445?hl=en).

Un ZIP d'installation Linux complet a été testé sur Retroid et iPlay. Un ZIP plus petit contenant les 39 fichiers Windows pris en charge a également été importé sur iPlay et a atteint Forest. La préparation du ZIP sur un PC Windows physique et l'importation d'un dossier d'installation Windows complet n'ont pas encore été vérifiées.

Si le ZIP complet dépasse 1 Gio, le dossier `tools` du package contient un assistant qui valide les 39 fichiers requis et crée un ZIP plus petit. Sous Windows, ouvrez PowerShell dans le dossier contenant `tools` et exécutez cette commande en remplaçant le premier chemin par votre dossier de jeu :

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Sous Linux, depuis le même dossier et avec Python 3.9 ou version ultérieure :

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

Sous Windows, si Python 3.9 ou version ultérieure est installé, vous pouvez également exécuter l'assistant Python à partir de PowerShell :

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Copiez le ZIP de l'assistant dans Téléchargements sur Android et sélectionnez-le au lieu du ZIP complet. Les assistants n'écrasent pas un ZIP existant et ne rejettent pas les versions de fichiers non reconnues. L'assistant Python a été exécuté à l'aide de Windows Python sous Wine et son ZIP a été importé sur iPlay. L'assistant PowerShell a été testé avec PowerShell 7 sous Linux. Aucun des deux assistants n’a été testé sur un PC Windows natif.

## Installer et jouer

1. Ouvrez `OpenBonk.apk` dans le gestionnaire de fichiers d'Android. Si vous voyez les détails du fichier, recherchez **Ouvrir avec → Programme d'installation du package**. Si Android demande l'autorisation d'installer à partir de cette application, lisez l'invite et suivez ses instructions. Vous pouvez désactiver cette autorisation par la suite.
2. Ouvrez OpenBonk. Dans le lanceur, choisissez **Controller** ou **Touch**.
3. Sélectionnez **Megabonk game files ZIP**, recherchez le ZIP dans Téléchargements et sélectionnez-le. **Import prepared Android data** est destiné à un bundle Android préalablement préparé, et non au Steam ZIP. Gardez l'application ouverte et attendez **Data ready**. Le message change entre les étapes, mais aucun pourcentage n'est affiché.
4. Sélectionnez **Play**. Au premier lancement, le texte et les textures sont importés avant que le menu du jeu n'apparaisse.

## Importer et sauvegarder la progression

L'importation d'un profil Steam est facultative. Fermez d’abord le jeu sur votre ordinateur et conservez une sauvegarde. Recherchez le dossier du compte dans `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` sous Linux ou essayez `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` sous Windows (le chemin Windows n'a pas été vérifié). ZIP `progression.json` et `stats.json` du même compte ensemble, copiez le ZIP sur Android et choisissez **Import profile ZIP**. Le démarrage du jeu remplace la progression du compte Android actuel, alors conservez votre sauvegarde d'origine. Il s'agit d'une importation unique et non d'une synchronisation PC vers Android. Le profil ZIP est privé ; ne le joignez pas à un rapport.

Pour sauvegarder la progression d'Android, fermez complètement le jeu, ouvrez le lanceur et sélectionnez **Export profile**. Choisissez un profil et enregistrez le ZIP dans un endroit privé. Vous pouvez le restaurer en utilisant **Import profile ZIP**. Le ZIP contient des progrès et des statistiques, mais pas de contrôles ou de mods. L'export a été testé sur un iPlay avec un profil de test ; conservez également la sauvegarde Steam originale.

**Sauvegardes Android :** cette version ne désactive pas la sauvegarde automatique. En fonction des paramètres de l'appareil, Android peut inclure des données d'application, telles que des sauvegardes ou une importation de profil en attente, dans les sauvegardes de compte et les transferts d'appareils. Le lanceur utilise Internet pour les mises à jour et le catalogue de mods. Vérifiez les paramètres de sauvegarde d'Android si vous ne le souhaitez pas. La désinstallation de l'application peut supprimer les données locales ; sauvegardez d’abord la progression. [Sauvegarde automatique Android](https://developer.android.com/identity/data/autobackup).

## Dépannage

- **ZIP incompatible :** sélectionnez `Megabonk_Data` à partir d'une installation complète et d'une version de jeu prise en charge. Les mises à jour Steam peuvent modifier les fichiers vérifiés par le lanceur.
- **Espace insuffisant :** libérez de l'espace et réessayez. Le lanceur ne calcule pas encore à l’avance l’encombrement total.
- **L'importation de texte échoue au lancement :** rouvrez le jeu pour réessayer pendant que le ZIP en attente reste en cache. Si Android a vidé le cache, sélectionnez à nouveau le ZIP.
- **L'APK ne s'installera pas sur une autre version :** sa signature ou son ID d'application peuvent différer. Sauvegardez toute progression que vous souhaitez conserver avant de désinstaller une application existante.
- **Play Protect demande de scanner l'APK :** sur un iPlay, Android a proposé d'envoyer le fichier à Google pour analyse avant l'installation. Lisez l'invite et décidez si vous souhaitez l'envoyer. Le résultat de cette analyse et une installation complète via le programme d'installation du système n'ont pas encore été vérifiés.

## Signaler un problème

Incluez le modèle de l'appareil, la version Android, si vous avez utilisé une manette ou un appareil tactile, les étapes que vous avez suivies, ce à quoi vous vous attendiez et ce qui s'est passé. Joignez une capture d'écran d'erreur si cela est utile. N'incluez pas votre ZIP Steam, vos informations de profil ou de compte dans un rapport ordinaire.
