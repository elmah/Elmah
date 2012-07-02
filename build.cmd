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
set MSBUILDEXE=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
if not exist "%MSBUILDEXE%" (
    echo Microsoft Build Engine ^(MSBuild^) 4.0 does not appear to be 
    echo installed on this machine, which is required to build the 
    echo solution.
    exit /b 1
)
set EnableNuGetPackageRestore=true
for %%i in (debug release) do if exist "%MSBUILDEXE%" "%MSBUILDEXE%" Elmah.sln /p:Configuration=%%i /p:DownloadNuGetExe=true %*
