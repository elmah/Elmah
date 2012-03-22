@echo off
setlocal
pushd "%~dp0"
hg kwdemo 2>nul > nul
if %errorlevel% neq 0 goto :nohgkwext
if not exist .hg goto :norepo
ver > nul
if exist .hg\hgrc findstr keywordmaps .hg\hgrc > nul
if errorlevel 1 (call :addkws) else (echo keywords appear already configured)
hg kwdemo
goto :EOF

:norepo
echo abort: no repository found in '%cd%' (.hg not found)!
exit /b 1

:nohgkwext
echo hg not installed or not found in PATH!
echo perhaps hg keyword extension is not enabled; see:
echo http://mercurial.selenic.com/wiki/KeywordExtension
exit /b 1

:addkws
setlocal
call :hgrckw >> .hg\hgrc
goto :EOF

:hgrckw
setlocal
echo.
echo [keyword]
echo README.*=
echo src/Elmah/**.cs=
echo src/Elmah/**.sql=
echo src/Elmah/**.vbs=
echo samples/Demo/**.aspx=
echo.
echo [keywordmaps]
echo Id = {file^|basename} {node^|short} {date^|svnutcdate} {author^|user}
echo Revision = {node^|short}
goto :EOF
