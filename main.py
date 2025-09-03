import os
import sys
import ctypes
import json
import time
import requests
import pyperclip
import keyboard
import threading
import queue
import keyring
import shutil
import logging
import tkinter as tk
from tkinter import ttk, messagebox
from PIL import Image, ImageDraw
import pystray
from typing import Union, TYPE_CHECKING, Optional

# Windows toast notifications are optional in the packaged exe.
# If the Windows Runtime bindings are missing at runtime, fall back gracefully
# so the tray app still starts.
try:
    from windows_toasts import Toast, WindowsToaster
    _toaster: Optional[WindowsToaster] = WindowsToaster('CopyPolish')
except Exception:
    Toast = None  # type: ignore
    # Avoid binding class name to a value in runtime; keep type-only import for checkers
    if TYPE_CHECKING:
        from windows_toasts import WindowsToaster as _WindowsToaster
    _toaster = None

def hide_console_window():
    if os.name == 'nt':
        try:
            hwnd = ctypes.windll.kernel32.GetConsoleWindow()
            if hwnd:
                ctypes.windll.user32.ShowWindow(hwnd, 0)  # SW_HIDE
        except Exception:
            pass

APP_NAME = 'CopyPolish'
OLD_APP_NAME = 'AutoCopyAI'
task_queue = queue.Queue()
ui_queue = queue.Queue()

SITE_URL = os.getenv("OPENROUTER_SITE_URL", "https://desktop.app/copypolish")
SITE_NAME = os.getenv("OPENROUTER_SITE_NAME", "CopyPolish Desktop Tool")

CONFIG_DIR = os.path.join(os.getenv('APPDATA', os.path.expanduser('~')), APP_NAME)
CONFIG_PATH = os.path.join(CONFIG_DIR, 'config.json')
LOG_PATH = os.path.join(CONFIG_DIR, 'app.log')
config_lock = threading.Lock()
config = {}
is_listening = False
hotkey_handlers = []
tray_icon = None
# New defaults chosen to avoid common Outlook shortcuts
# Polish/Re-write: Ctrl+Alt+Y (Y = Yaz/yeniden yaz)
# Translate TR→EN: Ctrl+Alt+T (T = Translation)
default_hotkey = 'ctrl+alt+y'
default_translate_hotkey = 'ctrl+alt+t'
# Paste last screenshot path: Ctrl+Alt+V
default_screenshot_path_hotkey = 'ctrl+alt+v'

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
    try:
        if _toaster is not None and Toast is not None:
            new_toast = Toast()
            new_toast.text_fields = [title, body]
            _toaster.show_toast(new_toast)
        else:
            # Fallback: best-effort message box only for critical messages.
            # Keep silent for normal info to avoid bothering users.
            if os.name == 'nt' and body and ('Hata' in title or 'Başarısız' in title):
                try:
                    ctypes.windll.user32.MessageBoxW(None, f"{title}: {body}", APP_NAME, 0)
                except Exception:
                    pass
    except Exception:
        # Never crash on notification errors
        pass


def setup_logging(debug: bool = False):
    try:
        os.makedirs(CONFIG_DIR, exist_ok=True)
    except Exception:
        pass
    try:
        logging.basicConfig(
            filename=LOG_PATH,
            filemode='a',
            level=logging.DEBUG if debug else logging.INFO,
            format='%(asctime)s [%(levelname)s] %(message)s',
        )
        logging.info('CopyPolish logging started (debug=%s)', debug)
    except Exception:
        pass


def _excepthook(exctype, value, tb):
    try:
        logging.exception('Uncaught exception', exc_info=(exctype, value, tb))
    except Exception:
        pass
    try:
        if os.name == 'nt':
            ctypes.windll.user32.MessageBoxW(None, f'Hata: {value}', APP_NAME, 0x10)
    except Exception:
        pass


def has_flag(name: str) -> bool:
    try:
        return any(arg.lower() == name.lower() for arg in sys.argv)
    except Exception:
        return False

