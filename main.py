import os
import json
import time
import requests
import pyperclip
import keyboard
import threading
import queue
import keyring
import shutil
import tkinter as tk
from tkinter import ttk, messagebox
from PIL import Image, ImageDraw
import pystray
from windows_toasts import Toast, WindowsToaster

APP_NAME = 'CopyPolish'
OLD_APP_NAME = 'AutoCopyAI'
toaster = WindowsToaster(APP_NAME)
task_queue = queue.Queue()
ui_queue = queue.Queue()

SITE_URL = os.getenv("OPENROUTER_SITE_URL", "https://desktop.app/copypolish")
SITE_NAME = os.getenv("OPENROUTER_SITE_NAME", "CopyPolish Desktop Tool")

CONFIG_DIR = os.path.join(os.getenv('APPDATA', os.path.expanduser('~')), APP_NAME)
CONFIG_PATH = os.path.join(CONFIG_DIR, 'config.json')
config_lock = threading.Lock()
config = {}
is_listening = False
hotkey_handlers = []
tray_icon = None
default_hotkey = 'ctrl+shift+k'
default_translate_hotkey = 'ctrl+shift+j'

SYSTEM_PROMPT = """Sen, bir e-postanın ana mesajını ve samimiyet tonunu koruyarak onu daha akıcı ve etkili hale getiren bir iletişim asistanısın. Aşağıdaki kurallara harfiyen uymalısın:

1.  TONU KORU (En Önemli Kural): Orijinal metin ne kadar samimi veya resmi ise, senin metnin de o seviyede olmalıdır. Samimi bir dili ("Selam abi") asla aşırı resmi bir dile ("Sayın Yetkili") çevirme.
2.  ANLAMI DEĞİŞTİRME: Cümlenin temel anlamını, amacını veya içerdiği komutu asla değiştirme. Sadece dilbilgisi, akıcılık ve yazım hatalarını düzelt. Örneğin, 'Dosyayı ilet' komutunu 'Dosyayı iletiyorum' ifadesine çevirme.
3.  SELAMLAMAYI KORU: Orijinal metindeki selamlama ne ise (örn: "Merhaba,"), yanıtın da birebir aynı selamlamayla başlamalıdır.
4.  GEREKSİZ BİLGİ EKLEME: Orijinal metinde olmayan bilgileri ("...bilginize sunarım" gibi) ekleme.
5.  PLACEHOLDER KULLANMA: Yanıtına "[ADINIZ]" gibi yer tutucular ekleme.
6.  TEKNİK TOKEN GÖSTERME: Yanıtın asla '<|...|>' gibi teknik token'lar içermemeli.
7.  SADECE YENİDEN YAZILMIŞ METNİ DÖNDÜR: Yanıtın, sadece ve sadece yeniden yazılmış metni içermelidir, başka hiçbir şey değil.
8.  Mailleri her zaman daha kibar bir şekilde yaz. Emreder gibi yazma asla olmamalı."""

TRANSLATE_PROMPT = """You are a precise translator from Turkish to English. Follow these rules:

1. Preserve meaning and tone. Do not embellish.
2. Output only the English translation text, nothing else.
3. Keep formatting and line breaks when possible.
"""

def show_notification(title, body=""):
    new_toast = Toast()
    new_toast.text_fields = [title, body]
    toaster.show_toast(new_toast)

def get_api_key() -> str | None:
    v = keyring.get_password(APP_NAME, 'OPENROUTER_API_KEY')
    if v:
        return v
    old = None
    try:
        old = keyring.get_password(OLD_APP_NAME, 'OPENROUTER_API_KEY')
    except Exception:
        old = None
    if old:
        try:
            keyring.set_password(APP_NAME, 'OPENROUTER_API_KEY', old)
        except Exception:
            pass
        return old
    return None

