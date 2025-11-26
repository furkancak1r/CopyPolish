# Repository Guidelines

## Proje Yapısı ve Modül Organizasyonu

### Teknoloji Stack
- C# .NET Outlook VSTO Add-in
- Office Interop APIs
- Windows Forms

### Proje Yapısı

- Çekirdek kod `CopyPolish/` içinde; Outlook VSTO eklentisi için `ThisAddIn.cs`, şerit tanımları (`Ribbon*.xml`) ve formlar (`LoadingForm.cs`, `SearchForm.cs`, `SettingsForm.cs`) burada.
- `SearchForm.cs`: Gelişmiş e-posta arama formu - Outlook Table API, BackgroundWorker ile async arama, çoklu filtre desteği.
- `ModelConfiguration.cs`: AI model konfigürasyonları.
- `OpenRouterClient.cs`: OpenRouter API istemcisi.
- Proje dosyası: `CopyPolish/CopyPolish.csproj` ve çözüm: `CopyPolish.sln`.
- Derlenmiş çıktı ve ClickOnce dağıtımı kök `dist/` altında tutulur (tek dist kullanın); geçici build çıktıları `CopyPolish/bin` ve `CopyPolish/obj`.
- `CopyPolish/dist` altı kullanılmıyor; varsa temizleyin. Publish her zaman geçici dizine (ör. `C:/temp/CopyPolish_Build/`) alınıp kök `dist/` altına kopyalanır.
- İç dokümantasyon `remember/memory/*.md` içinde.
- Bilinen sorunlar ve çözümler: `KNOWN_ISSUES.md`.

## Derleme, Test ve Geliştirme Komutları

- `msbuild CopyPolish.sln /p:Configuration=Debug /p:Platform="Any CPU"` — yerel debug derlemesi.
- `msbuild CopyPolish.sln /p:Configuration=Release /p:Platform="Any CPU"` — üretim derlemesi.
- `msbuild CopyPolish/CopyPolish.csproj /t:Publish /p:PublishDir=dist/` — ClickOnce paketini `dist/` içine yayınlar.
- Publish öncesi `dist/` ve `bin/Release/app.publish` klasörlerini temizleyin.
- Tercih edilen akış: `PublishDir="C:/temp/CopyPolish_Build/"` ile geçici dizine alın, ardından çıktıyı kök `dist/` altına kopyalayın; `CopyPolish/dist` oluşturmayın/taşımayın.
- VS ile çalışıyorsanız Solution Explorer'dan yapılandırma seçip Build/Publish menülerini kullanabilirsiniz.

## Tek EXE Kurulum Dosyası Oluşturma

VSTO eklentileri için tek bir dağıtılabilir EXE oluşturmak için aşağıdaki adımları izleyin:

### Adım 1: Temiz Publish

```bash
# Eski dosyaları temizle
rm -rf dist/

# Farklı bir dizine publish yap (kilitli dosya sorunlarını önler)
msbuild CopyPolish/CopyPolish.csproj -t:Publish -p:Configuration=Release -p:PublishDir="C:/temp/CopyPolish_Build/" -p:BootstrapperEnabled=true
```

### Adım 2: WinRAR SFX ile Tek EXE Oluşturma

1. SFX konfigürasyon dosyası oluşturun (`sfx_config.txt`):
```
Path=.\
Setup=setup.exe
TempMode
Silent=2
Overwrite=1
Title=CopyPolish Kurulum
```

2. WinRAR ile self-extracting EXE oluşturun:
```bash
cd C:/temp/CopyPolish_Build/
"C:/Program Files/WinRAR/WinRAR.exe" a -sfx -z"sfx_config.txt" -ep1 "../CopyPolish.exe" "setup.exe" "CopyPolish.vsto" "Application Files"
```

### Hızlı Tek Komut

```bash
# Temiz build, publish ve tek EXE oluşturma
rm -rf dist/ C:/temp/CopyPolish_Build/
mkdir -p C:/temp/CopyPolish_Build/
msbuild CopyPolish/CopyPolish.csproj -t:Publish -p:Configuration=Release -p:PublishDir="C:/temp/CopyPolish_Build/" -p:BootstrapperEnabled=true
cd C:/temp/CopyPolish_Build/
echo -e "Path=.\nSetup=setup.exe\nTempMode\nSilent=2\nOverwrite=1\nTitle=CopyPolish Kurulum" > sfx_config.txt
"C:/Program Files/WinRAR/WinRAR.exe" a -sfx -z"sfx_config.txt" -ep1 "CopyPolish.exe" "setup.exe" "CopyPolish.vsto" "Application Files"
```

### Önemli Notlar

- **IExpress kullanmayın**: Sadece tek dosya paketler, VSTO için `Application Files` klasörü gereklidir.
- **Gerekli dosyalar**: `setup.exe` (bootstrapper), `CopyPolish.vsto`, `Application Files/` klasörü birlikte paketlenmelidir.
- **Kilitli dosya sorunu**: Publish dizini kilitli kalırsa farklı bir dizine (`C:/temp/`) publish yapın.
- **Hedef PC gereksinimleri**: .NET Framework 4.7.2, VSTO Runtime, Microsoft Outlook.
- **Son EXE boyutu**: ~789 KB (tüm dosyalar dahil).

