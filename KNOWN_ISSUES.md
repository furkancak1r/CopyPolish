### Known Issues & Fix Log

#### Outlook Table StoreID column breaks preview {#outlook-table-storeid-column-breaks-preview}
- Date: 2025-11-26T15:40:00Z
- Context: main/local-Windows/VS2022-VSTO
- Error signature: `Klasör hatası (Gelen Kutusu): "StoreID" özelliği bilinmiyor.`
- Symptoms/Impact: Gelişmiş arama tek sonuç döndürse bile önizleme boş kalıyor; bazı sorgularda sonuç listesi 0'a düşüyor.
- Root cause: Outlook Table API'ye mevcut olmayan `StoreID` kolonu eklendiği için COM hata veriyor ve sonuçlar/önizleme yüklenemiyor.
- Resolution: Table'dan StoreID kolonu ekleme/okuma kaldırıldı, preview için folder.StoreID kullanıldı ve ilk sonuç otomatik seçilip önizleme tetikleniyor.
- Prevent recurrence:
  - Table API'ye yalnızca desteklenen kolonları ekle; yeni alan eklerken Outlook şemasını kontrol et.
  - Preview için StoreID'yi folder nesnesinden al, satır kolonlarına bağımlı kılma.
  - Tek sonuç senaryosunda ilk satırı otomatik seçip önizlemeyi tetikle.
- Files/Commands touched: `CopyPolish/SearchForm.cs`, `MSBuild ... /t:Publish`, `dist/CopyPolish.exe`
- References: n/a

#### ClickOnce manifest path duplication blocks install {#clickonce-manifest-path-duplication}
- Date: 2025-11-25T11:57:30Z
- Context: unknown-branch/local-Windows/MSBuild-17.14
- Error signature: `DeploymentDownloadException: ...dist/Application Files/CopyPolish_1_0_0_5/Application Files/CopyPolish_1_0_0_5/CopyPolish.dll.manifest` not found
- Symptoms/Impact: VSTO add-in install fails; “Arama” butonu ve güncel eklenti yüklenemiyor.
- Root cause: ClickOnce publish çıktı yolunda “Application Files” klasörü iki kez yer alacak şekilde manifest/codebase oluşması (eski publish kalıntıları + yerel dosyadan kurulum).
- Resolution: dist temizlenip `BootstrapperEnabled=false` ile yeniden publish edilerek yeni `CopyPolish/dist/CopyPolish.vsto` üretildi; kurulum kök VSTO’dan çalıştırılıyor.
- Prevent recurrence:
  - Publish öncesi `dist` ve `bin/Release/app.publish` klasörlerini temizle.
  - ClickOnce kurulumunu her zaman kök `CopyPolish/dist/CopyPolish.vsto` üzerinden başlat, iç içe klasörlerden değil.
  - PublishDir/InstallUrl ayarlarını tek konuma sabitle, web bootstrapper kapalı tut.
- Files/Commands touched: `CopyPolish/CopyPolish.csproj` (publish ayarları), `CopyPolish/dist` (yeniden üretim), `MSBuild ... /t:Publish /p:BootstrapperEnabled=false`
- References: n/a


#### Gelişmiş arama Türkçe karakter ve stabilite sorunları {#advanced-search-turkish-chars-stability}
- Date: 2025-11-25T14:30:00Z
- Context: main/local-Windows/VS2022-VSTO
- Error signature: `"hayırlı olsun" araması sonuç getirmiyordu; ToLowerInvariant() Türkçe i/İ, ı/I karakterlerini yanlış dönüştürüyordu`
- Symptoms/Impact: Türkçe karakterli aramalarda eksik sonuçlar, SQL injection riski, COM object memory leak, form kapanınca arka plan araması devam etmesi.
- Root cause: `ToLowerInvariant()` İngilizce culture kullanıyordu; SQL filtrede özel karakterler escape edilmiyordu; Row COM nesneleri release edilmiyordu.
- Resolution: Türkçe CultureInfo (`tr-TR`) ile büyük/küçük harf dönüşümü, SQL escape fonksiyonu, Row ReleaseComObject, FormClosing worker iptali, TreeView parent-child senkronizasyonu eklendi.
- Prevent recurrence:
  - Türkçe metin işlemlerinde daima `CultureInfo("tr-TR")` kullan.
  - SQL sorgu parametrelerini her zaman escape et (`'` → `''`, `%` → `[%]`).
  - COM nesnelerini (Row, Table, MailItem) try-finally ile release et.
- Files/Commands touched: `CopyPolish/SearchForm.cs` (ToTurkishLower, EscapeSqlString, TreeFolders_AfterCheck, FormClosing handler, Marshal.ReleaseComObject)
- References: n/a