def get_api_key() -> Union[str, None]:
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
    if 'hotkey_screenshot_path' not in data:
        data['hotkey_screenshot_path'] = default_screenshot_path_hotkey

    # Targeted migration: if existing shortcuts are known to conflict with Outlook,
    # switch to the new safer defaults. Only migrate if they match the old pairs.
    try:
        hk_lower = str(data.get('hotkey', '')).lower()
        hk_tr_lower = str(data.get('hotkey_translate', '')).lower()
        conflicting_mains = {'ctrl+shift+k', 'ctrl+shift+l'}
        # include prior default 'ctrl+alt+e' to avoid AltGr→€ conflict on many layouts
        conflicting_trs = {'ctrl+shift+l', 'ctrl+shift+j', 'ctrl+alt+e'}
        if hk_lower in conflicting_mains:
            data['hotkey'] = default_hotkey
        if hk_tr_lower in conflicting_trs:
            data['hotkey_translate'] = default_translate_hotkey
        # ensure screenshot path hotkey has a sensible default if missing/empty
        if not str(data.get('hotkey_screenshot_path', '')).strip():
            data['hotkey_screenshot_path'] = default_screenshot_path_hotkey
    except Exception:
        pass
    with config_lock:
        config.update(data)

def save_config():
    os.makedirs(CONFIG_DIR, exist_ok=True)
    with config_lock:
        with open(CONFIG_PATH, 'w', encoding='utf-8') as f:
            json.dump(config, f, ensure_ascii=False, indent=2)

def rewrite_text(selected_data: str) -> Union[str, None]:
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

def translate_text_tr_en(selected_data: str) -> Union[str, None]:
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

def get_latest_screenshot_path() -> Union[str, None]:
    # Common screenshot directories (EN & TR)
    home = os.path.expanduser('~')
    onedrive = os.getenv('ONEDRIVE') or os.path.join(home, 'OneDrive')
    candidates = [
        os.path.join(home, 'Pictures', 'Screenshots'),
        os.path.join(onedrive, 'Pictures', 'Screenshots'),
        os.path.join(home, 'Resimler', 'Ekran görüntüleri'),
        os.path.join(home, 'Resimler', 'Ekran Görüntüleri'),
    ]
    exts = {'.png', '.jpg', '.jpeg', '.bmp'}
    newest_file = None
    newest_mtime = -1.0
    for base in candidates:
        if not os.path.isdir(base):
            continue
        try:
            for name in os.listdir(base):
                p = os.path.join(base, name)
                if not os.path.isfile(p):
                    continue
                _, ext = os.path.splitext(name)
                if ext.lower() not in exts:
                    continue
                try:
                    mt = os.path.getmtime(p)
                    if mt > newest_mtime:
                        newest_mtime = mt
                        newest_file = p
                except Exception:
                    continue
        except Exception:
            continue
    return newest_file

