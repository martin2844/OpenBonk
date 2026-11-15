# OpenBonk beta kurulum kılavuzu

> İngilizce'den makine çevirisi. Lütfen belirsiz talimatları bildirin.

> OpenBonk, test aşamasında olan resmi olmayan bir topluluk limanıdır. Her testçinin Megabonk'un kendi uyumlu Steam kopyasına ihtiyacı vardır.

## İhtiyacınız olan şey

- Android 8 veya üzerini çalıştıran 64 bit Android cihazı. Beta, Retroid Pocket 5 ve Android 15 yüklü iPlay 70 mini Pro üzerinde test edildi; diğer cihazlarla uyumluluk henüz kurulmamıştır.
- Bir bilgisayarda Megabonk'un kendi uyumlu Steam kurulumunuz. Başlatıcı, desteklenen belirli dosyaları kontrol eder; farklı bir oyun güncellemesi uyumlu olmayabilir.
- ZIP, hazırlık ve oyun verileri için ekstra boş alan. Gereksinimler cihaza göre değişir. Hazırlık birkaç dakika sürebilir (bir iPlay 70 mini Pro testinde yaklaşık dört dakika).
- Beta paketindeki `OpenBonk.apk`. Bir kurulumu değiştirmeden önce mevcut tüm test ilerlemelerini yedekleyin. Farklı şekilde imzalanmış bir APK, ayrı bir kurulum gerektirebilir.

## Beta paketini edinin

`OpenBonk-beta.zip`'yi [beta sürümünden](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.0-beta) indirin ve bilgisayarınıza çıkarın. `OpenBonk.apk`, bir kurulum kılavuzu ve bir `tools` klasörü içerir. ZIP yardımcısına ihtiyacınız varsa `tools` klasörünün tamamını saklayın. APK yükleyicidir; kendi Steam kopyanızdan ayrı olarak özel bir ZIP oluşturmalısınız.

## Oyun verileri ZIP'ini oluşturun

