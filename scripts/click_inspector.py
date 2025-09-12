import time
import sys
import os
import ctypes

try:
    import win32api
    import win32con
    import win32gui
    import win32process
except ImportError as e:
    print("pywin32 gerekli: pip install pywin32", file=sys.stderr)
    raise

# UI Automation (tercihen): daha detaylı kontrol bilgileri için
try:
    import uiautomation as auto  # pip install uiautomation
except Exception:
    auto = None


def get_process_name(pid: int) -> str:
    """PID'den exe adını elde etmeye çalış (yolun sadece adı)."""
    try:
        PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
        handle = win32api.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, False, pid)
        try:
            exe_path = win32process.GetModuleFileNameEx(handle, 0)
            return os.path.basename(exe_path)
        finally:
            win32api.CloseHandle(handle)
    except Exception:
        return "?"


def print_win32_info(x: int, y: int) -> int:
    """İmleç altındaki pencere için Win32 (HWND) bilgilerini yazdır."""
    try:
        hwnd = win32gui.WindowFromPoint((x, y))
    except Exception:
        hwnd = 0

    print("- Win32")
    if not hwnd:
        print("  hwnd: 0 (bulunamadı)")
        return 0

    try:
        class_name = win32gui.GetClassName(hwnd)
    except Exception:
        class_name = "?"
    try:
        title = win32gui.GetWindowText(hwnd)
    except Exception:
        title = "?"

    try:
        _, pid = win32process.GetWindowThreadProcessId(hwnd)
    except Exception:
        pid = 0

    rect = None
    try:
        rect = win32gui.GetWindowRect(hwnd)
    except Exception:
        rect = None

    print(f"  hwnd: 0x{hwnd:08X}")
    print(f"  class: {class_name}")
    print(f"  title: {title}")
    print(f"  pid: {pid} ({get_process_name(pid)})")
    if rect:
        l, t, r, b = rect
        print(f"  rect: ({l}, {t}, {r}, {b})")

    # Ebeveyn zinciri (kısa)
    try:
        parent = win32gui.GetParent(hwnd)
        depth = 0
        while parent and depth < 5:
            try:
                p_class = win32gui.GetClassName(parent)
            except Exception:
                p_class = "?"
            try:
                p_title = win32gui.GetWindowText(parent)
            except Exception:
                p_title = "?"
            print(f"  ^ parent[{depth}]: hwnd=0x{parent:08X} class={p_class} title={p_title}")
            parent = win32gui.GetParent(parent)
            depth += 1
    except Exception:
        pass

    return hwnd


def safe_get(obj, name, default=""):
    try:
        return getattr(obj, name)
    except Exception:
        return default


def print_uia_info(x: int, y: int):
    """İmleç altındaki UI Automation kontrol bilgisini yazdır."""
    if auto is None:
        print("- UIA: uiautomation kurulu değil (pip install uiautomation)")
        return

    try:
        ctrl = auto.ControlFromPoint(x, y)
    except Exception as e:
        print(f"- UIA: ControlFromPoint hatası: {e}")
        return

    print("- UIA")
    try:
        # Temel özellikler
        name = safe_get(ctrl, 'Name', '')
        ctype = safe_get(ctrl, 'ControlTypeName', '')
        cls = safe_get(ctrl, 'ClassName', '')
        aid = safe_get(ctrl, 'AutomationId', '')
        fid = safe_get(ctrl, 'FrameworkId', '')
        pid = safe_get(ctrl, 'ProcessId', 0)
        nwh = safe_get(ctrl, 'NativeWindowHandle', 0)
        rect = safe_get(ctrl, 'BoundingRectangle', None)

        print(f"  name: {name}")
        print(f"  type: {ctype}")
        print(f"  class: {cls}")
        print(f"  automation_id: {aid}")
        print(f"  framework: {fid}")
        print(f"  pid: {pid} ({get_process_name(pid)})")
        if nwh:
            print(f"  native_hwnd: 0x{int(nwh):08X}")
        if rect:
            try:
                print(f"  rect: ({rect.left}, {rect.top}, {rect.right}, {rect.bottom})")
            except Exception:
                print(f"  rect: {rect}")

        # Atalar zinciri (kısa)
        try:
            parent = ctrl.GetParentControl()
            depth = 0
            while parent and depth < 6:
                pname = safe_get(parent, 'Name', '')
                ptype = safe_get(parent, 'ControlTypeName', '')
                pcls = safe_get(parent, 'ClassName', '')
                paid = safe_get(parent, 'AutomationId', '')
                print(f"  ^ parent[{depth}]: type={ptype} name=\"{pname}\" class={pcls} aid={paid}")
                parent = parent.GetParentControl()
                depth += 1
        except Exception:
            pass

        # Üst seviye kontrol
        try:
            top = ctrl.GetTopLevelControl()
            tname = safe_get(top, 'Name', '')
            ttype = safe_get(top, 'ControlTypeName', '')
            tcls = safe_get(top, 'ClassName', '')
            print(f"  top_level: type={ttype} name=\"{tname}\" class={tcls}")
        except Exception:
            pass

    except Exception as e:
        print(f"  UIA yazdırma hatası: {e}")


def main():
    print("Click Inspector başlatıldı.")
    print("Sol tık (sadece Outlook'ta): imleç altı info | Ctrl+C: çıkış")

    was_pressed = False
    try:
        while True:
            state = win32api.GetKeyState(win32con.VK_LBUTTON)
            if state < 0 and not was_pressed:
                was_pressed = True
            elif state >= 0 and was_pressed:
                was_pressed = False
                x, y = win32gui.GetCursorPos()

                try:
                    hwnd = win32gui.WindowFromPoint((x, y))
                    if hwnd:
                        _, pid = win32process.GetWindowThreadProcessId(hwnd)
                        process_name = get_process_name(pid)
                        if process_name and process_name.lower() == 'outlook.exe':
                            ts = time.strftime('%H:%M:%S')
                            print(f"\n=== Click @ ({x}, {y}) in Outlook [{ts}] ===")
                            print_win32_info(x, y)
                            print_uia_info(x, y)
                except Exception:
                    pass
            time.sleep(0.03)
    except KeyboardInterrupt:
        print("\nÇıkılıyor...")


if __name__ == '__main__':
    main()
  