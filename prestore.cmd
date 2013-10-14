@echo off
setlocal
for %%i in (NuGet.exe) do set nuget_path=%%~dpnx$PATH:i
if "%nuget_path%"=="" goto :nonuget
set packages_path=%~dp0packages
if exist "%packages_path%\repositories.config" (
    for /f "usebackq delims=" %%p in (`PowerShell -C "[xml](Get-Content '%packages_path%\repositories.config') | Select-Xml //repository/@path | %%{$_.Node.Value}"`) do if errorlevel==0 call :restore "%packages_path%\%%p"
) else (
    for /r %%d in (.) do if errorlevel==0 if exist %%d\packages.config call :restore "%%d"
)
goto :EOF

:restore
setlocal
echo Restoring packages for "%~1"
cd "%~dp1" 
"%nuget_path%" install -OutputDirectory "%packages_path%"
goto :EOF

:nonuget
echo NuGet executable not found in PATH
echo For more on NuGet, see http://nuget.codeplex.com
exit /b 2
