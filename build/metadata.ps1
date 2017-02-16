﻿param ([hashtable]$Properties)

Import-Module "$PSScriptRoot\metadata.web.psm1" -DisableNameChecking
Import-Module "$PSScriptRoot\metadata.winservice.psm1" -DisableNameChecking
Import-Module "$PSScriptRoot\metadata.transform.psm1" -DisableNameChecking

function Get-EntryPointsMetadata ($EntryPoints, $Context) {

	$entryPointsMetadata = @{}

	# конвертер нужен всегда (очистка ресурсов)
	$Context.EntryPoint = 'ConvertUseCasesService'
	$entryPointsMetadata += Get-WinServiceMetadata $Context

	# production копия конвертера нужна всегда (очистка ресурсов)
	$productionContext = $Context.Clone()
	$productionContext.EnvType = 'Production' 
	$productionContext.EntryPoint = 'ConvertUseCasesService-Production'
	$entryPointsMetadata += Get-WinServiceMetadata $productionContext 

	switch ($EntryPoints){
		'ValidationRules.Querying.Host' {
			$Context.EntryPoint = $_
			$entryPointsMetadata += Get-WebMetadata $Context
		}

		'ValidationRules.Replication.Host' {
			$Context.EntryPoint = $_
			$entryPointsMetadata += Get-WinServiceMetadata $Context
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
    if($updateSchemas -contains 'Facts') {
        $arguments += @('-facts', '-aggregates', '-messages')
    }
    if($updateSchemas -contains 'Aggregates') {
        $arguments += @('-aggregates', '-messages')
    }
    if($updateSchemas -contains 'Messages') {
        $arguments += @('-messages')
    }

	$metadata += @{ 'Arguments' = ($arguments | select -Unique) }

	$Context.EntryPoint = 'ValidationRules.StateInitialization.Host'
	$metadata += Get-TransformMetadata $Context

	return @{ 'ValidationRules.StateInitialization.Host' = $metadata }
}

function Get-UpdateSchemasMetadata ($UpdateSchemas, $Context) {
	$metadata = @{}

	$updateSchemasMetadata = @{ }

	if ($UpdateSchemas -and $UpdateSchemas -isnot [array]){
		$UpdateSchemas = $UpdateSchemas.Split(@(','), 'RemoveEmptyEntries')
	}

	foreach ($schema in $UpdateSchemas){
		foreach($key in $AllSchemas.Keys -match $schema) {
			$updateSchemasMetadata.Add($key, $AllSchemas[$key])
		}
	}

	if ($updateSchemasMetadata.Count -ne 0){
		$metadata += @{ 'UpdateSchemas' = $updateSchemasMetadata }
		$metadata += Get-BulkToolMetadata $UpdateSchemas $Context
	}

	return $metadata
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

    if($Properties['BuildSystem']) {
		$buildSystem = $Properties.BuildSystem
    } else {
		$buildSystem = 'Local'
	}

	$environmentMetadata += @{ 'BuildSystem' = $buildSystem } 
	$environmentMetadata += Get-NuGetMetadata

	$environmentName = $Properties['EnvironmentName']
	if (!$environmentName){
		return $environmentMetadata
	}

    $environmentMetadata += @{ 'Common' = @{ 'EnvironmentName' = $environmentName } }

	$context = $AllEnvironments[$environmentName]
	if ($context -eq $null){
		throw "Can't find environment for name '$environmentName'"
	}
	$context.EnvironmentName = $environmentName

	$context.UseCaseRoute = $Properties['UseCaseRoute']

	if ($Properties.ContainsKey('EntryPoints')){
		$entryPoints = $Properties['EntryPoints']
	
		if ($entryPoints -and $entryPoints -isnot [array]){
			$entryPoints = $entryPoints.Split(@(','), 'RemoveEmptyEntries')
		}

	} else {
		$entryPoints = $AllEntryPoints
	}

	$environmentMetadata += Get-EntryPointsMetadata $entryPoints $context

	$updateSchemas = $Properties['UpdateSchemas']
	if ($updateSchemas){
		if ($updateSchemas -isnot [array]){
			$updateSchemas = $updateSchemas.Split(@(','), 'RemoveEmptyEntries')
		}
		$environmentMetadata += Get-UpdateSchemasMetadata $updateSchemas $context
	}

	return $environmentMetadata
}

$AllSchemas = @{

	'Facts' = @{
		ConnectionStringKey = 'Facts'
		SqlFiles = @(
			'Schemas\DropOldFacts.sql'
			'Schemas\Facts.sql'
		)
	}

	'Aggregates' = @{
		ConnectionStringKey = 'Aggregates'
		SqlFiles = @(
			'Schemas\DropOldAggregates.sql'
			'Schemas\Price.Aggregates.sql'
			'Schemas\Account.Aggregates.sql'
			'Schemas\Consistency.Aggregates.sql'
			'Schemas\Firm.Aggregates.sql'
			'Schemas\Advertisement.Aggregates.sql'
			'Schemas\Project.Aggregates.sql'
			'Schemas\Theme.Aggregates.sql'
		)
	}

	'Messages' = @{
		ConnectionStringKey = 'Messages'
		SqlFiles = @('Schemas\Messages.sql')
	}
}

$AllEntryPoints = @(
	'ValidationRules.Querying.Host'
	'ValidationRules.Replication.Host'
	'ConvertUseCasesService'
)

$AllEnvironments = @{

	'Edu.Chile' = @{ EnvType = 'Edu'; Country = 'Chile' }
	'Edu.Cyprus' = @{ EnvType = 'Edu'; Country = 'Cyprus' }
	'Edu.Czech' = @{ EnvType = 'Edu'; Country = 'Czech' }
	'Edu.Emirates' = @{ EnvType = 'Edu'; Country = 'Emirates' }
	'Edu.Kazakhstan' = @{ EnvType = 'Edu'; Country =' Kazakhstan' }
	'Edu.Russia' = @{ EnvType = 'Edu'; Country = 'Russia' }
	'Edu.Ukraine' = @{ EnvType = 'Edu'; Country = 'Ukraine' }

	'Business.Russia' = @{ EnvType = 'Business'; Country = 'Russia' }
	
	'Int.Chile' = @{ EnvType = 'Int'; Country = 'Chile' }
	'Int.Cyprus' = @{ EnvType = 'Int'; Country = 'Cyprus' }
	'Int.Czech' = @{ EnvType = 'Int'; Country = 'Czech' }
	'Int.Emirates' = @{ EnvType = 'Int'; Country = 'Emirates' }
	'Int.Kazakhstan' = @{ EnvType = 'Int'; Country = 'Kazakhstan' }
	'Int.Russia' = @{ EnvType = 'Int'; Country = 'Russia' }
	'Int.Ukraine' = @{ EnvType = 'Int'; Country = 'Ukraine' }
	
	'Load.Russia' = @{ EnvType = 'Load'; Country = 'Russia' }
	'Load.Cyprus' = @{ EnvType = 'Load'; Country = 'Cyprus' }
	'Load.Czech' = @{ EnvType = 'Load'; Country = 'Czech' }
	'Load.Ukraine' = @{ EnvType = 'Load'; Country = 'Ukraine' }

	'Production.Chile' = @{ EnvType = 'Production'; Country = 'Chile' }
	'Production.Cyprus' = @{ EnvType = 'Production'; Country = 'Cyprus' }
	'Production.Czech' = @{ EnvType = 'Production'; Country = 'Czech' }
	'Production.Emirates' = @{ EnvType = 'Production'; Country = 'Emirates' }
	'Production.Kazakhstan' = @{ EnvType = 'Production'; Country = 'Kazakhstan' }
	'Production.Russia' = @{ EnvType = 'Production'; Country = 'Russia' }
	'Production.Ukraine' = @{ EnvType = 'Production'; Country = 'Ukraine' }
	
	'Test.01' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '01' }
	'Test.02' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '02' }
	'Test.03' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '03' }
	'Test.04' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '04' }
	'Test.05' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '05' }
	'Test.06' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '06' }
	
	'Test.07' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '07' }
	'Test.08' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '08' }
	'Test.09' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '09' }
	'Test.10' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '10' }
	'Test.11' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '11' }
	'Test.12' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '12' }
	'Test.13' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '13' }
	'Test.14' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '14' }
	'Test.15' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '15' }
	'Test.16' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '16' }
	'Test.17' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '17' }
	'Test.18' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '18' }
	'Test.19' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '19' }
	'Test.20' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '20' }
	'Test.21' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '21' }
	'Test.22' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '22' }
	'Test.23' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '23' }
	'Test.24' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '24' }
	'Test.25' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '25' }
	'Test.88' = @{ EnvType = 'Test'; Country = 'Russia'; Index = '88' }

	'Test.101' = @{ EnvType = 'Test'; Country = 'Cyprus'; Index = '101' }
	'Test.102' = @{ EnvType = 'Test'; Country = 'Cyprus'; Index = '102' }
	'Test.103' = @{ EnvType = 'Test'; Country = 'Cyprus'; Index = '103' }
	'Test.108' = @{ EnvType = 'Test'; Country = 'Cyprus'; Index = '108' }
	
	'Test.201' = @{ EnvType = 'Test'; Country = 'Czech'; Index = '201' }
	'Test.202' = @{ EnvType = 'Test'; Country = 'Czech'; Index = '202' }
	'Test.203' = @{ EnvType = 'Test'; Country = 'Czech'; Index = '203' }
	'Test.208' = @{ EnvType = 'Test'; Country = 'Czech'; Index = '208' }
	
	'Test.301' = @{ EnvType = 'Test'; Country = 'Chile'; Index = '301' }
	'Test.302' = @{ EnvType = 'Test'; Country = 'Chile'; Index = '302' }
	'Test.303' = @{ EnvType = 'Test'; Country = 'Chile'; Index = '303' }
	'Test.304' = @{ EnvType = 'Test'; Country = 'Chile'; Index = '304' }
	'Test.308' = @{ EnvType = 'Test'; Country = 'Chile'; Index = '308' }
	'Test.320' = @{ EnvType = 'Test'; Country = 'Chile'; Index = '320' }
	
	'Test.401' = @{ EnvType = 'Test'; Country = 'Ukraine'; Index = '401' }
	'Test.402' = @{ EnvType = 'Test'; Country = 'Ukraine'; Index = '402' }
	'Test.403' = @{ EnvType = 'Test'; Country = 'Ukraine'; Index = '403' }
	'Test.404' = @{ EnvType = 'Test'; Country = 'Ukraine'; Index = '404' }
	'Test.408' = @{ EnvType = 'Test'; Country = 'Ukraine'; Index = '408' }
	
	'Test.501' = @{ EnvType = 'Test'; Country = 'Emirates'; Index = '501' }
	'Test.502' = @{ EnvType = 'Test'; Country = 'Emirates'; Index = '502' }
	'Test.503' = @{ EnvType = 'Test'; Country = 'Emirates'; Index = '503' }
	'Test.508' = @{ EnvType = 'Test'; Country = 'Emirates'; Index = '508' }
	
	'Test.601' = @{ EnvType = 'Test'; Country = 'Kazakhstan'; Index = '601' }
	'Test.602' = @{ EnvType = 'Test'; Country = 'Kazakhstan'; Index = '602' }
	'Test.603' = @{ EnvType = 'Test'; Country = 'Kazakhstan'; Index = '603' }
	'Test.604' = @{ EnvType = 'Test'; Country = 'Kazakhstan'; Index = '604' }
	'Test.605' = @{ EnvType = 'Test'; Country = 'Kazakhstan'; Index = '605' }
	'Test.608' = @{ EnvType = 'Test'; Country = 'Kazakhstan'; Index = '608' }
}

return Parse-EnvironmentMetadata $Properties