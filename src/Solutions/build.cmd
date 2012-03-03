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
REM This is a batch file that can used to build ELMAH for Microsoft .NET 
REM Framework 1.x and 2.0. The build is created for only those versions
REM that are found to be installed in the expected locations (see below).
REM
REM To compile for Microsoft .NET Framework 1.0, you must have Microsoft
REM Visual Studio .NET 2002 installed in the standard path proposed by
REM by its installer.
REM
REM To compile for Microsoft .NET Framework 1.1, you must have Microsoft
REM Visual Studio .NET 2003 installed in the standard path proposed by
REM by its installer.
REM
REM To compile for Microsoft .NET Framework 2.0, you only need 
REM MSBUILD.EXE (version 4.0, part of Microsoft .NET Framework 4.0) and 
REM which is expected to be located in the standard installation directory.

setlocal
pushd "%~dp0"
set DEVENV70EXE=%ProgramFiles%\Microsoft Visual Studio .NET\Common7\IDE\devenv.com
set DEVENV71EXE=%ProgramFiles%\Microsoft Visual Studio .NET 2003\Common7\IDE\devenv.com
set MSBUILD4EXE=%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
for %%i in (debug release) do if exist "%DEVENV70EXE%" "%DEVENV70EXE%" 2002\Elmah.sln /build %%i
for %%i in (debug release) do if exist "%DEVENV71EXE%" "%DEVENV71EXE%" 2003\Elmah.sln /build %%i
for %%i in (debug release) do if exist "%MSBUILD4EXE%" "%MSBUILD4EXE%" 2010\Elmah.sln /p:Configuration=%%i
popd