#### Tam ifade araması Body içeriğinde sonuç getirmiyordu {#exact-phrase-search-body-not-working}
- Date: 2025-11-26T08:30:00Z
- Context: main/local-Windows/VS2022-VSTO-Outlook
- Error signature: `TurkishIndexOf(body, phrase) = -1` — Body null veya boş geliyordu, GetItemFromID çağrılmıyordu
- Symptoms/Impact: `"hayırlı olsun"` gibi tırnak içi tam ifade aramalarında mail içeriğinde bulunan eşleşmeler listelenmiyordu; SQL filtresi kelimeleri ayrı ayrı buluyordu ancak sonraki doğrulama aşamasında Body okunamıyordu.
- Root cause: Table'dan Body kolonu null döndüğünde GetItemFromID ile içeriği okuma mantığı tam ifade aramasında eksikti; sadece ek kontrolü için çağrılıyordu.
- Resolution: Tam ifade aramasında Body null/boş ise GetItemFromID ile mail içeriği okunup TurkishIndexOf kontrolü yapılacak şekilde kod genişletildi; debug loglama iyileştirildi.
- Prevent recurrence:
  - Table API'den Body kolonu null dönebilir; her zaman fallback olarak GetItemFromID kullan.
  - Tam ifade aramalarında tüm alanlarda (konu, gönderen, içerik, ek) tutarlı kontrol yap.
  - Debug loglarını ilk N satır için detaylı tut, sorun tespitini kolaylaştır.
- Files/Commands touched: `CopyPolish/SearchForm.cs` (isExactPhrase bloğunda Body null kontrolü ve GetItemFromID fallback eklendi)
- References: n/a


#### ClickOnce setup.exe oluşturma ve imzalama hatası {#clickonce-setup-exe-signing-error}
- Date: 2025-11-26T08:55:00Z
- Context: main/local-Windows/MSBuild-17.14.23/VS2022-Community
- Error signature: `MSB3482: SignTool Error: The file is being used by another process` ve `MSB3169: Unable to finish updating resource for setup.exe with error 8007006E`
- Symptoms/Impact: ClickOnce publish işlemi başarısız oluyor; setup.exe dosyası oluşturulamıyor veya imzalanamıyor.
- Root cause: Önceki publish işleminden kalan setup.exe dosyası başka bir işlem tarafından kilitli tutuluyordu; bootstrapper oluşturma sırasında kaynak güncelleme hatası meydana geldi.
- Resolution: BootstrapperEnabled=false parametresi ile publish yapılarak VSTO dosyaları oluşturuldu; ardından IExpress ile CopyPolish.vsto'yu içeren CopyPolish.exe self-extracting installer oluşturuldu.
- Prevent recurrence:
  - Publish öncesi dist klasörünü tamamen sil (`rm -rf dist`).
  - Kilitli dosya sorunu devam ederse farklı bir publish dizini kullan.
  - VSTO için standalone exe gerektiğinde IExpress veya benzeri araçlarla wrapper oluştur.
- Files/Commands touched: `CopyPolish.sed` (IExpress direktif dosyası), `output/install.bat`, MSBuild `-p:BootstrapperEnabled=false`, `iexpress /N CopyPolish.sed`
- References: n/a


#### IExpress SFX VSTO kurulumunda başarısız oluyor {#iexpress-sfx-vsto-install-failure}
- Date: 2025-11-26T09:10:00Z
- Context: main/local-Windows/IExpress/WinRAR-SFX
- Error signature: `<C:\Users\...\Temp\IXP000.TMP\CopyPolish.vsto> işlemini oluşturma hatası. Neden:` (boş neden mesajı)
- Symptoms/Impact: IExpress ile oluşturulan self-extracting EXE çalıştırıldığında VSTO kurulum hatası veriyor; neden boş geliyor.
- Root cause: IExpress sadece tek dosyayı (CopyPolish.vsto) paketliyor ancak VSTO kurulumu için yanında `Application Files` klasörü ve içindeki DLL/manifest dosyaları gerekli; geçici klasörden çalıştırıldığında manifest yolları bulunamıyor.
- Resolution: WinRAR SFX kullanılarak tüm dosyalar (CopyPolish.exe bootstrapper, CopyPolish.vsto, Application Files klasörü) tek bir self-extracting EXE'ye paketlendi; çıkarma sonrası otomatik kurulum başlatılıyor.
- Prevent recurrence:
  - VSTO dağıtımı için tüm dosyaları (vsto + Application Files + setup.exe) birlikte paketle.
  - IExpress yerine WinRAR SFX veya 7-Zip SFX kullan; daha iyi klasör yapısı desteği var.
  - Tek EXE gerektiğinde farklı publish dizinine oluştur, sonra WinRAR ile paketle.
- Files/Commands touched: `dist/sfx_config.txt`, `WinRAR.exe a -sfx -z"sfx_config.txt" CopyPolish.exe ...`, `CopyPolish.exe` (789 KB tek installer)
- References: n/a


#### ICO Kalite Sorunu {#ico-quality-issue}
- Date: 2025-11-26T12:57:00Z
- Context: main/local-Windows
- Error signature: `ICO dosyaları düşük kaliteli görünüyor, Outlook ribbon'da pixelleniyor`
- Symptoms/Impact: Kullanıcı arayüzünde kalitesiz ikon görünümü.
- Root cause: ICO formatının yetersizliği veya düşük çözünürlük kullanımı.
- Resolution: PNG formatına geçildi, yüksek çözünürlük kullanıldı (64x64), GetButtonImage fonksiyonu güncellenip PNG desteği eklendi.
- Prevent recurrence:
  - İkonlar için daima PNG formatı ve en az 64x64 çözünürlük kullan.
- Files/Commands touched: `GetButtonImage` fonksiyonu
- References: n/a
