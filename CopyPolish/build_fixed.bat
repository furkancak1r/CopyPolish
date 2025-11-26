@echo off
echo Starting fixed build... > build_fixed_log.txt
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" CopyPolish.csproj /t:Publish /p:Configuration=Release /p:VSToolsPath="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Microsoft\VisualStudio\v17.0" >> build_fixed_log.txt 2>&1
if %errorlevel% neq 0 (
    echo Build failed. See build_fixed_log.txt for details.
    exit /b 1
)
echo Build successful! >> build_fixed_log.txt
exit /b 0
