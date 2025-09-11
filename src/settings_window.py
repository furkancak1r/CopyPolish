import tkinter as tk
from tkinter import ttk, messagebox
import logging
from src.api_handler import APIHandler
from src import config as cfg


class SettingsWindow(tk.Toplevel):
    def __init__(self, parent=None, on_close=None):
        super().__init__(parent)
        self.api_handler = APIHandler()
        self.logger = logging.getLogger(__name__)
        self.on_close = on_close
        self.setup_window()
        self.create_widgets()
        self.load_settings()

    def setup_window(self):
        self.title("CopyPolish - Ayarlar")
        self.geometry("520x360")
        self.resizable(True, True)

        # Pencereyi ekranın ortasına yerleştir (başlangıçta)
        self.update_idletasks()
        x = (self.winfo_screenwidth() // 2) - (520 // 2)
        y = (self.winfo_screenheight() // 2) - (360 // 2)
        self.geometry(f"520x360+{x}+{y}")

        # Window davranışı ayarları
        self.attributes('-topmost', True)
        self.grab_set()  # Modal davranış
        self.focus_set()

        # Pencere kapatma eventi
        self.protocol("WM_DELETE_WINDOW", self.cancel)

    def create_widgets(self):
        # Ana frame
        main_frame = ttk.Frame(self, padding="20")
        main_frame.pack(fill=tk.BOTH, expand=True)

        # Başlık
        title_label = ttk.Label(
            main_frame,
            text="CopyPolish Ayarları",
            font=("Arial", 16, "bold")
        )
        title_label.pack(pady=(0, 20))

        # API Anahtarı bölümü
        api_frame = ttk.LabelFrame(main_frame, text="OpenRouter API Ayarları", padding="15")
        api_frame.pack(fill=tk.X, pady=(0, 20))

        # API anahtarı açıklaması
        desc_label = ttk.Label(
            api_frame,
            text="OpenRouter API anahtarınızı girin. Ücretsiz hesap için: https://openrouter.ai",
            font=("Arial", 9),
            foreground="gray"
        )
        desc_label.pack(anchor=tk.W, pady=(0, 10))

        # API anahtarı girişi
        api_key_frame = ttk.Frame(api_frame)
        api_key_frame.pack(fill=tk.X, pady=(0, 10))

        ttk.Label(api_key_frame, text="API Anahtarı:").pack(anchor=tk.W)

        # API key entry ve show/hide
        entry_frame = ttk.Frame(api_key_frame)
        entry_frame.pack(fill=tk.X, pady=(5, 0))

        self.api_key_var = tk.StringVar()
        self.api_key_entry = ttk.Entry(
            entry_frame,
            textvariable=self.api_key_var,
            show="*",
            width=40
        )
        self.api_key_entry.pack(side=tk.LEFT, fill=tk.X, expand=True)
        try:
            self.api_key_entry.focus_set()
        except Exception:
            pass

        # Kısayol ve sağ tık menüsü bağla
        self.bind_entry_shortcuts(self.api_key_entry)

        self.show_key_var = tk.BooleanVar()
        self.show_key_check = ttk.Checkbutton(
            entry_frame,
            text="Göster",
            variable=self.show_key_var,
            command=self.toggle_api_key_visibility
        )
        self.show_key_check.pack(side=tk.RIGHT, padx=(10, 0))

        # Test butonu
        self.test_button = ttk.Button(
            api_frame,
            text="API Bağlantısını Test Et",
            command=self.test_api_connection
        )
        self.test_button.pack(anchor=tk.W, pady=(5, 0))

        # Model ayarları
        model_frame = ttk.LabelFrame(main_frame, text="Model Ayarları", padding="15")
        model_frame.pack(fill=tk.X, pady=(0, 20))

        # Mevcut modeller listesi (önerilen ücretsiz modeller)
        available_models = [
            "qwen/qwen3-coder:free",
            "mistralai/mistral-nemo:free",
            "qwen/qwen2.5:free",
            "meta-llama/llama-3.1-8b-instruct:free",
        ]

        # İyileştirme modeli
        ttk.Label(model_frame, text="İyileştirme Modeli:").pack(anchor=tk.W)
        self.model_improve_var = tk.StringVar()
        self.model_improve_combo = ttk.Combobox(
            model_frame, textvariable=self.model_improve_var,
            values=available_models, state="readonly"
        )
        self.model_improve_combo.pack(fill=tk.X, pady=(2, 8))

        # Çeviri modeli
        ttk.Label(model_frame, text="Çeviri Modeli (TR→EN):").pack(anchor=tk.W)
        self.model_translate_var = tk.StringVar()
        self.model_translate_combo = ttk.Combobox(
            model_frame, textvariable=self.model_translate_var,
            values=available_models, state="readonly"
        )
        self.model_translate_combo.pack(fill=tk.X, pady=(2, 8))

        # Otomatik fallback seçeneği
        self.auto_fallback_var = tk.BooleanVar()
        self.auto_fallback_check = ttk.Checkbutton(
            model_frame,
            text="Sağlayıcı hatasında otomatik alternatif model dene",
            variable=self.auto_fallback_var,
        )
        self.auto_fallback_check.pack(anchor=tk.W, pady=(4, 0))

        # Butonlar
        button_frame = ttk.Frame(main_frame)
        button_frame.pack(fill=tk.X, pady=(10, 0))

        ttk.Button(
            button_frame,
            text="İptal",
            command=self.cancel
        ).pack(side=tk.RIGHT, padx=(10, 0))

        ttk.Button(
            button_frame,
            text="Kaydet",
            command=self.save_settings
        ).pack(side=tk.RIGHT)

        # Kısayollar: Enter ve Ctrl+S kaydetsin
        try:
            self.bind('<Control-s>', lambda e: self.save_settings())
            self.bind('<Control-S>', lambda e: self.save_settings())
            self.api_key_entry.bind('<Return>', lambda e: self.save_settings())
        except Exception:
            pass

        # İçerik yerleşince boyutu ayarla
        try:
            self.after(0, self.adjust_size)
        except Exception:
            pass

    def adjust_size(self):
        try:
            self.update_idletasks()
            req_w = max(520, self.winfo_reqwidth())
            req_h = max(360, self.winfo_reqheight())
            x = (self.winfo_screenwidth() // 2) - (req_w // 2)
            y = (self.winfo_screenheight() // 2) - (req_h // 2)
            self.geometry(f"{req_w}x{req_h}+{x}+{y}")
            self.minsize(500, 340)
        except Exception:
            pass

    def bind_entry_shortcuts(self, entry: ttk.Entry):
        # Ctrl+V yapıştırma
        def paste_event(event=None):
            try:
                text = self.clipboard_get()
                if text:
                    entry.insert(tk.INSERT, text)
            except Exception:
                pass
            return "break"

        entry.bind('<Control-v>', paste_event)
        entry.bind('<Control-V>', paste_event)
        entry.bind('<Shift-Insert>', paste_event)

        # Sağ tık menüsü
        menu = tk.Menu(entry, tearoff=0)
        menu.add_command(label="Yapıştır", command=paste_event)

        def show_menu(event):
            try:
                menu.tk_popup(event.x_root, event.y_root)
            finally:
                menu.grab_release()

        entry.bind('<Button-3>', show_menu)

    def toggle_api_key_visibility(self):
        if self.show_key_var.get():
            self.api_key_entry.config(show="")
        else:
            self.api_key_entry.config(show="*")

    def load_settings(self):
        try:
            api_key = self.api_handler.get_api_key()
            if api_key:
                self.api_key_var.set(api_key)
            # Model/config yükle
            c = cfg.load_config()
            try:
                self.model_improve_var.set(c.get("model_improve", "qwen/qwen3-coder:free"))
                self.model_translate_var.set(c.get("model_translate", "qwen/qwen3-coder:free"))
                self.auto_fallback_var.set(bool(c.get("auto_fallback", True)))
            except Exception:
                pass
        except Exception as e:
            self.logger.error(f"Ayarlar yükleme hatası: {e}")

    def save_settings(self):
        try:
            api_key = self.api_key_var.get().strip()

            if not api_key:
                messagebox.showwarning("Uyarı", "Lütfen API anahtarını girin.")
                return

            # API anahtarını kaydet
            if self.api_handler.set_api_key(api_key):
                # Konfigürasyonu güncelle
                current_cfg = cfg.load_config()
                current_cfg["model_improve"] = self.model_improve_var.get() or current_cfg.get("model_improve")
                current_cfg["model_translate"] = self.model_translate_var.get() or current_cfg.get("model_translate")
                current_cfg["auto_fallback"] = bool(self.auto_fallback_var.get())
                if not cfg.save_config(current_cfg):
                    self.logger.warning("Konfigürasyon kaydedilemedi, varsayılanlar kullanılacak")
                messagebox.showinfo("Başarılı", "Ayarlar başarıyla kaydedildi!")
                try:
                    self.grab_release()
                except Exception:
                    pass
                self.destroy()
                try:
                    if callable(self.on_close):
                        self.on_close()
                except Exception:
                    pass
            else:
                messagebox.showerror("Hata", "API anahtarı kaydedilemedi!")

        except Exception as e:
            self.logger.error(f"Ayarlar kaydetme hatası: {e}")
            messagebox.showerror("Hata", f"Ayarlar kaydedilemedi: {str(e)}")

    def test_api_connection(self):
        try:
            # Geçici olarak API anahtarını ayarla
            temp_key = self.api_key_var.get().strip()
            if not temp_key:
                messagebox.showwarning("Uyarı", "Lütfen önce API anahtarını girin.")
                return

            # Test et
            self.test_button.config(text="Test ediliyor...", state="disabled")
            self.update()

            # Kalıcı kaydetmeden test et
            success, message = self.api_handler.test_api_connection_with_key(temp_key)

            if success:
                messagebox.showinfo("Başarılı", message)
            else:
                messagebox.showerror("Hata", message)

        except Exception as e:
            self.logger.error(f"API test hatası: {e}")
            messagebox.showerror("Hata", f"API testi başarısız: {str(e)}")
        finally:
            self.test_button.config(text="API Bağlantısını Test Et", state="normal")

    def cancel(self):
        try:
            self.grab_release()
        except Exception:
            pass
        self.destroy()
        try:
            if callable(self.on_close):
                self.on_close()
        except Exception:
            pass
