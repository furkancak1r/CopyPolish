@echo off
echo Starting build... > my_build_log.txt
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" CopyPolish.csproj /t:Publish /p:Configuration=Release >> my_build_log.txt 2>&1
if %errorlevel% neq 0 (
    echo Community failed, trying Enterprise... >> my_build_log.txt
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" CopyPolish.csproj /t:Publish /p:Configuration=Release >> my_build_log.txt 2>&1
)
echo Done. >> my_build_log.txt
