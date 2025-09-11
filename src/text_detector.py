import time
import threading
import logging
import win32api
import win32con
import win32gui
import win32process
import os
import pyperclip
from src.contextual_toolbar import ContextualToolbar

class TextDetector:
    def __init__(self, api_handler, notification_system, tk_root):
        self.api_handler = api_handler
        self.notification_system = notification_system
        self.tk_root = tk_root
        self.toolbar = ContextualToolbar(api_handler, notification_system, tk_root)
        self.logger = logging.getLogger(__name__)
        
        self.running = False
        self.paused = False
        self.last_selection_time = 0
        self.last_selected_text = ""
        self.mouse_pressed = False
        self.selection_start_pos = None
        self.selection_threshold_px = 5
        self.was_outlook_active_on_press = False
        
    def start(self):
        self.running = True
        self.detection_thread = threading.Thread(target=self.detection_loop, daemon=True)
        self.detection_thread.start()
        self.logger.info("Metin algılama başlatıldı")
        
    def stop(self):
        self.running = False
        self.logger.info("Metin algılama durduruldu")
        
    def detection_loop(self):
        while self.running:
            try:
                if self.paused:
                    time.sleep(0.1)
                    continue
                self.check_text_selection()
                time.sleep(0.1)
            except Exception as e:
                self.logger.error(f"Algılama döngüsü hatası: {e}")
                time.sleep(1)
                
    def pause(self):
        self.logger.info("Metin algılama duraklatıldı")
        self.paused = True

    def resume(self):
        self.logger.info("Metin algılama devam ediyor")
        self.paused = False

    def _is_outlook_active(self):
        try:
            hwnd = win32gui.GetForegroundWindow()
            if hwnd == 0:
                return False
            
            _, pid = win32process.GetWindowThreadProcessId(hwnd)
            
            h_process = win32api.OpenProcess(win32con.PROCESS_QUERY_LIMITED_INFORMATION, False, pid)
            if not h_process:
                return False

            try:
                exe_path = win32process.GetModuleFileNameEx(h_process, 0)
                exe_name = os.path.basename(exe_path)
                if exe_name.lower() == 'outlook.exe':
                    return True
            finally:
                win32api.CloseHandle(h_process)
                
        except Exception:
            return False
            
        return False

    def check_text_selection(self):
        try:
            left_button_state = win32api.GetKeyState(win32con.VK_LBUTTON)
            
            if left_button_state < 0: # Mouse is pressed down
                if not self.mouse_pressed:
                    # This is the start of a new click action.
                    
                    # Proactive close: If the toolbar is visible, this click's only job is to close it.
                    if self.toolbar.window and self.toolbar.window.winfo_exists():
                        self.toolbar.hide_toolbar()
                        return 

                    # If toolbar was not open, proceed with starting a potential selection.
                    self.mouse_pressed = True
                    self.selection_start_pos = win32gui.GetCursorPos()
                    self.was_outlook_active_on_press = self._is_outlook_active()
            else: # Mouse is up
                if self.mouse_pressed:
                    # This was a drag/selection action, so handle it.
                    self.mouse_pressed = False
                    self.handle_mouse_release()
                    
        except Exception as e:
            self.logger.error(f"Metin seçim kontrolü hatası: {e}")
            
    def handle_mouse_release(self):
        if not self.was_outlook_active_on_press:
            return

        try:
            time.sleep(0.05)
            
            end_pos = win32gui.GetCursorPos()
            did_drag = False
            if self.selection_start_pos is not None and end_pos is not None:
                try:
                    dx = abs(end_pos[0] - self.selection_start_pos[0])
                    dy = abs(end_pos[1] - self.selection_start_pos[1])
                    did_drag = (dx >= self.selection_threshold_px) or (dy >= self.selection_threshold_px)
                except Exception:
                    did_drag = False
            
            if not did_drag:
                return

            selected_text = self.get_selected_text(did_drag=did_drag)
            
            if selected_text and len(selected_text.strip()) > 3:
                current_time = time.time()
                
                if (selected_text != self.last_selected_text or 
                    current_time - self.last_selection_time > 2):
                    
                    self.last_selected_text = selected_text
                    self.last_selection_time = current_time
                    
                    cursor_pos = win32gui.GetCursorPos()
                    x, y = cursor_pos
                    
                    self.toolbar.show_toolbar(x + 10, y + 10, selected_text)
                    self.logger.info(f"Metin seçildi: {selected_text[:50]}...")
                    
        except Exception as e:
            self.logger.error(f"Fare bırakma işlemi hatası: {e}")
            
    def get_selected_text(self, did_drag: bool = False):
        return self._get_selected_text_fallback(did_drag)

    def _get_selected_text_fallback(self, did_drag: bool = False):
        original_clip = None
        try:
            original_clip = pyperclip.paste()
        except Exception:
            pass

        try:
            self.send_copy_command()
            time.sleep(0.05)
            copied_text = pyperclip.paste()

            if original_clip is not None:
                pyperclip.copy(original_clip)
            
            if copied_text == original_clip and not did_drag:
                return ""
            
            return copied_text.strip() if copied_text else ""
        except Exception as e:
            self.logger.error(f"Yedek metin alma hatası: {e}")
            if original_clip is not None:
                try: pyperclip.copy(original_clip)
                except Exception: pass
            return ""

    def send_copy_command(self):
        try:
            win32api.keybd_event(win32con.VK_CONTROL, 0, 0, 0)
            win32api.keybd_event(ord('C'), 0, 0, 0)
            time.sleep(0.01)
            win32api.keybd_event(ord('C'), 0, win32con.KEYEVENTF_KEYUP, 0)
            win32api.keybd_event(win32con.VK_CONTROL, 0, win32con.KEYEVENTF_KEYUP, 0)
        except Exception as e:
            self.logger.error(f"Kopyalama komutu hatası: {e}")
  