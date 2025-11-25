@echo off
echo Trying VS 2022 Community...
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" /t:Publish /p:Configuration=Release > build_output.txt 2>&1
if %errorlevel% equ 0 goto success

echo VS 2022 Community failed or not found. Trying VS 2022 Enterprise...
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" /t:Publish /p:Configuration=Release >> build_output.txt 2>&1
if %errorlevel% equ 0 goto success

echo VS 2022 Enterprise failed or not found. Trying VS 2019 Community...
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" /t:Publish /p:Configuration=Release >> build_output.txt 2>&1
if %errorlevel% equ 0 goto success

echo All attempts failed.
exit /b 1

:success
echo Build successful!
exit /b 0
