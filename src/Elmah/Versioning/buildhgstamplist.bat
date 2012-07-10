@echo off

setlocal
pushd "%~dp0"


set hr=---------------------------------------------------------------------------
set basedir=..\
set outputfile=HgSccStampList.Populate.cs
set currlocalidfile=hgscc.localid.curr.txt
set currremoteidfile=hgscc.remoteid.curr.txt
set prevlocalidfile=hgscc.localid.prev.txt
set prevremoteidfile=hgscc.remoteid.prev.txt
set filechangesetsfile=hgscc.filechangesets.txt
:main
call :prepare ^
  && call :getchangesets ^
  && call :createfile
goto :EOF

:prepare
echo %hr%
echo Making sure current id files exist
call :ensurefileexists %currlocalidfile%
call :ensurefileexists %currremoteidfile%

echo Backing up current id files
call :backuppreviousrun %currlocalidfile% %prevlocalidfile%
call :backuppreviousrun %currremoteidfile% %prevremoteidfile%

goto :EOF

:getchangesets
echo %hr%
echo Getting the current local changeset
hg identify -i > %currlocalidfile%

echo Getting the current remote changeset
hg id -r tip http://code.google.com/p/elmah > %currremoteidfile%

goto :EOF

:createfile

IF NOT EXIST %outputfile% (
call :getfilechangesets ^
  && call :recreatefile
goto :EOF
)

fc %currlocalidfile% %prevlocalidfile% > nul
if errorlevel 1 (
REM the local changeset has changed, so we need to recreate the output
call :getfilechangesets ^
  && call :recreatefile
goto :EOF
)

fc %currremoteidfile% %prevremoteidfile% > nul
if errorlevel 1 (
REM the local changeset has changed, so we need to recreate the output
call :recreatefile
goto :EOF
)

echo %hr%
echo Nothing has changed, so keeping existing output
goto :EOF

:ensurefileexists
IF NOT EXIST %1 echo Not Found > %1
goto :EOF

:backuppreviousrun
copy /y %1 %2
goto :EOF

:getfilechangesets

echo %hr%
echo Getting file changesets
del %filechangesetsfile%
for /f %%F in ('dir /s /a-d /b "%basedir%"') DO (
call :hgfile %%F
)
goto :EOF

:recreatefile

echo %hr%
echo Recreating file
type headertemplate.txt > %outputfile%
call :setproperty LocalChangeset %currlocalidfile%
call :setproperty RemoteChangeset %currremoteidfile%
type %filechangesetsfile% >> %outputfile%
type footertemplate.txt >> %outputfile%
goto :EOF

:hgfile
echo %1
hg log -l 1 --template "            SccStamps.Add(new HgSccStamp(@""%1"", ""{date|isodatesec}"", ""{author}"", ""{node|short}""));\r\n" %1 >> %filechangesetsfile%
goto :EOF

:setproperty
echo             %1 = @^" >> %outputfile%
type %2 >> %outputfile%
echo                   ^"; >> %outputfile%
goto :EOF
