# CopyPolish - Akıllı Metin Asistanı

CopyPolish, Windows masaüstünde çalışan, seçili metinleri anında iyileştiren ve çeviren akıllı bir araçtır. Kullanıcı bir metin seçtiğinde, fare imlecinin yanında beliren araç çubuğu ile tek tıkla metni mükemmelleştirebilir.

## ✨ Özellikler

- **🎯 Bağlamsal Araç Çubuğu**: Metin seçtiğinizde otomatik beliren mini araç çubuğu
- **🔧 Metin İyileştirme**: Dilbilgisi, akıcılık ve ton açısından metni iyileştirme
- **🌐 Anında Çeviri**: Türkçe'den İngilizce'ye çeviri (TR→EN)
- **⚙️ Kolay Ayarlar**: Sistem tepsisinden erişilebilen ayarlar penceresi
- **🔐 Güvenli Depolama**: API anahtarları Windows Credential Manager'da güvenle saklanır
- **📢 Akıllı Bildirimler**: İşlem durumu hakkında anlık bildirimler

## 🚀 Kurulum

### Gereksinimler
- Windows 10/11
- Python 3.7 veya üzeri
- OpenRouter API anahtarı (ücretsiz hesap: https://openrouter.ai)

### Adımlar

1. **Projeyi klonlayın:**
   ```bash
   git clone [repository-url]
   cd CopyPolish2
   ```

2. **Gerekli kütüphaneleri yükleyin:**
   ```bash
   pip install -r requirements.txt
   ```

3. **Uygulamayı test edin:**
   ```bash
   python test_app.py
   ```

4. **Uygulamayı başlatın:**
   ```bash
   python main.py
   ```

   Veya Windows'ta:
   ```bash
   start.bat
   ```

## 🔧 Kullanım

1. **Kurulum sonrası:**
   - Uygulama sistem tepsisinde çalışmaya başlar
   - Tepsideki simgeye sağ tıklayarak "Ayarlar"ı açın
   - OpenRouter API anahtarınızı girin ve kaydedin

2. **Metin işleme:**
   - Herhangi bir uygulamada metni fare ile seçin
   - Beliren araç çubuğundan istediğiniz işlemi seçin:
     - **🔧 Düzelt**: Metni iyileştir
     - **🌐 TR→EN**: Türkçe'den İngilizce'ye çevir
   - İşlenmiş metin otomatik olarak yerine yapıştırılır

## 📁 Proje Yapısı

```
CopyPolish2/
├── main.py                 # Ana uygulama dosyası
├── src/
│   ├── api_handler.py      # OpenRouter API entegrasyonu
│   ├── contextual_toolbar.py  # Araç çubuğu arayüzü
│   ├── text_detector.py    # Metin seçim algılama
│   ├── settings_window.py  # Ayarlar penceresi
│   └── notification_system.py  # Bildirim sistemi
├── test_app.py            # Test script'i
├── requirements.txt       # Python bağımlılıkları
├── start.bat             # Windows başlatma script'i
└── README.md             # Bu dosya
```

## 🔑 API Ayarları

1. [OpenRouter.ai](https://openrouter.ai) adresinden ücretsiz hesap oluşturun
2. API anahtarınızı alın
3. CopyPolish ayarlarından API anahtarını girin
4. "API Bağlantısını Test Et" butonu ile test edin

**Varsayılan Model:** `qwen/qwen3-coder:free` (Ücretsiz)

## 🛠️ Sorun Giderme

### Yaygın Sorunlar:

**"API anahtarı bulunamadı" hatası:**
- Sistem tepsisinden ayarları açın
- API anahtarınızı girin ve kaydedin

**Araç çubuğu belirmiyor:**
- Metni fare ile sürükleyerek seçin (çift tıklama değil)
- Windows Defender'ın uygulamayı engellediğini kontrol edin

**Bildirimler görünmüyor:**
- Windows bildirim ayarlarını kontrol edin
- Uygulama loglarını `copypolish.log` dosyasından inceleyin

## 📝 Loglar

Uygulama, tüm işlemleri `copypolish.log` dosyasında kaydeder. Sorun yaşadığınızda bu dosyayı kontrol edebilirsiniz.

## 🔒 Güvenlik

- API anahtarları Windows Credential Manager'da şifrelenerek saklanır
- Hiçbir veri harici sunucularda depolanmaz
- Tüm metin işlemleri OpenRouter API'si üzerinden gerçekleşir

## 📋 Sistem Gereksinimleri

- **İşletim Sistemi:** Windows 10/11
- **RAM:** Minimum 50 MB
- **Python:** 3.7+
- **İnternet:** API çağrıları için gerekli

## 🤝 Katkıda Bulunma

Bu proje açık kaynak kodludur. Katkılarınızı bekliyoruz!

## 📞 Destek

Sorun yaşıyorsanız:
1. `test_app.py` dosyasını çalıştırın
2. `copypolish.log` dosyasını kontrol edin
3. GitHub Issues bölümünden bildirin

---

**CopyPolish** - Yazılı iletişiminizi hızlandıran görünmez asistan 🚀
