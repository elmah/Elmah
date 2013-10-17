@echo off
setlocal
for %%i in (NuGet.exe) do set nuget_path=%%~dpnx$PATH:i
if "%nuget_path%"=="" goto :nonuget
set packages_path=%~dp0packages
if exist "%packages_path%\repositories.config" (
    for /f "usebackq delims=" %%p in (`PowerShell -C "[xml](Get-Content '%packages_path%\repositories.config') | Select-Xml //repository/@path | %%{$_.Node.Value}"`) do call :restore "%packages_path%\%%p" || exit /b 1
) else (
    for /r %%d in (.) do if exist "%%~d\packages.config" call :restore "%%~d\packages.config" || exit /b 1
)
goto :EOF

:restore
setlocal
echo Restoring packages for "%~dp1"
cd "%~dp1" 
"%nuget_path%" install %1 -OutputDirectory "%packages_path%"
exit /b %errorlevel%

:nonuget
echo NuGet executable not found in PATH
echo For more on NuGet, see http://nuget.codeplex.com
exit /b 2
