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
setlocal
pushd "%~dp0"
call :main %*
popd
goto :EOF

:main
set MSBUILDEXE=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
if not exist "%MSBUILDEXE%" (
    echo Microsoft Build Engine ^(MSBuild^) 4.0 does not appear to be 
    echo installed on this machine, which is required to build the 
    echo solution.
    exit /b 1
)
for /f %%v in ('%MSBUILDEXE% /version /nologo') do for /f "delims=. tokens=1-4" %%i in ("%%v") do set vmaj=%%i& set vmin=%%j& set vbld=%%k& set vrev=%%l
if not "%vmaj%.%vmin%.%vbld%"=="4.0.30319" (
    echo There was a problem determining the version of your Microsoft Build Engine 
    echo ^(MSBuild^) 4.0 installation.
    exit /b 1
)
"%MSBUILDEXE%" nugetRestore.proj
set build="%MSBUILDEXE%" Elmah.sln
if %vrev% lss 17929 (
    echo ================================= WARNING! ================================
    echo Projects targeting Microsoft .NET Framework 4.5 will be skipped as it does 
    echo not appear to be installed on this machine.
    echo ===========================================================================
    echo.
    set build=%build% /t:Elmah;Elmah_net-4_0;Elmah_Tests
)
for %%v in (3.5 4.0 4.5) do for %%c in (Debug Release) do %build% "/p:Configuration=NETFX %%v %%c;AspNetConfiguration=%%c" /v:m %* || exit /b 1
goto :EOF
