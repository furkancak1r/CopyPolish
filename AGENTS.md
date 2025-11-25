# Repository Guidelines

## Proje Yapısı ve Modül Organizasyonu
- Çekirdek kod `CopyPolish/` içinde; Outlook VSTO eklentisi için `ThisAddIn.cs`, şerit tanımları (`Ribbon*.xml`) ve formlar (`LoadingForm.cs`, `SearchForm.cs`, `SettingsForm.cs`) burada.
- Proje dosyası: `CopyPolish/CopyPolish.csproj` ve çözüm: `CopyPolish.sln`.
- Derlenmiş çıktı ve ClickOnce dağıtımı `dist/` altında; geçici build çıktıları `CopyPolish/bin` ve `CopyPolish/obj`.
- İç dokümantasyon `remember/memory/*.md` içinde.

## Derleme, Test ve Geliştirme Komutları
- `msbuild CopyPolish.sln /p:Configuration=Debug /p:Platform="Any CPU"` — yerel debug derlemesi.
- `msbuild CopyPolish.sln /p:Configuration=Release /p:Platform="Any CPU"` — üretim derlemesi.
- `msbuild CopyPolish/CopyPolish.csproj /t:Publish /p:PublishDir=dist/` — ClickOnce paketini `dist/` içine yayınlar.
- VS ile çalışıyorsanız Solution Explorer’dan yapılandırma seçip Build/Publish menülerini kullanabilirsiniz.

## Kodlama Stili ve İsimlendirme
- Dil: C# 4.7.2, 4 boşluklu girinti, `var` sadece açık tür bağlamı varken.
- Adlandırma: PascalCase sınıf/metot; camelCase yerel değişken/parametre; sabitler ALL_CAPS.
- UI öğeleri ve Ribbon kontrolleri için anlamlı adlar kullanın (örn. `btnImproveSelection`).
- Konfigürasyonlar `Properties.Settings` altında tutuluyor; yeni ayarlar eklerken varsayılan değerleri tanımlayın.

## Test Rehberi
- Otomatik test yok; değişiklik sonrası manuel QA yapın:
  - Outlook’ta yeni e-posta oluşturup seçili metni iyileştirme akışını çalıştırın.
  - Bağlam dahil/haricini ayarlardan değiştirip beklenen davranışı doğrulayın.
  - API anahtarı boşken kullanıcı mesajlarının göründüğünü kontrol edin.
- Geri dönüşümü azaltmak için soruna yönelik küçük repro senaryoları yazın.

## Commit ve PR Kuralları
- Commit mesajları Türkçe ve emir kipinde, kısa ve spesifik olsun (örn. `Seçili metin hata denetimini güçlendir`).
- Tek konu per commit; gereksiz dosyaları (bin/obj, `CopyPolish.csproj.user`, sertifika dosyaları) sahnelemeyin.
- PR’lar: değişiklik özeti, test notları, ilgili issue/iş tanımı linki; UI etkisi varsa ekran görüntüsü ekleyin.

## Güvenlik ve Yapılandırma İpuçları
- OpenRouter/API anahtarlarını yalnızca kullanıcı ayarlarında tutun; repo’ya eklemeyin.
- ClickOnce publish dizini (`dist/`) dağıtım için kullanılabilir; özel dağıtım yolu gerekiyorsa `PublishDir` parametresiyle override edin.
- Outlook/Word COM nesnelerini kullanırken null ve seçim denetimlerini koruyun; kullanıcıya hata mesajlarını yerelleştirilmiş metinlerle gösterin.
