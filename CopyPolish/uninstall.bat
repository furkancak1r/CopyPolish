@echo off
setlocal enabledelayedexpansion

:: CopyPolish ClickOnce uninstall helper

echo [1/5] Outlook kapatiliyor (yoksayilabilir)...
taskkill /IM outlook.exe /F >nul 2>&1

echo [2/5] ClickOnce cache temizleniyor...
rundll32 dfshim CleanOnlineAppCache

echo [3/5] CopyPolish kisa yollarini temizleniyor...
set START_SHORTCUT="%APPDATA%\Microsoft\Windows\Start Menu\Programs\HP Inc\CopyPolish.lnk"
if exist %START_SHORTCUT% del /f /q %START_SHORTCUT%

echo [4/5] ClickOnce uygulama klasorleri temizleniyor...
set APPROOT=%LOCALAPPDATA%\Apps\2.0
for /d %%D in ("%APPROOT%\*copy..vsto*") do (
  echo   - removing %%D
  rmdir /s /q "%%D"
)
for /d %%D in ("%APPROOT%\*\*\copy..vsto*") do (
  echo   - removing %%D
  rmdir /s /q "%%D"
)

echo [5/5] Web kopyasi varsa ClickOnce cache'ten temizleniyor...
mage -cc >nul 2>&1

echo.
echo Temizlik tamam. Simdi 'dist\CopyPolish.vsto' dosyasini yeniden calistirarak kurulumu baslatabilirsiniz.
pause
