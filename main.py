import tkinter as tk
import threading
import pystray
from PIL import Image, ImageDraw
import sys
import os
import logging
import time
from src.text_detector import TextDetector
from src.settings_window import SettingsWindow
from src.api_handler import APIHandler
from src.notification_system import NotificationSystem

class CopyPolishApp:
    def __init__(self):
        self.setup_logging()
        self.text_detector = None
        self.settings_window = None
        self.api_handler = APIHandler()
        self.notification_system = NotificationSystem()
        self.tray_icon = None
        self.root = None
        self.setup_tkinter_root()
        
    def setup_logging(self):
        # Attempt to fix console encoding issues before setting up logging
        try:
            # sys.stderr is the default stream for logging
            if hasattr(sys.stderr, 'reconfigure'):
                sys.stderr.reconfigure(encoding='utf-8')
            if hasattr(sys.stdout, 'reconfigure'):
                sys.stdout.reconfigure(encoding='utf-8')
        except (TypeError, AttributeError):
            # This might fail in some non-console environments, which is acceptable.
            pass

        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler('copypolish.log', encoding='utf-8'),
                logging.StreamHandler() # This will now use the reconfigured stream
            ],
            force=True  # Overwrites existing handlers to prevent duplicates
        )
        self.logger = logging.getLogger(__name__)
        
    def setup_tkinter_root(self):
        """Gizli tkinter root penceresi oluştur"""
        self.root = tk.Tk()
        self.root.withdraw()  # Pencereyi gizle
        self.root.title("CopyPolish")
        
    def create_icon_image(self):
        width = 64
        height = 64
        color1 = (255, 255, 255)
        color2 = (0, 120, 215)
        
        image = Image.new('RGB', (width, height), color1)
        dc = ImageDraw.Draw(image)
        
        dc.rectangle(
            (width // 4, height // 4, width * 3 // 4, height * 3 // 4),
            fill=color2
        )
        
        return image
    
    def create_tray_menu(self):
        return pystray.Menu(
            pystray.MenuItem("Ayarlar", self.open_settings),
            pystray.MenuItem("Çıkış", self.quit_app)
        )
    
    def open_settings(self, icon=None, item=None):
        # Tüm Tk işlemlerini ana Tk thread'inde çalıştır
        def _open():
            try:
                # Metin algılamayı ayarlar açıkken duraklat
                try:
                    if self.text_detector:
                        self.text_detector.pause()
                except Exception:
                    pass

                # Önceki pencere varsa kapat
                if self.settings_window is not None:
                    try:
                        if self.settings_window.winfo_exists():
                            self.settings_window.destroy()
                    except:
                        pass
                
                # Yeni ayarlar penceresi aç
                def on_settings_close():
                    try:
                        if self.text_detector:
                            self.text_detector.resume()
                    except Exception:
                        pass

                self.settings_window = SettingsWindow(self.root, on_close=on_settings_close)
                self.logger.info("Ayarlar penceresi açıldı")
            except Exception as e:
                self.logger.error(f"Ayarlar penceresi açma hatası: {e}")
        
        try:
            self.root.after(0, _open)
        except Exception as e:
            self.logger.error(f"Ayar penceresi zamanlama hatası: {e}")
    
    def quit_app(self, icon=None, item=None):
        self.logger.info("Uygulama kapatılıyor...")
        if self.text_detector:
            self.text_detector.stop()

        # Tray'i durdur
        try:
            if self.tray_icon:
                self.tray_icon.stop()
        except Exception:
            pass

        # Tk kapatma işlemlerini ana Tk thread'inde yap
        def _close_tk():
            try:
                if self.settings_window and self.settings_window.winfo_exists():
                    self.settings_window.destroy()
            except Exception:
                pass
            try:
                self.root.quit()
                self.root.destroy()
            except Exception:
                pass

        try:
            if self.root:
                self.root.after(0, _close_tk)
        except Exception:
            pass
    
    def start_text_detection(self):
        try:
            from src.text_detector import TextDetector
            self.text_detector = TextDetector(
                api_handler=self.api_handler,
                notification_system=self.notification_system,
                tk_root=self.root
            )
            self.text_detector.start()
            self.logger.info("Metin algılama başlatıldı")
        except Exception as e:
            self.logger.error(f"Metin algılama başlatılamadı: {e}")
    
    def run(self):
        try:
            self.logger.info("CopyPolish başlatılıyor...")
            
            icon_image = self.create_icon_image()
            menu = self.create_tray_menu()
            
            self.tray_icon = pystray.Icon(
                "CopyPolish",
                icon_image,
                menu=menu
            )
            
            # Metin algılamayı ayrı thread'de başlat
            detection_thread = threading.Thread(target=self.start_text_detection)
            detection_thread.daemon = True
            detection_thread.start()
            
            # System tray'i arka planda çalıştır
            self.logger.info("Sistem tepsisinde çalışıyor...")
            tray_thread = threading.Thread(target=self.tray_icon.run, daemon=True)
            tray_thread.start()
            
            # Tk ana döngüsünü ana thread'de çalıştır
            self.logger.info("Tk arayüz döngüsü başlatılıyor...")
            self.root.mainloop()

        except Exception as e:
            self.logger.error(f"Uygulama başlatma hatası: {e}")
            raise

def _main():
    backoff = 1
    while True:
        app = CopyPolishApp()
        try:
            app.run()
            break
        except KeyboardInterrupt:
            try:
                app.quit_app()
            except Exception:
                pass
            break
        except Exception:
            try:
                app.quit_app()
            except Exception:
                pass
            time.sleep(backoff)
            backoff = backoff * 2 if backoff < 60 else 60

if __name__ == "__main__":
    _main()
  