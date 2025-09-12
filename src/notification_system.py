import logging
import os
import sys
import threading
import ctypes

# Prefer clickable toasts; fall back to basic toasts if needed
try:
    from win10toast_click import ToastNotifier as ClickToastNotifier
except Exception:
    ClickToastNotifier = None

try:
    from win10toast import ToastNotifier as BasicToastNotifier  # type: ignore
except Exception:
    BasicToastNotifier = None

# Optional Windows 10+ notifier as another fallback
try:
    from winotify import Notification as WinNotification  # type: ignore
    from winotify import audio as winotify_audio  # type: ignore
except Exception:
    WinNotification = None
    winotify_audio = None


def resource_path(relative_path: str) -> str:
    """Get absolute path to resource, works for dev and for PyInstaller"""
    try:
        base_path = sys._MEIPASS  # type: ignore[attr-defined]
    except Exception:
        base_path = os.path.abspath(".")
    return os.path.join(base_path, relative_path)


class NotificationSystem:
    def __init__(self):
        self.logger = logging.getLogger(__name__)
        self.app_name = "CopyPolish"

        # Pick a notifier that works in both dev and PyInstaller exe
        self.toaster = None

        if ClickToastNotifier is not None:
            try:
                self.toaster = ClickToastNotifier()
                self.logger.info("Bildirim backend: win10toast_click")
            except Exception as e:
                self.logger.warning(f"win10toast_click init failed, falling back: {e}")
                self.toaster = None
        if self.toaster is None and BasicToastNotifier is not None:
            try:
                self.toaster = BasicToastNotifier()
                self.logger.info("Bildirim backend: win10toast")
            except Exception as e:
                self.logger.warning(f"win10toast init failed: {e}")
                self.toaster = None

        try:
            self.icon_path = resource_path("icon.ico")
            if not os.path.exists(self.icon_path):
                self.icon_path = None
        except Exception:
            self.icon_path = None

        # Ensure process has a stable AppUserModelID so Windows uses our Start Menu icon
        try:
            ctypes.windll.shell32.SetCurrentProcessExplicitAppUserModelID(self.app_name)
        except Exception:
            pass

        # Create Start Menu shortcut after icon_path is determined (for AUMID)
        try:
            if getattr(sys, 'frozen', False):
                self._ensure_start_menu_shortcut()
        except Exception as e:
            self.logger.warning(f"Start Menu kısayolu oluşturulamadı (devam): {e}")

    def show_success(self, message: str, title: str = "Başarılı"):
        self._show_notification(title, message, "success")

    def show_error(self, message: str, title: str = "Hata"):
        self._show_notification(title, message, "error")

    def show_info(self, message: str, title: str = "Bilgi"):
        self._show_notification(title, message, "info")

    def show_warning(self, message: str, title: str = "Uyarı"):
        self._show_notification(title, message, "warning")

    def _show_notification(self, title: str, message: str, notification_type: str = "info"):
        def do_show():
            # Trim long messages to keep toasts tidy
            msg = (message[:97] + "...") if isinstance(message, str) and len(message) > 100 else message
            full_title = f"{self.app_name} - {title}" if title else self.app_name

            # Primary path: use toast library if available
            if self.toaster is not None:
                try:
                    # Use library-managed threading to avoid COM issues in frozen apps
                    self.toaster.show_toast(
                        title=full_title,
                        msg=msg,
                        icon_path=self.icon_path,
                        duration=5,
                        threaded=True,
                    )
                    # Treat any non-exception call as success (libs often return None)
                    self.logger.info(f"Bildirim gonderildi: {title} - {message}")
                    return
                except Exception as e:
                    self.logger.error(f"Bildirim gosterme hatasi: {e}")

            # Second fallback: try winotify if available
            if WinNotification is not None:
                try:
                    toast = WinNotification(
                        app_id=self.app_name,
                        title=full_title,
                        msg=str(msg),
                        icon=self.icon_path if self.icon_path else "",
                        duration="short",
                    )
                    try:
                        if winotify_audio:
                            toast.set_audio(winotify_audio.Default, loop=False)
                    except Exception:
                        pass
                    toast.show()
                    self.logger.info("Winotify bildirimi gosterildi (fallback).")
                    return
                except Exception as e:
                    self.logger.error(f"Winotify gosterme hatasi: {e}")

            # Final fallback: show a lightweight system message box (non-blocking thread)
            def _fallback_msgbox():
                try:
                    # MB_TOPMOST | MB_SETFOREGROUND
                    ctypes.windll.user32.MessageBoxW(0, str(msg), str(full_title), 0x00040000 | 0x00010000)
                except Exception as ie:
                    self.logger.error(f"Mesaj kutusu gosterilemedi: {ie}")

            threading.Thread(target=_fallback_msgbox, daemon=True).start()
            self.logger.info("Mesaj kutusu fallback tetiklendi.")

        # Run the notification display in a separate thread to prevent blocking the main GUI thread.
        threading.Thread(target=do_show, daemon=True).start()

    def _show_messagebox_direct(self, title: str, message: str):
        full_title = f"{self.app_name} - {title}" if title else self.app_name
        def _mb():
            try:
                ctypes.windll.user32.MessageBoxW(0, str(message), str(full_title), 0x00040000 | 0x00010000)
            except Exception as e:
                self.logger.error(f"Mesaj kutusu dogrudan gosterilemedi: {e}")
        threading.Thread(target=_mb, daemon=True).start()

    def show_api_error(self, error_message: str):
        if "API anahtarı" in (error_message or ""):
            self.show_error(
                "API anahtarınızı ayarlardan kontrol edin.",
                "API Anahtarı Hatası",
            )
        elif "rate limit" in (error_message or "").lower():
            self.show_warning(
                "API kullanım limitiniz aşıldı. Lütfen daha sonra tekrar deneyin.",
                "Kullanım Limiti",
            )
        elif "bağlantı" in (error_message or "").lower():
            self.show_error(
                "İnternet bağlantınızı kontrol edin.",
                "Bağlantı Hatası",
            )
        else:
            self.show_error(error_message, "API Hatası")

    def show_startup_notification(self):
        # Try native toast first; automatic fallbacks if it fails
        msg = "CopyPolish arka planda çalışıyor. Metin seçin ve araç çubuğunu kullanın!"
        title = "Hoş Geldiniz"
        self.show_info(msg, title)

    def _ensure_start_menu_shortcut(self):
        # Create a Start Menu shortcut with AppUserModelID (AUMID) so Windows allows toast notifications
        try:
            appdata = os.environ.get('APPDATA')
            if not appdata:
                return
            start_menu = os.path.join(appdata, 'Microsoft', 'Windows', 'Start Menu', 'Programs')
            os.makedirs(start_menu, exist_ok=True)
            lnk_path = os.path.join(start_menu, f'{self.app_name}.lnk')

            target = sys.executable
            app_id = self.app_name

            # Build or update the shortcut using ShellLink and set AppUserModel.ID
            import pythoncom
            from win32com.shell import shell, shellcon  # noqa: F4401
            from win32com.propsys import propsys, pscon

            link = pythoncom.CoCreateInstance(
                shell.CLSID_ShellLink, None, pythoncom.CLSCTX_INPROC_SERVER, shell.IID_IShellLink
            )
            link.SetPath(target)
            link.SetWorkingDirectory(os.path.dirname(target))
            # Use the exe's embedded icon for reliability (onefile safe)
            try:
                link.SetIconLocation(target, 0)
            except Exception:
                pass

            prop_store = link.QueryInterface(propsys.IID_IPropertyStore)
            propvar = propsys.PROPVARIANTType(app_id, pythoncom.VT_LPWSTR)
            prop_store.SetValue(pscon.PKEY_AppUserModel_ID, propvar)
            prop_store.Commit()

            persist_file = link.QueryInterface(pythoncom.IID_IPersistFile)
            persist_file.Save(lnk_path, 0)

            self.logger.info(f"Start Menu kısayolu oluşturuldu (AUMID set): {lnk_path}")
        except Exception as e:
            self.logger.warning(f"Start Menu kısayolu/AUMID ayarlanamadı (devam): {e}")
  