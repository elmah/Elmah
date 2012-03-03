@echo off

echo ELMAH - Error Logging Modules and Handlers for ASP.NET
echo Copyright (c) 2004-9 Atif Aziz. All rights reserved.

setlocal
pushd "%~dp0"

set NETFX_BASE_PATH=%SystemRoot%\Microsoft.NET\Framework

if "%1"=="" call :help
if "%1"=="all" call :all
if "%1"=="1.0" call :net-1-0
if "%1"=="1.1" call :net-1-1
if "%1"=="2.0" call :net-2-0
if "%1"=="solutions" call :solutions
popd
goto :EOF

:all
call :lic
call :net-1-0
call :net-1-1
call :net-2-0
goto :EOF

:net-1-0
call :compile v1.0.3705 net-1.0 /d:NET_1_0
goto :EOF

:net-1-1
call :compile v1.1.4322 net-1.1 /d:NET_1_1 /r:System.Data.OracleClient.dll
goto :EOF

:net-2-0
call :compile v2.0.50727 net-2.0 /d:NET_2_0 /r:lib\System.Data.SQLite.dll /r:lib\Npgsql.dll /r:lib\mysql.data.dll /r:lib\System.Data.SqlServerCe.dll /nowarn:618
call :deps 2.0
goto :EOF

:deps
echo.
echo Copying dependencies to output directories...
for %%i in (Debug Release) do if exist bin\net-%1\%%i copy lib bin\net-%1\%%i
goto :EOF

:solutions
call src\Solutions\build
goto :EOF

:compile
echo.
echo Compiling for Microsoft .NET Framework %1
set CSC_PATH=%NETFX_BASE_PATH%\%1\csc.exe
if not exist "%CSC_PATH%" (
    echo.
    echo WARNING! 
    echo Microsoft .NET Framework %1 does not appear installed on 
    echo this machine. Skipping target!
    goto :EOF
)
set BIN_OUT_DIR=bin\%2
for %%i in (Debug Release) do if not exist %BIN_OUT_DIR%\%%i md %BIN_OUT_DIR%\%%i
echo Compiling DEBUG configuration
echo.
set CSC_FILES=/recurse:src\Elmah\*.cs /res:src\Elmah\ErrorLog.css,Elmah.ErrorLog.css /res:src\Elmah\RemoteAccessError.htm,Elmah.RemoteAccessError.htm /res:src\Elmah\mkmdb.vbs,Elmah.mkmdb.vbs
set CSC_COMMON=/unsafe- /checked- /warnaserror+ /nowarn:1591,618 /warn:4 /d:TRACE /debug+ /baseaddress:285212672 /r:Microsoft.JScript.dll /r:Microsoft.Vsa.dll
"%CSC_PATH%" /t:library /out:%BIN_OUT_DIR%\Debug\Elmah.dll   %CSC_COMMON% /doc:%BIN_OUT_DIR%\Debug\Elmah.xml   /debug:full               %CSC_FILES% /d:DEBUG %3 %4 %5 %6 %7 %8 %9
echo Compiling RELEASE configuration
echo.
"%CSC_PATH%" /t:library /out:%BIN_OUT_DIR%\Release\Elmah.dll %CSC_COMMON% /doc:%BIN_OUT_DIR%\Release\Elmah.xml /debug:pdbonly /optimize+ %CSC_FILES%          %3 %4 %5 %6 %7 %8 %9
goto :EOF

:help
echo.
echo Usage: %~n0 TARGET
echo.
echo TARGET
echo     is the target to build (all, 1.0, 1.1, 2.0)
echo.
echo This is a batch script that can used to build ELMAH binaries for 
echo Microsoft .NET Framework 1.x and 2.0. The binaries are created for 
echo only those versions that are found to be installed in the expected 
echo locations on the local machine.
echo.
echo The following versions appear to be installed on this system:
echo.
for %%i in (v1.0.3705 v1.1.4322 v2.0.50727) do if exist "%NETFX_BASE_PATH%\%%i\csc.exe" echo - %%i
call :lic
goto :EOF


:lic
echo.
echo Licensed under the Apache License, Version 2.0 (the "License");
echo you may not use this file except in compliance with the License.
echo You may obtain a copy of the License at
echo.
echo    http://www.apache.org/licenses/LICENSE-2.0
echo.
echo Unless required by applicable law or agreed to in writing, software
echo distributed under the License is distributed on an "AS IS" BASIS,
echo WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
echo See the License for the specific language governing permissions and
echo limitations under the License.
goto :EOF
