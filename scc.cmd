@echo off
pushd "%~dp0"
PowerShell -Version 2 -C "git ls-files | ? { $_ -match '^src/.+\.(cs|sql|vbs|css)$' } | %% { echo \"[assembly: Elmah.Scc(\"\"`$Id: $(Split-Path -Leaf $_) $(git log '--format=%%h %%ai %%an' -1 $_) `$\"\")]\" }" > src\SccStamps.g.cs
popd