### 26.11.2025 dist Güncelleme Özeti

- Temizlik: `dist/`, `CopyPolish/bin/Release/app.publish` ve `C:/temp/CopyPolish_Build/` silindi.
- Build/Publish: `"%ProgramFiles%/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" CopyPolish/CopyPolish.csproj -t:Publish -p:Configuration=Release -p:PublishDir="C:/temp/CopyPolish_Build/" -p:BootstrapperEnabled=true`
- Çıktı taşıma: `C:/temp/CopyPolish_Build/{Application Files, CopyPolish.vsto, setup.exe}` -> kök `dist/`.
- SFX: `dist/sfx_config.txt` güncellendi; `"%ProgramFiles%/WinRAR/WinRAR.exe" a -sfx -z"sfx_config.txt" -ep1 dist/CopyPolish.exe setup.exe CopyPolish.vsto "Application Files"` ile yeni tek EXE üretildi.

## Kodlama Stili ve İsimlendirme

- Dil: C# 4.7.2, 4 boşluklu girinti, `var` sadece açık tür bağlamı varken.
- Adlandırma: PascalCase sınıf/metot; camelCase yerel değişken/parametre; sabitler ALL_CAPS.
- UI öğeleri ve Ribbon kontrolleri için anlamlı adlar kullanın (örn. `btnImproveSelection`).
- Konfigürasyonlar `Properties.Settings` altında tutuluyor; yeni ayarlar eklerken varsayılan değerleri tanımlayın.
- **Ek Kurallar**:
  - Türkçe yorumlar ve değişken isimleri kullanın.
  - Exception handling her yerde olmalı.
  - Embedded resources kullanımı tercih edilmeli.

## Icon Formatı

- PNG dosyaları tercih ediliyor (ICO yerine).
- 64x64 minimum çözünürlük.
- Şeffaf arka plan.

## Türkçe Karakter ve Lokalizasyon

- Türkçe metin işlemlerinde `CultureInfo("tr-TR")` kullanın; `ToLowerInvariant()` Türkçe i/İ, ı/I karakterlerini yanlış dönüştürür.
- SQL/DASL sorgularında özel karakterleri escape edin: `'` → `''`, `%` → `[%]`, `_` → `[_]`.
- Büyük/küçük harf duyarsız karşılaştırma için `StringComparison.OrdinalIgnoreCase` kullanın.
- UI metinleri Türkçe olmalı; emoji yerine metin veya ASCII karakterler tercih edin (uyumluluk için).

## COM Nesneleri ve Bellek Yönetimi

- Outlook COM nesnelerini (`MailItem`, `Table`, `Row`, `Attachment`) kullandıktan sonra `Marshal.ReleaseComObject()` ile serbest bırakın.
- Döngülerde COM nesnelerini try-finally ile sarmalayın.
- `BackgroundWorker` kullanırken `FormClosing` event'inde `CancelAsync()` çağırın.
- Uzun süren aramalarda batch işleme kullanın (örn. her 50 sonuçta UI güncellemesi).

## Test Rehberi

- Otomatik test yok; değişiklik sonrası manuel QA yapın:
  - Outlook'ta yeni e-posta oluşturup seçili metni iyileştirme akışını çalıştırın.
  - Gelişmiş arama: Türkçe karakterli arama (ör. "hayırlı olsun"), tırnak içi tam ifade araması, filtre kombinasyonları.
  - Bağlam dahil/haricini ayarlardan değiştirip beklenen davranışı doğrulayın.
  - API anahtarı boşken kullanıcı mesajlarının göründüğünü kontrol edin.
  - TreeView klasör seçimi: parent seçilince children'ların da seçildiğini doğrulayın.
- Geri dönüşümü azaltmak için soruna yönelik küçük repro senaryoları yazın.

## Commit ve PR Kuralları

- Commit mesajları Türkçe ve emir kipinde, kısa ve spesifik olsun (örn. `Seçili metin hata denetimini güçlendir`).
- Tek konu per commit; gereksiz dosyaları (bin/obj, `CopyPolish.csproj.user`, sertifika dosyaları) sahnelemeyin.
- PR'lar: değişiklik özeti, test notları, ilgili issue/iş tanımı linki; UI etkisi varsa ekran görüntüsü ekleyin.
- Önemli bug düzeltmelerini `KNOWN_ISSUES.md`'ye kaydedin.

## Güvenlik ve Yapılandırma İpuçları

- OpenRouter/API anahtarlarını yalnızca kullanıcı ayarlarında tutun; repo'ya eklemeyin.
- ClickOnce publish dizini (`dist/`) dağıtım için kullanılabilir; özel dağıtım yolu gerekiyorsa `PublishDir` parametresiyle override edin.
- ClickOnce kurulumunu her zaman kök `dist/CopyPolish.vsto` üzerinden başlatın.
- Outlook/Word COM nesnelerini kullanırken null ve seçim denetimlerini koruyun; kullanıcıya hata mesajlarını yerelleştirilmiş metinlerle gösterin.
- SQL sorgularında kullanıcı girdilerini escape ederek injection riskini önleyin.
