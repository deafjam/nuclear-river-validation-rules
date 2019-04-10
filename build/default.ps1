Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\metadata.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\buildqueue.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\deployqueue.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\deploy.winservice.psm1" -DisableNameChecking

Include "$BuildToolsRoot\psake\common.ps1"
Include "$BuildToolsRoot\psake\unittests.ps1"
Include 'servicebus.ps1'
Include 'convertusecases.ps1'
Include 'bulktool.ps1'
Include 'unittests.ps1'

# Querying.Host
Task QueueBuild-QueryingHost {
	if ($Metadata['ValidationRules.Querying.Host']){
		$projectFileName = Get-ProjectFileName 'src' 'ValidationRules.Querying.Host'
		QueueBuild-WebPackage $projectFileName 'ValidationRules.Querying.Host'
	}
}
Task QueueDeploy-QueryingHost {
	if ($Metadata['ValidationRules.Querying.Host']){
		QueueDeploy-WebPackage 'ValidationRules.Querying.Host'
	}
}

# Replication.Host
Task QueueBuild-ReplicationHost {
	if ($Metadata['ValidationRules.Replication.Host']){
		$projectFileName = Get-ProjectFileName 'src' 'ValidationRules.Replication.Host'
		QueueBuild-AppPackage $projectFileName 'ValidationRules.Replication.Host'
	}
}
Task QueueDeploy-ReplicationHost {
	if ($Metadata['ValidationRules.Replication.Host']){
		QueueDeploy-WinService 'ValidationRules.Replication.Host'
	}
}

# Replication.Host
Task QueueBuild-Tests {
	if ($Metadata['ValidationRules.Replication.Comparison.Tests']){
		$projectFileName = Get-ProjectFileName 'test' 'ValidationRules.Replication.Comparison.Tests'
		QueueBuild-AppPackage $projectFileName 'ValidationRules.Replication.Comparison.Tests'
	}

	if ($Metadata['ValidationRules.Replication.StateInitialization.Tests']){
		$projectFileName = Get-ProjectFileName 'test' 'ValidationRules.Replication.StateInitialization.Tests'
		QueueBuild-AppPackage $projectFileName 'ValidationRules.Replication.StateInitialization.Tests'
	}
}

Task Stop-ReplicationHost -Precondition { $Metadata['ValidationRules.Replication.Host'] -or $Metadata['ValidationRules.StateInitialization.Host']} {
	Load-WinServiceModule 'ValidationRules.Replication.Host'
	Take-WinServiceOffline 'ValidationRules.Replication.Host'
}

Task Validate-PullRequest -depends Run-UnitTestsCore

Task Build-Packages -depends `
	Build-ConvertUseCasesService, `
	QueueBuild-BulkTool, `
	QueueBuild-QueryingHost, `
	QueueBuild-ReplicationHost, `
	QueueBuild-Tests, `
	Build-Queue

Task Deploy-Packages -depends `
	Stop-ReplicationHost, `
	Deploy-ServiceBus, `
	Run-BulkTool, `
	QueueDeploy-ConvertUseCasesService, `
	QueueDeploy-QueryingHost, `
	QueueDeploy-ReplicationHost, `
	Deploy-Queue