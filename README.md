File: README.md
```markdown
# CopyPolish - Akıllı Metin Asistanı

CopyPolish, **Microsoft Outlook** içinde çalışan, seçtiğiniz metinleri anında iyileştiren ve çeviren akıllı bir Windows aracıdır. Bir metin seçtiğinizde, fare imlecinin yanında beliren araç çubuğu ile tek tıkla metninizi mükemmelleştirebilirsiniz.

## ✨ Özellikler

- **🎯 Sadece Outlook'ta Çalışır**: Yalnızca Microsoft Outlook aktifken devreye girerek diğer uygulamalarda gereksiz yere çalışmaz.
- **🚀 Anında Tepki**: Metni seçtiğiniz anda beliren hızlı ve hafif araç çubuğu.
- **🔧 Akıllı Metin İyileştirme**: Dilbilgisi, akıcılık ve ton açısından metni iyileştirme.
- **🌐 Anında Çeviri**: Türkçe'den İngilizce'ye anında çeviri (TR→EN).
- **⚙️ Kolay Ayarlar**: Sistem tepsisinden erişilebilen basit ayarlar penceresi.
- **🔐 Güvenli Depolama**: API anahtarları Windows Credential Manager'da güvenle saklanır.

## 🚀 Kurulum ve Kullanım

1.  **Uygulamayı İndirin:**
    *   Projenin "Releases" sayfasından en son `CopyPolish.exe` dosyasını indirin.

2.  **Çalıştırın:**
    *   İndirdiğiniz `CopyPolish.exe` dosyasına çift tıklayarak çalıştırın. Uygulama sistem tepsisinde (saat'in yanındaki ikonlar) çalışmaya başlayacaktır.

3.  **API Anahtarını Ayarlayın:**
    *   Sistem tepsisindeki CopyPolish simgesine sağ tıklayın ve "Ayarlar"ı seçin.
    *   [OpenRouter.ai](https://openrouter.ai) adresinden aldığınız ücretsiz API anahtarınızı girin ve "Kaydet"e tıklayın.

4.  **Kullanmaya Başlayın:**
    *   **Microsoft Outlook** içinde bir e-posta yazarken, düzenlemek istediğiniz metni fare ile seçin.
    *   Metnin yanında beliren araç çubuğundan "İyileştir" veya "TR→EN" seçeneğine tıklayın.
    *   İşlenmiş metin, orijinal metnin üzerine otomatik olarak yapıştırılacaktır.

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
├── icon.ico                # Uygulama simgesi
├── requirements.txt       # Python bağımlılıkları
├── start.bat             # Windows başlatma script'i
└── README.md             # Bu dosya
```

## 🛠️ Geliştiriciler İçin

Projeyi kaynak kodundan çalıştırmak veya geliştirmek isterseniz:

1.  **Projeyi klonlayın:**
    ```bash
    # git clone ...
    cd CopyPolish2
    ```

2.  **Gerekli kütüphaneleri yükleyin:**
    ```bash
    pip install -r requirements.txt
    ```

3.  **Uygulamayı başlatın:**
    ```bash
    python main.py
    ```

4.  **`.exe` dosyası oluşturun:**
    *   `pyinstaller` kullanarak tek dosya bir uygulama oluşturmak için aşağıdaki komutu çalıştırın. Çıktı, `dist` klasöründe olacaktır.
    ```bash
    pyinstaller --name "CopyPolish" --onefile --windowed --icon="icon.ico" --add-data="icon.ico;." main.py
    ```

## 📝 Loglar

Uygulama, tüm işlemleri `copypolish.log` dosyasında kaydeder. Sorun yaşadığınızda bu dosyayı kontrol edebilirsiniz.

---

**CopyPolish** - Yazılı iletişiminizi hızlandıran görünmez asistan 🚀
```