@echo off
setlocal
pushd "%~dp0"
rem TODO check hg can be found in path
rem TODO check whether keywords are already configured
if exist .hg (call :addkws) else (call :norepo)
goto :EOF

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

:norepo
setlocal
echo abort: no repository found in '%cd%' (.hg not found)!
exit /b 1
