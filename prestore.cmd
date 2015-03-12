@echo off
pushd "%~dp0"
call :main %*
popd
goto :EOF

:main
setlocal
for /f %%p in ('dir packages.config /s/b ^| findstr /v /i PrecompiledWeb') do (call :restore %%p || exit /b 1)
goto :EOF

:restore
setlocal
echo Restoring packages for "%~dp1"
tools\NuGet.exe restore %1 -PackagesDirectory packages
goto :EOF
