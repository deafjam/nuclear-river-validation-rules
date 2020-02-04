param([string[]]$TaskList = @(), [hashtable]$Properties = @{})
#Requires -Version 3.0

if ($TaskList.Count -eq 0){
	$TaskList = @('Build-Packages')
}

if ($Properties.Count -eq 0){
	$Properties.EnvironmentType = 'Test'
	$Properties.BusinessModel = 'Russia'
	$Properties.EnvironmentIndex = '20'
	$Properties.EntryPoints = @(
		'ValidationRules.Querying.Host'
		'ValidationRules.Replication.Host'
	)
	#$Properties.UpdateSchemas = 'PriceAggregate'
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#------------------------------

$Properties.SolutionDir = Join-Path $PSScriptRoot '..'
$Properties.BuildFile = Join-Path $PSScriptRoot 'default.ps1'

# Restore-Packages
& {
	& 'dotnet' @('msbuild', '/nologo', '/verbosity:quiet', '/t:Restore', $PSScriptRoot)
	if ($LastExitCode -ne 0) {
		throw "dotnet restore failed with exit code $LastExitCode"
	}
}

$packageName = "2GIS.NuClear.BuildTools"
$packageVersion = ([xml](Get-Content "$PSScriptRoot\build.csproj" -Raw)).SelectSingleNode("//PackageReference[@Include='$packageName']").Version
Import-Module "~\.nuget\packages\$packageName\$packageVersion\tools\buildtools.psm1" -DisableNameChecking -Force
# for local debug
#Import-Module "~\Projects\NuClear\buildtools\src\2GIS.NuClear.BuildTools\buildtools.psm1" -DisableNameChecking -Force

$metadata = & "$PSScriptRoot\metadata.ps1" $Properties
Add-Metadata $metadata

Run-Build $TaskList $Properties
