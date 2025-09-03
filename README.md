# CopyPolish - Otomatik Metin Düzeltme Aracı

Bu araç, seçtiğiniz herhangi bir metni bir klavye kısayolu (`CTRL+ALT+Y`) ile yakalar, OpenRouter AI servisine göndererek yeniden yazdırır ve sonucu otomatik olarak orijinal metnin üzerine yapıştırır. `CTRL+ALT+T` kısayolu ise seçili Türkçe metni İngilizceye çevirip yapıştırır. Her sonuç, sonuna bir boş satır eklenerek yapıştırılır.

## Özellikler
- Tepsi simgesi ve sağ tık menüsü: Başlat, Durdur, Ayarlar, Çıkış
- Ayarlar penceresi: API Key yönetimi (keyring), model seçimi, iki ayrı kısayol
- Kısayollar: Düzeltme `CTRL+ALT+Y`, Çeviri `CTRL+ALT+T`
 - Kısayollar: Düzeltme `CTRL+ALT+Y`, Çeviri `CTRL+ALT+T`, Son ekran görüntüsü yolu `CTRL+ALT+V` (ayarlar ile otomatik yapıştır kapatılabilir)

## Kurulum Adımları

### 1. Python Kurulumu
Eğer bilgisayarınızda Python yüklü değilse, https://www.python.org/downloads/ adresinden en son sürümü indirin. Kurulum sırasında "Add Python to PATH" seçeneğini işaretlediğinizden emin olun.

### 2. Bağımlılıkları Yükleme
Proje klasöründe bir komut istemi (CMD) veya PowerShell penceresi açın ve aşağıdaki komutu çalıştırarak gerekli Python kütüphanelerini yükleyin:

```bash
pip install -r requirements.txt
```

### 3. Çalıştırma
```bash
python main.py
```
Veya derlenmiş sürümü `dist/CopyPolish.exe` ile çalıştırın.

### 3.1. Exe Oluşturma (PyInstaller)
Windows için tek dosyalık `.exe` üretmek isterseniz:

```bash
pip install pyinstaller

# Yönetici yetkisi isteyen exe (önerilir)
pyinstaller CopyPolish.spec

# veya tek satırda yönetici yetkisiyle
pyinstaller --onefile --windowed --uac-admin --name CopyPolish main.py
```

Oluşan dosya `dist/CopyPolish.exe` yolundadır. Alternatif olarak `build.bat` çalıştırabilirsiniz.

Notlar
- Uygulama Windows'ta daima yönetici olarak çalışır. Python ile doğrudan çalıştırıldığında yetki yoksa UAC yükseltmesi ister; PyInstaller exe ise manifest ile yönetici ister.

## Başlangıçta Yöneticisiz UAC (Otomatik Başlat)
UAC istemi olmadan yönetici olarak başlatmak için Görev Zamanlayıcı kullanın (Startup klasörü yeterli değildir).

1) Exe'yi oluşturun ve repo kökünde `dist/CopyPolish.exe` bulunduğundan emin olun.
2) PowerShell'i yönetici olarak açın ve çalıştırın:

```powershell
./scripts/install-startup-elevated.ps1
# veya özel exe yolu ile
./scripts/install-startup-elevated.ps1 -ExePath "C:\\path\\to\\CopyPolish.exe"
```

Bu komut:
- Exe'yi `%LOCALAPPDATA%\CopyPolish\CopyPolish.exe` konumuna kopyalar.
- "CopyPolish Elevated" adlı bir görev oluşturur, "Girişte" tetiklenir ve "En yüksek ayrıcalıklarla" çalışır.
- Tek seferlik yönetici onayı gerekir; sonrasında her oturum açılışında UAC istemi olmadan tepsi uygulaması başlar.

Kaldırmak için (yönetici PowerShell):

```powershell
./scripts/uninstall-startup-elevated.ps1 -RemoveExe
```

## Kurulum Paketi (Program Ekle/Kaldır)
Uygulamayı klasik bir Windows programı olarak kurmak ve "Program Ekle/Kaldır" üzerinden kaldırmak için Inno Setup betiği sağladık.

Adımlar:
1) Exe üretin: `pyinstaller CopyPolish.spec`
2) Inno Setup (iscc.exe) yükleyin ve derleyin:
   - GUI ile: `installer/CopyPolish.iss` dosyasını açıp Build → Compile
   - Komut satırı: `iscc installer\CopyPolish.iss` veya `./scripts/build-installer.ps1`
3) Oluşan kurulum dosyasının adı `CopyPolish.exe` olacaktır (çıktı: `installer/Output/CopyPolish.exe`).

Kurulum:
- Dosyaları `C:\Program Files\CopyPolish` içine yerleştirir.
- Başlat menüsüne kısayol ekler.
- Girişte, yönetici ayrıcalıklarıyla çalışacak bir Zamanlanmış Görev kaydeder (UAC istemi olmadan).

Kaldırma:
- "Uygulamalar ve Özellikler / Program Ekle/Kaldır" üzerinden kaldırdığınızda zamanlanmış görevi de siler.

### 4. Ayarlar
- Tepsi simgesine sağ tık → Ayarlar
- API Key alanı varsayılan olarak maskelenir; "Göster" onay kutusuyla görünür yapılabilir. Boş kaydederse keyring'den silinir
- Model ve kısayollar (`CTRL+ALT+Y` ve `CTRL+ALT+T`) değiştirilebilir
 - Model ve kısayollar (`CTRL+ALT+Y`, `CTRL+ALT+T`, `CTRL+ALT+V`) değiştirilebilir; ekran görüntüsü yolu için “Otomatik yapıştır” aç/kapat seçeneği vardır.

Not: Eski sürümlerde kullanılan `CTRL+SHIFT+K/L/J` gibi Outlook ile çakışan kısayollar ile `CTRL+ALT+E` (birçok klavyede AltGr+E → €) otomatik olarak yeni güvenli varsayılanlara (`CTRL+ALT+Y` / `CTRL+ALT+T`) taşınır.
