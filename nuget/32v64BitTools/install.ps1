param($installPath, $toolsPath, $package, $project)

. (Join-Path $toolsPath "GetPostBuild32v64BitCmd.ps1")

# Get the current Post Build Event cmd
$currentPostBuildCmd = $project.Properties.Item("PostBuildEvent").Value

# Append our post build command if it's not already there
if (!$currentPostBuildCmd.Contains($PostBuild32v64BitCmd)) {
    $project.Properties.Item("PostBuildEvent").Value += $PostBuild32v64BitCmd
}
