# CopyPolish - Otomatik Metin Düzeltme Aracı

Bu araç, seçtiğiniz herhangi bir metni bir klavye kısayolu (`CTRL+SHIFT+K`) ile yakalar, OpenRouter AI servisine göndererek yeniden yazdırır ve sonucu otomatik olarak orijinal metnin üzerine yapıştırır. `CTRL+SHIFT+J` kısayolu ise seçili Türkçe metni İngilizceye çevirip yapıştırır. Her sonuç, sonuna bir boş satır eklenerek yapıştırılır.

## Özellikler
- Tepsi simgesi ve sağ tık menüsü: Başlat, Durdur, Ayarlar, Çıkış
- Ayarlar penceresi: API Key yönetimi (keyring), model seçimi, iki ayrı kısayol
- Kısayollar: Düzeltme `CTRL+SHIFT+K`, Çeviri `CTRL+SHIFT+J`

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

### 4. Ayarlar
- Tepsi simgesine sağ tık → Ayarlar
- API Key alanı maskesizdir; boş kaydederse keyring'den silinir
- Model ve kısayollar (`CTRL+SHIFT+K` ve `CTRL+SHIFT+J`) değiştirilebilir
