@echo off

REM ELMAH - Error Logging Modules and Handlers for ASP.NET
REM Copyright (c) 2007 Atif Aziz. All rights reserved.
REM
REM  Author(s):
REM
REM      Atif Aziz, http://www.raboof.com
REM
REM This library is free software; you can redistribute it and/or modify it 
REM under the terms of the New BSD License, a copy of which should have 
REM been delivered along with this distribution.
REM
REM THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
REM "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
REM LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
REM PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
REM OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
REM SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
REM LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
REM DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
REM THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
REM (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
REM OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
REM
REM -------------------------------------------------------------------------
REM

setlocal

REM Check if delayed evaluation of environment variables is enabled.
REM If not then re-start the script with them enabled.

set DELAY_TEST=test
if not "!DELAY_TEST!"=="test" (
    cmd /v /c "%0"
    goto :eof
)
pushd "%~dp0"

set LIB_PATH_x64=lib\x64
set BIN_PATH=bin\net-2.0\Release
set DEMO_PATH=samples\Demo
set DEMO_BIN_PATH=%DEMO_PATH%\bin
set DEMO_PORT=54321
set TOOLS_PATH=tools

if exist "%SystemRoot%\Microsoft.NET\Framework\v2.0.50727" goto go

echo The .NET Framework 2.0 does not appear to be installed on this 
echo machine, which is required to run the demo Web site.
set /p answer=Proceed anyway?
if "%answer%"=="y" goto go
if "%answer%"=="Y" goto go
exit /b 1

:go
if not exist "%BIN_PATH%" call build 2.0
if not exist "%DEMO_BIN_PATH%" md "%DEMO_BIN_PATH%"
copy /y "%BIN_PATH%" "%DEMO_BIN_PATH%"

if %PROCESSOR_ARCHITECTURE%==AMD64 copy /y "%LIB_PATH_x64%\System.Data.SQLite.DLL" "%DEMO_BIN_PATH%"

set MAIL_PATH=%DEMO_PATH%\Mails
if not exist "%MAIL_PATH%" md "%MAIL_PATH%"
if not exist "%DEMO_PATH%\App_Data" md "%DEMO_PATH%\App_Data"
if exist "%DEMO_PATH%\Web.config" del "%DEMO_PATH%\Web.config"
for /f "tokens=* delims=" %%i in (%DEMO_PATH%\Web.config.template) do (
    set LINE=%%i
    echo !LINE:{pickupDirectoryLocation}=%cd%\%MAIL_PATH%! >> "%DEMO_PATH%\Web.config"
)

start /min %TOOLS_PATH%\Cassini\Cassini "%cd%\%DEMO_PATH%" %DEMO_PORT% --launch

popd