1. Steam'de Megabonk'un özelliklerini açın ve **Yüklü Dosyalar → Gözat**'ı seçin.
2. `Megabonk_Data` klasörünü bulun. Windows'ta sağ tıklayın ve **Şuraya gönder → Sıkıştırılmış (sıkıştırılmış) klasör** seçeneğini seçin; Windows 11'de bu **Daha fazla seçenek göster** altında olabilir. Linux'ta dosya yöneticinizin sıkıştırma seçeneğini kullanın ve ZIP'i seçin. Yalnızca içeriğini değil, tüm klasörü sıkıştırın: ZIP, alt klasörleriyle birlikte bir `Megabonk_Data` klasörü içermelidir. [Microsoft: dosyaları sıkıştırın ve sıkıştırın](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. ZIP'in **1 GiB**'den büyük olmadığından emin olun. Android cihazınızı bağlayın ve kilidini açın, USB bildiriminde **Dosya aktarımı** seçeneğini seçin ve hem Steam ZIP'i hem de betanın `OpenBonk.apk` dosyasını İndirilenler'e kopyalayın. Steam ZIP'ini gizli tutun: oyun kopyanızdaki dosyaları içerir. [Android: dosyaları USB ile aktarın](https://support.google.com/android/answer/9064445?hl=en).

Tam bir Linux kurulum ZIP'i Retroid ve iPlay'de test edildi. Desteklenen 39 Windows dosyasını içeren daha küçük bir ZIP de iPlay'e aktarıldı ve Forest'a ulaştı. ZIP'in fiziksel bir Windows PC'de hazırlanması ve tam Windows kurulum klasörünün içe aktarılması henüz doğrulanmadı.

Tam ZIP 1 GiB'yi aşarsa paketin `tools` klasörü gerekli 39 dosyayı doğrulayan ve daha küçük bir ZIP oluşturan bir yardımcı içerir. Windows'ta, `tools` içeren klasörde PowerShell'i açın ve ilk yolu oyun klasörünüzle değiştirerek bu komutu çalıştırın:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Linux'ta, aynı klasörden ve Python 3.9 veya sonraki sürümlerde:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

Windows'ta Python 3.9 veya üzeri yüklüyse Python yardımcısını PowerShell'den de çalıştırabilirsiniz:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Yardımcının ZIP'ini Android'deki İndirilenler'e kopyalayın ve tam ZIP yerine onu seçin. Yardımcılar mevcut bir ZIP'in üzerine yazmaz ve tanınmayan dosya sürümlerini reddetmez. Python yardımcısı, Wine altında Windows Python kullanılarak çalıştırıldı ve ZIP'i iPlay'e aktarıldı. PowerShell yardımcısı Linux'ta PowerShell 7 ile test edildi. Her iki yardımcı da yerel bir Windows PC'de test edilmemiştir.

## Yükleyin ve oynayın

1. Android'in dosya yöneticisinde `OpenBonk.apk`'yi açın. Dosya ayrıntılarını görüyorsanız **Birlikte aç → Paket yükleyici** seçeneğini arayın. Android söz konusu uygulamadan yükleme yapmak için izin isterse istemi okuyun ve talimatları izleyin. Daha sonra bu izni kapatabilirsiniz.
2. OpenBonk'u açın. Başlatıcıda **Controller** veya **Touch**'yi seçin.
3. **Import Steam game**'yi seçin, İndirilenler'de ZIP'i bulun ve seçin. **Import prepared Android data**, Steam ZIP için değil, önceden hazırlanmış bir Android paketi içindir. Uygulamayı açık tutun ve **Data ready**'yi bekleyin. Mesaj aşamalar arasında değişir ancak yüzde gösterilmez.
4. **Play**'yi seçin. İlk başlatmada, oyun menüsü görünmeden önce metin ve dokular içe aktarılır.

## İlerlemeyi içe aktarın ve yedekleyin

Steam profilinin içe aktarılması isteğe bağlıdır. Öncelikle oyunu bilgisayarınızdan kapatın ve yedeğini alın. Linux'ta `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` adresindeki hesap klasörünü bulun veya Windows'ta `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\`'yi deneyin (Windows yolu doğrulanmadı). ZIP `progression.json` ve `stats.json`'yi aynı hesaptan birlikte kullanın, ZIP'i Android'e kopyalayın ve **Import profile ZIP**'yi seçin. Oyunun başlatılması mevcut Android hesabının ilerlemesinin yerini alır, bu nedenle orijinal yedeğinizi saklayın. Bu, PC'den Android'e senkronizasyon değil, tek seferlik bir içe aktarma işlemidir. Profil ZIP'i özeldir; bunu bir rapora eklemeyin.

Android ilerlemesini yedeklemek için oyunu tamamen kapatın, başlatıcıyı açın ve **Export progress**'yi seçin. Bir profil seçin ve ZIP'i özel bir yere kaydedin. **Import profile ZIP** kullanarak geri yükleyebilirsiniz. ZIP, ilerleme ve istatistikleri içerir, ancak kontrolleri veya modları içermez. Dışa aktarma, bir test profiliyle iPlay'de test edildi; Orijinal Steam yedeğini de saklayın.

**Android yedeklemeleri:** bu sürüm Otomatik Yedekleme'yi devre dışı bırakmaz. Cihaz ayarlarına bağlı olarak Android, hesap yedeklemelerine ve cihaz aktarımlarına kaydetmeler veya beklemedeki profillerin içe aktarılması gibi uygulama verilerini dahil edebilir. APK İnternet izni istemez ancak Android kendi yedeklemelerini yönetebilir. Bunu istemiyorsanız Android'in yedekleme ayarlarını inceleyin. Uygulamanın kaldırılması yerel verileri silebilir; Öncelikle ilerlemeyi yedekleyin. [Android Otomatik Yedekleme](https://developer.android.com/identity/data/autobackup).

## Sorun giderme

- **Uyumsuz ZIP:** Tam kurulumdan ve desteklenen oyun sürümünden `Megabonk_Data`'yi seçin. Steam güncellemeleri, başlatıcı tarafından kontrol edilen dosyaları değiştirebilir.
- **Yeterli alan yok:** alanı boşaltın ve tekrar deneyin. Başlatıcı henüz toplam alan gereksinimlerini önceden hesaplamaz.
- **Metin içe aktarma başlatma sırasında başarısız oluyor:** Bekleyen ZIP önbellekte kalırken oyunu yeniden açarak yeniden deneyin. Android önbelleği temizlediyse ZIP'i tekrar seçin.
- **APK başka bir sürümün üzerine yüklenmeyecektir:** imzası veya uygulama kimliği farklı olabilir. Mevcut bir uygulamayı kaldırmadan önce tutmak istediğiniz ilerlemeyi yedekleyin.
- **Play Protect, APK'nın taranmasını ister:** bir iPlay'de Android, kurulumdan önce dosyayı analiz için Google'a göndermeyi teklif etti. İstemi okuyun ve gönderip göndermeyeceğinize karar verin. Bu taramanın sonucu ve sistem yükleyici aracılığıyla tam kurulum henüz doğrulanmadı.

## Sorun bildir

Cihaz modelini, Android sürümünü, kumandayı mı yoksa dokunmayı mı kullandığınızı, attığınız adımları, ne beklediğinizi ve ne olduğunu ekleyin. Yararlıysa bir hata ekran görüntüsü ekleyin. Steam ZIP'inizi, profilinizi veya hesap bilgilerinizi sıradan bir rapora dahil etmeyin.
