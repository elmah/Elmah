$solutionDir = [System.IO.Path]::GetDirectoryName($dte.Solution.FullName) + "\"
$path = $installPath.Replace($solutionDir, "`$(SolutionDir)")

$NativeAssembliesDir = Join-Path $path "NativeBinaries"
$x86 = $(Join-Path $NativeAssembliesDir "x86\*.dll")
$x64 = $(Join-Path $NativeAssembliesDir "x64\*.dll")

$PostBuild32v64BitCmd = "
if not exist `"`$(TargetDir)x86`" md `"`$(TargetDir)x86`"
copy `"$x86`" `"`$(TargetDir)x86`"
if not exist `"`$(TargetDir)x64`" md `"`$(TargetDir)x64`"
copy `"$x64`" `"`$(TargetDir)x64"