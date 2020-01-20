param ([hashtable]$Properties)

Import-Module "$PSScriptRoot\metadata.web.psm1" -DisableNameChecking
Import-Module "$PSScriptRoot\metadata.winservice.psm1" -DisableNameChecking
Import-Module "$PSScriptRoot\metadata.transform.psm1" -DisableNameChecking
Import-Module "$PSScriptRoot\metadata.nunit.psm1" -DisableNameChecking

function Get-EntryPointsMetadata ($EntryPoints, $Context) {

	$entryPointsMetadata = @{}

	switch ($EntryPoints){
		'ValidationRules.Querying.Host' {
			$Context.EntryPoint = $_
			$entryPointsMetadata += Get-WebMetadata $Context
		}

		'ValidationRules.Replication.Host' {
			$Context.EntryPoint = $_
			$entryPointsMetadata += Get-WinServiceMetadata $Context
		}

		'ValidationRules.Replication.Comparison.Tests' {
			$Context.EntryPoint = $_
			$entryPointsMetadata += Get-AssemblyMetadata $Context
		}

		'ValidationRules.Replication.StateInitialization.Tests' {
			$Context.EntryPoint = $_
			$entryPointsMetadata += Get-AssemblyMetadata $Context
		}

		default {
			throw "Can't find entrypoint $_"
		}
	}

	return $entryPointsMetadata
}

function Get-BulkToolMetadata ($updateSchemas, $Context){
	$metadata = @{}

	$arguments = @()
	if($updateSchemas -contains 'ErmFacts') {
		$arguments += @('-erm-facts', '-aggregates', '-messages')
	}
	if($updateSchemas -contains 'KafkaFacts') {
		$arguments += @('-kafka-facts', '-aggregates', '-messages')
	}
	if($updateSchemas -contains 'Aggregates') {
		$arguments += @('-aggregates', '-messages')
	}
	if($updateSchemas -contains 'Messages') {
		$arguments += @('-messages')
	}

	$metadata += @{ 'Arguments' = ( ($arguments + @('-webapp')) | select -Unique) }

	$Context.EntryPoint = 'ValidationRules.StateInitialization.Host'
	$metadata += Get-TransformMetadata $Context

	return @{ 'ValidationRules.StateInitialization.Host' = $metadata }
}

function Get-CommonMetadata (){
	return @{
		'Common' = @{
			'UnitTests' = @{
				# database unit tests performance optimization
				'Configuration' = 'Release'
			}
		}
	}
}

function Get-MSBuildMetadata {
	return @{
		'MSBuild' = @{
			'Setup' = @{
				'UseVisualStudioBuild' = $true
				
				# параллельный билд падает, в sdk пока есть проблемы с этим
				'MaxCpuCount' = 1
			}
		}
	}
}

function Get-NuGetMetadata {
	return @{
		'NuGet' = @{
			'Publish' = @{
				'Source' = 'https://www.nuget.org/api/v2/package'
				'PrereleaseSource' = 'http://nuget.2gis.local/api/v2/package'

				'SymbolSource'= 'https://nuget.smbsrc.net/api/v2/package'
				'PrereleaseSymbolSource' = 'http://nuget.2gis.local/SymbolServer/NuGet'
			}
		}
	}
}

function Parse-EnvironmentMetadata ($Properties) {

	$environmentMetadata = @{}
	$environmentMetadata += Get-CommonMetadata
	$environmentMetadata += Get-MSBuildMetadata
	$environmentMetadata += Get-NuGetMetadata

	if ($Properties['EnvironmentType'] -and $Properties['BusinessModel']){
		$context = @{
			'EnvType' = $Properties.EnvironmentType
			'Country' = $Properties.BusinessModel
		}

		# Используется для именования AppPool сайтов
		$context.EnvironmentName = "$($context.EnvType).$($context.Country)"

		if ($Properties['EnvironmentIndex']) {
			$context.Index = $Properties.EnvironmentIndex
			$context.EnvironmentName += '.' + $context.Index
		}
		# Используется для поиска/именования сервисов в buildtools
		$environmentMetadata.Common.EnvironmentName = $context.EnvironmentName

		Write-Host "Environment name $($context.EnvironmentName)"

		if ($Properties['EntryPoints']){
			$entryPoints = $Properties.EntryPoints
		
			if ($entryPoints -and $entryPoints -isnot [array]){
				$entryPoints = $entryPoints.Split(@(','), [System.StringSplitOptions]::RemoveEmptyEntries)
			}
	
			$environmentMetadata += Get-EntryPointsMetadata $entryPoints $context
		}

		if ($Properties['UpdateSchemas']){
			$updateSchemas = $Properties.UpdateSchemas 
	
			if ($updateSchemas -isnot [array]){
				$updateSchemas = $updateSchemas.Split(@(','), [System.StringSplitOptions]::RemoveEmptyEntries)
			}
			$environmentMetadata += @{ 'UpdateSchemas' = $true }
	
			$environmentMetadata += Get-BulkToolMetadata $updateSchemas $context
		}
	}
	
	return $environmentMetadata
}

return Parse-EnvironmentMetadata $Properties