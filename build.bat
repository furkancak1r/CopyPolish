:: C:\Users\furkan.cakir\Desktop\FurkanPRS\Kodlar\test\Auto_Copy_AI\build.bat
@echo off
echo Building executable file...

pip install pyinstaller

echo Stopping any running CopyPolish.exe...
taskkill /F /IM CopyPolish.exe >NUL 2>&1
timeout /T 1 /NOBREAK >NUL

echo Cleaning previous build artifacts...
if exist build rmdir /S /Q build
if exist dist rmdir /S /Q dist

pyinstaller --onefile --windowed --uac-admin --clean --noconfirm --name CopyPolish main.py

echo.
echo Build finished.
echo The executable can be found in the 'dist' folder as CopyPolish.exe.
pause
