import logging
from plyer import notification
import threading
import os
import sys

def resource_path(relative_path):
    """ Get absolute path to resource, works for dev and for PyInstaller """
    try:
        base_path = sys._MEIPASS
    except Exception:
        base_path = os.path.abspath(".")

    return os.path.join(base_path, relative_path)

class NotificationSystem:
    def __init__(self):
        self.logger = logging.getLogger(__name__)
        self.app_name = "CopyPolish"
        try:
            self.icon_path = resource_path("icon.ico")
            # Check if the file actually exists to prevent plyer errors
            if not os.path.exists(self.icon_path):
                self.icon_path = "" # Plyer handles empty string as no icon
        except Exception:
            self.icon_path = ""
        
    def show_success(self, message: str, title: str = "Başarılı"):
        self._show_notification(title, message, "success")
        
    def show_error(self, message: str, title: str = "Hata"):
        self._show_notification(title, message, "error")
        
    def show_info(self, message: str, title: str = "Bilgi"):
        self._show_notification(title, message, "info")
        
    def show_warning(self, message: str, title: str = "Uyarı"):
        self._show_notification(title, message, "warning")
        
    def _show_notification(self, title: str, message: str, notification_type: str = "info"):
        def show_async():
            try:
                if len(message) > 100:
                    message_short = message[:97] + "..."
                else:
                    message_short = message
                    
                notification.notify(
                    title=f"{self.app_name} - {title}",
                    message=message_short,
                    app_name=self.app_name,
                    app_icon=self.icon_path,
                    timeout=5,
                    toast=True
                )
                
                self.logger.info(f"Bildirim gösterildi: {title} - {message}")
                
            except Exception as e:
                self.logger.error(f"Bildirim gösterme hatası: {e}")
                print(f"[{self.app_name}] {title}: {message}")
                
        threading.Thread(target=show_async, daemon=True).start()
        
    def show_api_error(self, error_message: str):
        if "API anahtarı" in error_message:
            self.show_error(
                "API anahtarınızı ayarlardan kontrol edin.",
                "API Anahtarı Hatası"
            )
        elif "rate limit" in error_message.lower():
            self.show_warning(
                "API kullanım limitiniz aşıldı. Lütfen daha sonra tekrar deneyin.",
                "Kullanım Limiti"
            )
        elif "bağlantı" in error_message.lower():
            self.show_error(
                "İnternet bağlantınızı kontrol edin.",
                "Bağlantı Hatası"
            )
        else:
            self.show_error(error_message, "API Hatası")
            
    def show_startup_notification(self):
        self.show_info(
            "CopyPolish arka planda çalışıyor. Metin seçin ve araç çubuğunu kullanın!",
            "Hoş Geldiniz"
        )
  