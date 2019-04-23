Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\artifacts.psm1" -DisableNameChecking

Task QueueBuild-BulkTool  {
	if ($Metadata['ValidationRules.StateInitialization.Host']){
		$projectFileName = Get-ProjectFileName 'src' 'ValidationRules.StateInitialization.Host'
		QueueBuild-AppPackage $projectFileName 'ValidationRules.StateInitialization.Host'
	}
}

Task Run-BulkTool -Precondition { $Metadata['ValidationRules.StateInitialization.Host'] }{
	Run-BulkTool $Metadata['ValidationRules.StateInitialization.Host'].Arguments
}

Task Run-BulkTool-Drop -Precondition { $Metadata['ValidationRules.StateInitialization.Host'] }{
	Run-BulkTool @('-webapp-drop')
}

function Run-BulkTool ($arguments){
	$artifactName = Get-Artifacts 'ValidationRules.StateInitialization.Host'
	$exePath = Join-Path $artifactName '2GIS.NuClear.ValidationRules.StateInitialization.Host.exe'

	Write-Host 'Invoke bulktool with' $arguments
	& $exePath $arguments | Write-Host

	if ($LastExitCode -ne 0) {
		throw "Command failed with exit code $LastExitCode"
	}
}