def on_hotkey_paste_last_screenshot_path():
    p = get_latest_screenshot_path()
    if not p:
        show_notification('Ekran görüntüsü bulunamadı')
        return

    def _paste():
        try:
            # Put path to clipboard
            pyperclip.copy(p)
            time.sleep(0.12)
            # Ensure modifier keys are not held by the user
            try:
                keyboard.release('alt')
                keyboard.release('left alt')
                keyboard.release('right alt')
                keyboard.release('ctrl')
                keyboard.release('shift')
                keyboard.release('left shift')
                keyboard.release('right shift')
                keyboard.release('windows')
            except Exception:
                pass
            if bool(config.get('screenshot_path_auto_paste', True)):
                # Paste into the focused field
                keyboard.send('ctrl+v')
                show_notification('Yol Yapıştırıldı', p)
            else:
                show_notification('Yol panoya kopyalandı', p)
        except Exception:
            show_notification('Yapıştırılamadı')

    threading.Thread(target=_paste, daemon=True).start()

 

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
    hk_ss = config.get('hotkey_screenshot_path', default_screenshot_path_hotkey)
    hotkey_handlers = [
        keyboard.add_hotkey(hk, on_hotkey_activate, suppress=True),
        keyboard.add_hotkey(hk_tr, on_hotkey_translate, suppress=True),
    ]
    if hk_ss:
        hotkey_handlers.append(
            keyboard.add_hotkey(hk_ss, on_hotkey_paste_last_screenshot_path, suppress=True)
        )
    is_listening = True
    try:
        hk_ss_disp = hk_ss.upper() if hk_ss else 'YOK'
    except Exception:
        hk_ss_disp = 'YOK'
    show_notification('Dinleyici Açık', f'{hk.upper()} / {hk_tr.upper()} / {hk_ss_disp} etkin')

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
    # Wider window; allow horizontal resize for flexible width
    root.geometry('720x240')
    root.minsize(640, 240)
    root.resizable(True, False)
    frm = ttk.Frame(root, padding=12)
    frm.pack(fill='both', expand=True)

    # API Key (masked by default) + Show toggle
    ttk.Label(frm, text='API Key').grid(row=0, column=0, sticky='w')
    api_var = tk.StringVar(value=api_key_existing)
    api_entry = ttk.Entry(frm, textvariable=api_var, show='*')
    api_entry.grid(row=0, column=1, sticky='we')
    show_var = tk.BooleanVar(value=False)

    def toggle_api_visibility():
        try:
            api_entry.config(show='' if show_var.get() else '*')
        except Exception:
            pass

    ttk.Checkbutton(frm, text='Göster', variable=show_var, command=toggle_api_visibility).grid(
        row=0, column=2, sticky='w', padx=(6, 0)
    )

    # Model selection
    ttk.Label(frm, text='Model').grid(row=1, column=0, sticky='w')
    model_var = tk.StringVar(value=config.get('model', 'qwen/qwen3-coder:free'))
    if mlist:
        model_input = ttk.Combobox(frm, textvariable=model_var, values=mlist)
    else:
        model_input = ttk.Entry(frm, textvariable=model_var)
    model_input.grid(row=1, column=1, columnspan=2, sticky='we')

    # Hotkeys
    ttk.Label(frm, text='Kısayol (Düzeltme)').grid(row=2, column=0, sticky='w')
    hotkey_var = tk.StringVar(value=config.get('hotkey', default_hotkey))
    hotkey_entry = ttk.Entry(frm, textvariable=hotkey_var)
    hotkey_entry.grid(row=2, column=1, columnspan=2, sticky='we')

    ttk.Label(frm, text='Kısayol (Çeviri TR→EN)').grid(row=3, column=0, sticky='w')
    hotkey_tr_var = tk.StringVar(value=config.get('hotkey_translate', default_translate_hotkey))
    hotkey_tr_entry = ttk.Entry(frm, textvariable=hotkey_tr_var)
    hotkey_tr_entry.grid(row=3, column=1, columnspan=2, sticky='we')

    ttk.Label(frm, text='Kısayol (Son ekran görüntüsü yolu)').grid(row=4, column=0, sticky='w')
    hotkey_ss_var = tk.StringVar(value=config.get('hotkey_screenshot_path', default_screenshot_path_hotkey))
    hotkey_ss_entry = ttk.Entry(frm, textvariable=hotkey_ss_var)
    hotkey_ss_entry.grid(row=4, column=1, columnspan=2, sticky='we')

    auto_paste_var = tk.BooleanVar(value=bool(config.get('screenshot_path_auto_paste', True)))
    ttk.Checkbutton(frm, text='Otomatik yapıştır', variable=auto_paste_var).grid(row=5, column=1, sticky='w')

    # Buttons
    btns = ttk.Frame(frm)
    btns.grid(row=6, column=0, columnspan=3, pady=10)

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
            # Allow disabling screenshot-path hotkey by leaving it blank
            hk_ss = hotkey_ss_var.get().strip()
            config['hotkey'] = hk
            config['hotkey_translate'] = hk_tr
            config['hotkey_screenshot_path'] = hk_ss
            config['screenshot_path_auto_paste'] = bool(auto_paste_var.get())
        save_config()
        if is_listening:
            stop_listener()
            start_listener()
        root.destroy()

    ttk.Button(btns, text='Kaydet', command=save_and_close).pack(side='left', padx=6)
    ttk.Button(btns, text='Kapat', command=root.destroy).pack(side='left', padx=6)

    # Column weights for flexible width: labels fixed, inputs expand
    try:
        frm.columnconfigure(0, weight=0)
        frm.columnconfigure(1, weight=1)
        frm.columnconfigure(2, weight=0)
    except Exception:
        pass
    root.mainloop()

