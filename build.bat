:: C:\Users\furkan.cakir\Desktop\FurkanPRS\Kodlar\test\Auto_Copy_AI\build.bat
@echo off
echo Building executable file...

pip install pyinstaller

pyinstaller --onefile --windowed --name CopyPolish main.py

echo.
echo Build finished.
echo The executable can be found in the 'dist' folder as CopyPolish.exe.
pause