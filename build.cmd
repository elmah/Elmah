@echo off
REM
REM ELMAH - Error Logging Modules and Handlers for ASP.NET
REM Copyright (c) 2004-9 Atif Aziz. All rights reserved.
REM
REM  Author(s):
REM
REM      Atif Aziz, http://www.raboof.com
REM
REM Licensed under the Apache License, Version 2.0 (the "License");
REM you may not use this file except in compliance with the License.
REM You may obtain a copy of the License at
REM
REM    http://www.apache.org/licenses/LICENSE-2.0
REM
REM Unless required by applicable law or agreed to in writing, software
REM distributed under the License is distributed on an "AS IS" BASIS,
REM WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
REM See the License for the specific language governing permissions and
REM limitations under the License.
REM
REM -------------------------------------------------------------------------
REM
setlocal enableextensions
pushd "%~dp0"
call :main %*
popd
goto :EOF

:main
if defined ProgramFiles(x86) if exist "%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"            set MSBUILDEXE=%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe
if not defined MSBUILDEXE    if exist "%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"                 set MSBUILDEXE=%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe
if not defined MSBUILDEXE    if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" set MSBUILDEXE=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
if not defined MSBUILDEXE goto :no-msbuild
if not exist "%MSBUILDEXE%" goto :no-msbuild
for /f %%v in ('"%MSBUILDEXE%" /version /nologo') do for /f "delims=. tokens=1-4" %%i in ("%%v") do set vmaj=%%i& set vmin=%%j& set vbld=%%k& set vrev=%%l
echo %vmaj% | findstr [0-9][0-9]* > nul || goto :unknown-msbuild-version
echo %vmin% | findstr [0-9][0-9]* > nul || goto :unknown-msbuild-version
echo %vbld% | findstr [0-9][0-9]* > nul || goto :unknown-msbuild-version
echo %vrev% | findstr [0-9][0-9]* > nul || goto :unknown-msbuild-version
set build="%MSBUILDEXE%" Elmah.sln
if "%vmaj%.%vmin%.%vbld%"=="4.0.30319" (
    if %vrev% lss 17929 (
        echo ================================= WARNING! ================================
        echo Projects targeting Microsoft .NET Framework 4.5 will be skipped as it does
        echo not appear to be installed on this machine.
        echo ===========================================================================
        echo.
        set build=%build% /t:Elmah;Elmah_net-4_0;Elmah_Tests
    )
)
call prestore ^
 && (for %%v in (3.5 4.0 4.5) do for %%c in (Debug Release) do %build% "/p:Configuration=NETFX %%v %%c;AspNetConfiguration=%%c" /v:m %* || exit /b 1)
goto :EOF

:unknown-msbuild-version
echo There was a problem determining the version of your Microsoft Build Engine
echo ^(MSBuild^) 4.0 installation.
exit /b 1

:no-msbuild
echo Microsoft Build Engine ^(MSBuild^) 4.0 does not appear to be
echo installed on this machine, which is required to build the
echo solution.
exit /b 1