def load_config():
    os.makedirs(CONFIG_DIR, exist_ok=True)
    try:
        old_dir = os.path.join(os.getenv('APPDATA', os.path.expanduser('~')), OLD_APP_NAME)
        old_cfg = os.path.join(old_dir, 'config.json')
        if os.path.exists(old_cfg) and not os.path.exists(CONFIG_PATH):
            try:
                shutil.copy2(old_cfg, CONFIG_PATH)
            except Exception:
                pass
    except Exception:
        pass
    if os.path.exists(CONFIG_PATH):
        try:
            with open(CONFIG_PATH, 'r', encoding='utf-8') as f:
                data = json.load(f)
        except Exception:
            data = {}
    else:
        data = {}
    if 'model' not in data:
        data['model'] = 'qwen/qwen3-coder:free'
    if 'hotkey' not in data:
        data['hotkey'] = default_hotkey
    if 'hotkey_translate' not in data:
        data['hotkey_translate'] = default_translate_hotkey
    with config_lock:
        config.update(data)

def save_config():
    os.makedirs(CONFIG_DIR, exist_ok=True)
    with config_lock:
        with open(CONFIG_PATH, 'w', encoding='utf-8') as f:
            json.dump(config, f, ensure_ascii=False, indent=2)

def rewrite_text(selected_data: str) -> str | None:
    api_key = get_api_key()
    if not api_key:
        return None
    user_prompt = f"Aşağıdaki metni, sistem talimatlarına uyarak yeniden yaz.\n\nYENİDEN YAZILACAK KISIM:\n{selected_data}"
    try:
        response = requests.post(
            "https://openrouter.ai/api/v1/chat/completions",
            headers={
                "Authorization": f"Bearer {api_key}",
                "HTTP-Referer": SITE_URL, "X-Title": SITE_NAME, "Content-Type": "application/json",
            },
            json={
                "model": config.get('model', 'qwen/qwen3-coder:free'),
                "messages": [{"role": "system", "content": SYSTEM_PROMPT}, {"role": "user", "content": user_prompt}],
            },
            timeout=30
        )
        response.raise_for_status()
        data = response.json()
        text = data.get("choices", [{}])[0].get("message", {}).get("content")
        return text if text and isinstance(text, str) else None
    except (requests.exceptions.RequestException, IndexError, KeyError):
        return None

def translate_text_tr_en(selected_data: str) -> str | None:
    api_key = get_api_key()
    if not api_key:
        return None
    user_prompt = f"Translate the following Turkish text into fluent, natural English. Keep tone and meaning.\n\nTEXT:\n{selected_data}"
    try:
        response = requests.post(
            "https://openrouter.ai/api/v1/chat/completions",
            headers={
                "Authorization": f"Bearer {api_key}",
                "HTTP-Referer": SITE_URL, "X-Title": SITE_NAME, "Content-Type": "application/json",
            },
            json={
                "model": config.get('model', 'qwen/qwen3-coder:free'),
                "messages": [{"role": "system", "content": TRANSLATE_PROMPT}, {"role": "user", "content": user_prompt}],
            },
            timeout=30
        )
        response.raise_for_status()
        data = response.json()
        text = data.get("choices", [{}])[0].get("message", {}).get("content")
        return text if text and isinstance(text, str) else None
    except (requests.exceptions.RequestException, IndexError, KeyError):
        return None

def processing_worker():
    while True:
        original_clipboard_content, selected_text = task_queue.get()
        show_notification('İşlem Başlatılıyor...')
        
        if isinstance(selected_text, str) and selected_text.startswith('__TRANSLATE__::'):
            payload = selected_text.split('::', 1)[1]
            corrected_text = translate_text_tr_en(payload)
        else:
            corrected_text = rewrite_text(selected_text)

        if corrected_text:
            # AI'dan gelen yanıtın sonundaki tüm boşlukları/satır sonlarını temizle.
            final_text = corrected_text.rstrip()
            
            # Her koşulda, sonuna bir Windows "Enter" karakteri ekle.
            final_text += '\r\n\r\n'

            pyperclip.copy(final_text)
            time.sleep(0.1)
            keyboard.send('ctrl+v')
            show_notification('İşlem Başarılı!', 'Metin düzeltildi ve yapıştırıldı.')
        else:
            show_notification('İşlem Başarısız Oldu', 'Metin düzeltilemedi. API hatası olabilir.')
            pyperclip.copy(original_clipboard_content)
        task_queue.task_done()

