import tkinter as tk
from tkinter import ttk
import threading
import time
import logging
import pyperclip
import win32api
import win32con

class ContextualToolbar:
    def __init__(self, api_handler, notification_system, tk_root):
        self.api_handler = api_handler
        self.notification_system = notification_system
        self.tk_root = tk_root
        self.window = None
        self.auto_hide_timer = None
        self.auto_hide_after_id = None
        self.logger = logging.getLogger(__name__)

    def show_toolbar(self, x, y, selected_text):
        def _show():
            if self.window:
                self._hide_toolbar_impl()
            self.create_toolbar_window(x, y, selected_text)
            self.start_auto_hide_timer()
        self.tk_root.after(0, _show)

    def create_toolbar_window(self, x, y, selected_text):
        self.window = tk.Toplevel(self.tk_root)
        self.window.title("")
        self.window.overrideredirect(True)
        self.window.attributes('-topmost', True)
        self.window.attributes('-alpha', 0.95)
        self.window.attributes('-toolwindow', True)

        style = ttk.Style()
        style.theme_use('clam')

        main_frame = ttk.Frame(self.window, padding="5")
        main_frame.pack(fill=tk.BOTH, expand=True)

        improve_btn = ttk.Button(
            main_frame,
            text="İyileştir",
            command=lambda: self.handle_improve_text(selected_text),
            width=10
        )
        improve_btn.pack(side=tk.LEFT, padx=2)

        translate_btn = ttk.Button(
            main_frame,
            text="TR→EN",
            command=lambda: self.handle_translate_text(selected_text),
            width=10
        )
        translate_btn.pack(side=tk.LEFT, padx=2)

        close_btn = ttk.Button(
            main_frame,
            text="×",
            command=self.hide_toolbar,
            width=3
        )
        close_btn.pack(side=tk.RIGHT, padx=2)

        self.position_window(x, y)
        self.window.bind('<FocusOut>', self.on_focus_out)
        self.window.bind('<Button-1>', self.on_click)

    def position_window(self, x, y):
        self.window.update_idletasks()
        width = self.window.winfo_reqwidth()
        height = self.window.winfo_reqheight()
        screen_width = self.window.winfo_screenwidth()
        screen_height = self.window.winfo_screenheight()

        if x + width > screen_width:
            x = screen_width - width - 10
        if y + height > screen_height:
            y = y - height - 20
            
        self.window.geometry(f"+{x}+{y}")

    def handle_improve_text(self, text):
        self.logger.info("Metin iyileştirme başlatıldı")
        self.hide_toolbar()
        self.notification_system.show_info("İşleniyor...")

        def improve_async():
            try:
                improved_text = self.api_handler.improve_text(text)
                if improved_text:
                    self.paste_text(improved_text)
                    self.notification_system.show_success("Metin başarıyla iyileştirildi!")
                else:
                    self.notification_system.show_error("Metin iyileştirilemedi!")
            except Exception as e:
                self.logger.error(f"Metin iyileştirme hatası: {e}")
                self.notification_system.show_api_error(str(e))
        
        threading.Thread(target=improve_async, daemon=True).start()

    def handle_translate_text(self, text):
        self.logger.info("Metin çevirisi başlatıldı")
        self.hide_toolbar()
        self.notification_system.show_info("İşleniyor...")

        def translate_async():
            try:
                translated_text = self.api_handler.translate_text(text)
                if translated_text:
                    self.paste_text(translated_text)
                    self.notification_system.show_success("Metin başarıyla çevrildi!")
                else:
                    self.notification_system.show_error("Metin çevrilemedi!")
            except Exception as e:
                self.logger.error(f"Metin çeviri hatası: {e}")
                self.notification_system.show_api_error(str(e))
        
        threading.Thread(target=translate_async, daemon=True).start()

    def _press_key(self, vk, down=True):
        try:
            if down:
                win32api.keybd_event(vk, 0, 0, 0)
            else:
                win32api.keybd_event(vk, 0, win32con.KEYEVENTF_KEYUP, 0)
        except Exception:
            pass

    def paste_text(self, text: str):
        """
        Pastes the given text by simulating CTRL+V. This is non-destructive
        and will only overwrite the currently selected text.
        """
        original_clip = None
        try:
            # Preserve user's clipboard
            original_clip = pyperclip.paste()
        except Exception:
            pass
        
        try:
            pyperclip.copy(text)
            self._press_key(win32con.VK_CONTROL, True)
            self._press_key(ord('V'), True)
            time.sleep(0.05)
            self._press_key(ord('V'), False)
            self._press_key(win32con.VK_CONTROL, False)
        except Exception as e:
            self.logger.error(f"Yapıştırma hatası: {e}")
        finally:
            # Restore the original clipboard content after a short delay
            if original_clip is not None:
                threading.Timer(0.2, lambda: pyperclip.copy(original_clip)).start()

    def start_auto_hide_timer(self):
        try:
            if self.auto_hide_after_id and self.window:
                try:
                    self.window.after_cancel(self.auto_hide_after_id)
                except Exception:
                    pass
                self.auto_hide_after_id = None

            if self.window:
                self.auto_hide_after_id = self.window.after(4000, self._hide_toolbar_impl)
        except Exception:
            pass

    def on_focus_out(self, event):
        self.window.after(100, self.hide_toolbar)

    def on_click(self, event):
        self.start_auto_hide_timer()

    def hide_toolbar(self):
        self.tk_root.after(0, self._hide_toolbar_impl)

    def _hide_toolbar_impl(self):
        if getattr(self, 'auto_hide_after_id', None) and self.window:
            try:
                self.window.after_cancel(self.auto_hide_after_id)
            except Exception:
                pass
            self.auto_hide_after_id = None
        if self.auto_hide_timer:
            try:
                self.auto_hide_timer.cancel()
            except Exception:
                pass
        if self.window:
            try:
                self.window.destroy()
            except Exception:
                pass
            self.window = None
  