import logging
from plyer import notification
import threading
import time

class NotificationSystem:
    def __init__(self):
        self.logger = logging.getLogger(__name__)
        self.app_name = "CopyPolish"
        self.icon_path = None  # Varsayılan sistem simgesi kullanılacak
        
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
                # Mesajı kısalt
                if len(message) > 100:
                    message_short = message[:97] + "..."
                else:
                    message_short = message
                    
                notification.notify(
                    title=f"{self.app_name} - {title}",
                    message=message_short,
                    app_name=self.app_name,
                    timeout=5,  # 5 saniye görünür
                    toast=True  # Windows toast notification
                )
                
                self.logger.info(f"Bildirim gösterildi: {title} - {message}")
                
            except Exception as e:
                self.logger.error(f"Bildirim gösterme hatası: {e}")
                # Fallback: Konsola yazdır
                print(f"[{self.app_name}] {title}: {message}")
                
        # Bildirimi ayrı thread'de göster
        threading.Thread(target=show_async, daemon=True).start()
        
    def show_processing(self, message: str = "İşlem devam ediyor..."):
        """İşlem durumu bildirimi"""
        self.show_info(message, "İşlem Durumu")
        
    def show_api_error(self, error_message: str):
        """API hatası özel bildirimi"""
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
            
    def show_text_processed(self, action: str, success: bool = True):
        """Metin işleme sonucu bildirimi"""
        if success:
            if action == "improve":
                self.show_success("Metniniz başarıyla iyileştirildi!")
            elif action == "translate":
                self.show_success("Metniniz başarıyla çevrildi!")
            else:
                self.show_success("İşlem başarıyla tamamlandı!")
        else:
            if action == "improve":
                self.show_error("Metin iyileştirilemedi.")
            elif action == "translate":
                self.show_error("Metin çevrilemedi.")
            else:
                self.show_error("İşlem başarısız oldu.")
                
    def show_startup_notification(self):
        """Uygulama başlangıç bildirimi"""
        self.show_info(
            "CopyPolish arka planda çalışıyor. Metin seçin ve araç çubuğunu kullanın!",
            "Hoş Geldiniz"
        )
