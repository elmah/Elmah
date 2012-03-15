@echo off
setlocal
pushd "%~dp0"
for /f %%i in (kwindex.txt) do call :id %%i
goto :EOF

:id
setlocal
echo %1
for /f "usebackq tokens=*" %%i in (`hg parents %1 --template "%~nx1 {node|short} {date|isodate} {author|user}\n"`) do set x=%%i
call replace %1 \$Id(:.+?)?\$ "$Id: %x% $" > %temp%\%~nx1
copy %temp%\%~nx1 %1 /y > nul
goto :EOF
