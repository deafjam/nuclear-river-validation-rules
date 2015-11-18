﻿Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#------------------------------

Import-Module "$BuildToolsRoot\modules\msbuild.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\artifacts.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\deploy.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\nuget.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\metadata.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking

Include "$BuildToolsRoot\psake\tasks.ps1"
Include 'convertusecases.ps1'
Include 'updateschemas.ps1'
Include 'bulktool.ps1'
Include 'datatest.ps1'

Task Run-DataTests -Depends QueueBuild-BulkTool,Build-Packages -Precondition { $Metadata['Replication.Bulk'] } {
    $artifactName = Get-Artifacts 'Replication.Bulk'
    $artifactExecutable = Join-Path $artifactName '2GIS.NuClear.Replication.Bulk.exe'
	$projects = Find-Projects '.' '*DataTest.Model*'
	Run-DataTests $projects 'UnitTests' $artifactExecutable.
}

Task QueueBuild-OData -Precondition { $Metadata['Web.OData'] } {
	$projectFileName = Get-ProjectFileName 'Querying' 'Querying.Web.OData'
	QueueBuild-WebPackage $projectFileName 'Web.OData'
}
Task Deploy-OData -Depends Take-ODataOffline -Precondition { $Metadata['Web.OData'] } {
	Deploy-WebPackage 'Web.OData'
	Validate-WebSite 'Web.OData' 'CustomerIntelligence/$metadata'
}

Task Take-ODataOffline -Precondition { $Metadata['Web.OData'] } {
	Take-WebsiteOffline 'Web.OData'
}

Task QueueBuild-TaskService -Precondition { $Metadata['Replication.EntryPoint'] } {
	$projectFileName = Get-ProjectFileName 'Replication' 'Replication.EntryPoint'
	QueueBuild-AppPackage $projectFileName 'Replication.EntryPoint'
}

Task Deploy-TaskService -Depends Import-WinServiceModule, Take-TaskServiceOffline -Precondition { $Metadata['Replication.EntryPoint'] } {
	Deploy-WinService 'Replication.EntryPoint'
}

Task Take-TaskServiceOffline -Depends Import-WinServiceModule -Precondition { $Metadata['Replication.EntryPoint'] } {
	Take-WinServiceOffline 'Replication.EntryPoint'
}

Task Import-WinServiceModule {
	Load-WinServiceModule 'Replication.EntryPoint'
}

Task Build-Packages -depends `
Build-ConvertUseCasesService, `
QueueBuild-OData, `
QueueBuild-TaskService, `
QueueBuild-BulkTool, `
Build-Queue

Task Deploy-Packages -depends `
Take-ODataOffline, `
Take-TaskServiceOffline, `
Update-Schemas, `
Run-BulkTool, `
Deploy-ConvertUseCasesService, `
Deploy-OData, `
Deploy-TaskService