def create_tray_image() -> Image.Image:
    try:
        # icon.ico dosyasını yükle
        img = Image.open('icon.ico')
        # Tray için uygun boyuta getir (genellikle 64x64)
        img = img.resize((64, 64), Image.Resampling.LANCZOS)
        # RGBA moduna çevir (şeffaflık için)
        if img.mode != 'RGBA':
            img = img.convert('RGBA')
        return img
    except Exception as e:
        # Eğer icon.ico yüklenemezse, eski basit tasarımı kullan
        print(f"Warning: Could not load icon.ico for tray: {e}")
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
        try:
            action, payload = ui_queue.get(timeout=1)
        except queue.Empty:
            # Periodic tick so Ctrl+C can be handled gracefully
            continue
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
    debug = has_flag('--debug')
    setup_logging(debug)
    sys.excepthook = _excepthook
    logging.info('Starting CopyPolish (pid=%s)', os.getpid())
    # Always require administrator privileges on Windows (can be disabled with --no-admin)
    if os.name == 'nt' and not has_flag('--no-admin'):
        try:
            is_admin = ctypes.windll.shell32.IsUserAnAdmin() != 0
        except Exception:
            is_admin = False
        if not is_admin:
            try:
                if getattr(sys, 'frozen', False):
                    prog = sys.executable
                    params = " ".join(f'"{arg}"' for arg in sys.argv[1:])
                else:
                    # Prefer pythonw.exe to avoid any console window
                    prog = sys.executable
                    try:
                        if prog.lower().endswith('python.exe'):
                            candidate = os.path.join(os.path.dirname(prog), 'pythonw.exe')
                            if os.path.exists(candidate):
                                prog = candidate
                    except Exception:
                        pass
                    script = os.path.abspath(sys.argv[0])
                    params = " ".join([f'"{script}"'] + [f'"{arg}"' for arg in sys.argv[1:]])
                ctypes.windll.shell32.ShellExecuteW(None, 'runas', prog, params, None, 1)
                return
            except Exception:
                try:
                    ctypes.windll.user32.MessageBoxW(None, 'Yönetici olarak başlatma başarısız.', APP_NAME, 0x10)
                except Exception:
                    pass
                return
    # Hide console window unless debugging
    if not debug:
        hide_console_window()
    else:
        logging.info('Debug mode active: console will remain visible')
    load_config()
    logging.info('Config loaded from %s', CONFIG_PATH)
    worker_thread = threading.Thread(target=processing_worker, daemon=True)
    worker_thread.start()
    logging.info('Worker thread started')
    start_listener()
    global tray_icon
    try:
        tray_icon = pystray.Icon(APP_NAME, create_tray_image(), APP_NAME, build_menu())
        tray_icon.run_detached()
        logging.info('Tray icon started')
    except Exception as e:
        logging.exception('Tray start failed: %s', e)
        show_notification('Hata', 'Tepsi başlatılamadı. Ayrıntılar app.log dosyasında.')
        return
    try:
        ui_dispatch_loop()
    except KeyboardInterrupt:
        # Graceful console exit (Ctrl+C)
        pass
    finally:
        stop_listener()
        try:
            if tray_icon:
                tray_icon.stop()
        except Exception:
            pass

if __name__ == "__main__":
    main()
  
