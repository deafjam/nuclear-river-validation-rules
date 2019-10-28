param([string[]]$TaskList = @(), [hashtable]$Properties = @{})
#Requires –Version 3.0

if ($TaskList.Count -eq 0){
	$TaskList = @('Run-UnitTests', 'Build-NuGet')
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#------------------------------
Clear-Host

$Properties.SolutionDir = Join-Path $PSScriptRoot '..'

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
Add-Metadata @{
	'NuGet' = @{
		'Publish' = @{
			'Source' = 'https://www.nuget.org/api/v2/package'
			'PrereleaseSource' = 'http://nuget.2gis.local/api/v2/package'

			'SymbolSource'= 'https://nuget.smbsrc.net/api/v2/package'
			'PrereleaseSymbolSource' = 'http://nuget.2gis.local/SymbolServer/NuGet'
		}
	}
}

Run-Build $TaskList $Properties