def on_hotkey_activate():
    original_clipboard_content = pyperclip.paste()
    pyperclip.copy('')
    keyboard.send('ctrl+c')
    time.sleep(0.2)
    selected_text = pyperclip.paste()
    if selected_text:
        task_queue.put((original_clipboard_content, selected_text))
    else:
        pyperclip.copy(original_clipboard_content)

def on_hotkey_translate():
    original_clipboard_content = pyperclip.paste()
    pyperclip.copy('')
    keyboard.send('ctrl+c')
    time.sleep(0.2)
    selected_text = pyperclip.paste()
    if selected_text:
        task_queue.put((original_clipboard_content, f"__TRANSLATE__::{selected_text}"))
    else:
        pyperclip.copy(original_clipboard_content)

def start_listener():
    global is_listening, hotkey_handlers
    if is_listening:
        return
    hk = config.get('hotkey', default_hotkey)
    hk_tr = config.get('hotkey_translate', default_translate_hotkey)
    hotkey_handlers = [
        keyboard.add_hotkey(hk, on_hotkey_activate, suppress=True),
        keyboard.add_hotkey(hk_tr, on_hotkey_translate, suppress=True),
    ]
    is_listening = True
    show_notification('Dinleyici Açık', f'{hk.upper()} / {hk_tr.upper()} etkin')

def stop_listener():
    global is_listening, hotkey_handlers
    if not is_listening:
        return
    try:
        for h in hotkey_handlers:
            try:
                keyboard.remove_hotkey(h)
            except Exception:
                pass
    except Exception:
        pass
    hotkey_handlers = []
    is_listening = False
    show_notification('Dinleyici Kapalı')

def fetch_models(api_key: str) -> list[str]:
    try:
        r = requests.get(
            "https://openrouter.ai/api/v1/models",
            headers={"Authorization": f"Bearer {api_key}", "HTTP-Referer": SITE_URL, "X-Title": SITE_NAME},
            timeout=20,
        )
        r.raise_for_status()
        data = r.json()
        arr = data.get('data', [])
        ids = [x.get('id') for x in arr if isinstance(x, dict) and x.get('id')]
        return ids if ids else []
    except Exception:
        return []

