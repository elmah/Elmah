@echo off

setlocal
pushd "%~dp0"

set hr=---------------------------------------------------------------------------
set binzip=ELMAH-1.2-sp1-bin-x86.zip
set srczip=ELMAH-1.2-sp1-src.zip
set nuget=nuget\Tools\nuget.exe

:main
call :clean ^
 && call :md tmp ^
 && call :download %binzip% /od:tmp ^
 && call :download %srczip% /od:tmp ^
 && call :unzip tmp\%srczip% -obase -y ^
 && call :unzip tmp\%binzip% -obase -y ^
 && call :autoupdate ^
 && call :packall nuget\*.nuspec
goto :EOF

:clean
call :rd bin && call :rd base && call :rd tmp
goto :EOF

:rd
if exist %1 rd %1 /s /q
if exist %1 exit /b 1
goto :EOF

:md
if not exist %1 md %1
goto :EOF

:download
setlocal
echo %hr%
echo Downloading %1...
call tools\wgets http://elmah.googlecode.com/files/%1 %2 %3 %4 %5 %6 %7 %8 %9
goto :EOF

:unzip
setlocal
echo %hr%
tools\7za x %*
goto :EOF

:autoupdate
echo %hr%
echo Making sure that NuGet.exe is up to date...
"%nuget%" update -self
goto :EOF

:packall
for /f %%F in ('dir /a-d /b "%1"') DO (
CALL :pack nuget\%%F
IF ERRORLEVEL 1 GOTO :EOF
)
GOTO :EOF

:pack
echo %hr%
echo Packaging %1
call :md bin && "%nuget%" pack "%1" -verbose -output bin
GOTO :EOF

:renames
setlocal
for /f "usebackq" %%i in (`powershell -Command "$(Get-Date).ToString('yyyy-MM-dd')"`) do set today=%%i
if "%today%"=="" exit /b 1
ren bin\elmah.1.2.nupkg                  elmah-1.2-%today%.nupkg ^
 && ren bin\elmah.qs.1.2.nupkg           elmah-1.2-qs-%today%.nupkg ^
 && ren bin\elmah.qs.msaccess.1.2.nupkg  elmah-1.2-qs-msaccess-%today%.nupkg ^
 && ren bin\elmah.qs.mysql.1.2.nupkg     elmah-1.2-qs-mysql-%today%.nupkg ^
 && ren bin\elmah.qs.oracle.1.2.nupkg    elmah-1.2-qs-oracle-%today%.nupkg ^
 && ren bin\elmah.qs.pgsql.1.2.nupkg     elmah-1.2-qs-pgsql-%today%.nupkg ^
 && ren bin\elmah.qs.sqlite.1.2.nupkg    elmah-1.2-qs-sqlite-%today%.nupkg ^
 && ren bin\elmah.qs.sqlserver.1.2.nupkg elmah-1.2-qs-sqlserver-%today%.nupkg ^
 && ren bin\elmah.qs.ssce.1.2.nupkg      elmah-1.2-qs-ssce-%today%.nupkg ^
 && ren bin\elmah.qs.xmlfs.1.2.nupkg     elmah-1.2-qs-xmlfs-%today%.nupkg
goto :EOF