def open_settings():
    api_key_existing = get_api_key() or ''
    mlist = fetch_models(api_key_existing) if api_key_existing else []
    root = tk.Tk()
    root.title('Ayarlar')
    root.geometry('420x200')
    root.resizable(False, False)
    frm = ttk.Frame(root, padding=12)
    frm.pack(fill='both', expand=True)
    ttk.Label(frm, text='API Key').grid(row=0, column=0, sticky='w')
    api_var = tk.StringVar(value=api_key_existing)
    api_entry = ttk.Entry(frm, textvariable=api_var, width=46)
    api_entry.grid(row=0, column=1, sticky='we')
    ttk.Label(frm, text='Model').grid(row=1, column=0, sticky='w')
    model_var = tk.StringVar(value=config.get('model', 'qwen/qwen3-coder:free'))
    if mlist:
        model_input = ttk.Combobox(frm, textvariable=model_var, values=mlist, width=44)
    else:
        model_input = ttk.Entry(frm, textvariable=model_var, width=46)
    model_input.grid(row=1, column=1, sticky='we')
    ttk.Label(frm, text='Kısayol (Düzeltme)').grid(row=2, column=0, sticky='w')
    hotkey_var = tk.StringVar(value=config.get('hotkey', default_hotkey))
    hotkey_entry = ttk.Entry(frm, textvariable=hotkey_var, width=46)
    hotkey_entry.grid(row=2, column=1, sticky='we')
    ttk.Label(frm, text='Kısayol (Çeviri TR→EN)').grid(row=3, column=0, sticky='w')
    hotkey_tr_var = tk.StringVar(value=config.get('hotkey_translate', default_translate_hotkey))
    hotkey_tr_entry = ttk.Entry(frm, textvariable=hotkey_tr_var, width=46)
    hotkey_tr_entry.grid(row=3, column=1, sticky='we')
    btns = ttk.Frame(frm)
    btns.grid(row=4, column=0, columnspan=2, pady=10)
    def save_and_close():
        k = api_var.get().strip()
        try:
            if k:
                keyring.set_password(APP_NAME, 'OPENROUTER_API_KEY', k)
            else:
                try:
                    keyring.delete_password(APP_NAME, 'OPENROUTER_API_KEY')
                except keyring.errors.PasswordDeleteError:
                    pass
        except Exception:
            messagebox.showerror('Hata', 'API key kaydedilemedi/silinemedi')
            return
        with config_lock:
            config['model'] = model_var.get().strip() or config.get('model', 'qwen/qwen3-coder:free')
            hk = hotkey_var.get().strip() or default_hotkey
            hk_tr = hotkey_tr_var.get().strip() or default_translate_hotkey
            config['hotkey'] = hk
            config['hotkey_translate'] = hk_tr
        save_config()
        if is_listening:
            stop_listener()
            start_listener()
        root.destroy()
    ttk.Button(btns, text='Kaydet', command=save_and_close).pack(side='left', padx=6)
    ttk.Button(btns, text='Kapat', command=root.destroy).pack(side='left', padx=6)
    for i in range(2):
        frm.columnconfigure(i, weight=1)
    root.mainloop()

def create_tray_image() -> Image.Image:
    img = Image.new('RGBA', (64, 64), (0, 122, 204, 255))
    d = ImageDraw.Draw(img)
    d.rectangle([(12, 12), (52, 52)], outline=(255, 255, 255, 255), width=3)
    return img

def menu_start(icon, item):
    start_listener()
    try:
        icon.menu = build_menu()
        icon.update_menu()
    except Exception:
        pass

def menu_stop(icon, item):
    stop_listener()
    try:
        icon.menu = build_menu()
        icon.update_menu()
    except Exception:
        pass

def menu_settings(icon, item):
    try:
        ui_queue.put(('open_settings', None))
    except Exception:
        pass

def menu_exit(icon, item):
    try:
        icon.stop()
    except Exception:
        pass
    try:
        ui_queue.put(('exit', None))
    except Exception:
        pass

def build_menu():
    return pystray.Menu(
        pystray.MenuItem('Başlat', menu_start, default=False, enabled=not is_listening),
        pystray.MenuItem('Durdur', menu_stop, default=False, enabled=is_listening),
        pystray.MenuItem('Ayarlar', menu_settings),
        pystray.MenuItem('Çıkış', menu_exit)
    )

def ui_dispatch_loop():
    while True:
        action, payload = ui_queue.get()
        if action == 'open_settings':
            try:
                open_settings()
            except Exception:
                pass
        elif action == 'exit':
            ui_queue.task_done()
            break
        ui_queue.task_done()

def on_hotkey_activate():
    original_clipboard_content = pyperclip.paste()
    pyperclip.copy('')
    keyboard.send('ctrl+c')
    time.sleep(0.2)
    selected_text = pyperclip.paste()
    if selected_text:
        task_queue.put((original_clipboard_content, selected_text))
    else:
        pyperclip.copy(original_clipboard_content)

def main():
    load_config()
    worker_thread = threading.Thread(target=processing_worker, daemon=True)
    worker_thread.start()
    start_listener()
    global tray_icon
    tray_icon = pystray.Icon(APP_NAME, create_tray_image(), APP_NAME, build_menu())
    tray_icon.run_detached()
    try:
        ui_dispatch_loop()
    finally:
        stop_listener()

if __name__ == "__main__":
    main()
  