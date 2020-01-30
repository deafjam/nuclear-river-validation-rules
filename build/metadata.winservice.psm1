Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$PSScriptRoot\metadata.web.psm1" -DisableNameChecking
Import-Module "$PSScriptRoot\metadata.transform.psm1" -DisableNameChecking
Import-Module "$PSScriptRoot\metadata.servicebus.psm1" -DisableNameChecking

function Get-QuartzConfigMetadata ($Context){

	$quartzConfigs = @()

	switch ($Context.EnvType){
		'Production' {
			switch ($Context.Country){
				'Russia' {
					$quartzConfigs += @('kafka.quartz.Production.Russia.config')
					$quartzConfigs += @('quartz.Production.Russia.config')
					$quartzConfigs += @('quartz.Production.config')
				}
				default {
					$quartzConfigs += @('quartz.Production.config')
				}
			}
		}
		default {
			switch ($Context.Country){
				'Russia' {
					$quartzConfigs += @('Templates\kafka.quartz.Test.Russia.config')
					$quartzConfigs += @('Templates\quartz.Test.Russia.config')
					$quartzConfigs += @('Templates\quartz.Test.config')
				}
				default {
					$quartzConfigs += @('Templates\quartz.Test.config')
				}
			}
		}
	}

	return @{
		'QuartzConfigs' =  $quartzConfigs
	}
}

function Get-TargetHostsMetadata ($Context){

	switch ($Context.EnvType) {
		'Production' {
			switch ($Context.EntryPoint){
				'ValidationRules.Replication.Host'{
					return @{ 'TargetHosts' = @('uk-erm-bus01', 'uk-erm-bus02', 'uk-erm-bus03', 'uk-erm-bus04') }
				}
				default {
					throw "Unknown entrypoint $_"
				}
			}
		}
		'Load' {
			return @{ 'TargetHosts' = @('uk-erm-iis10', 'uk-erm-iis11', 'uk-erm-iis12') }
		}
		'Test' {
			switch ($Context.Country) {
				'Russia' {
					return @{ 'TargetHosts' = @('uk-erm-test03') }
				}
				default {
					return @{ 'TargetHosts' = @('uk-erm-test02') }
				}
			}
		}
		default {
			$webMetadata = Get-WebMetadata $Context
			if ($webMetadata -eq $null){
				throw "Can't find web metadata for entrypoint $($Context.EntryPoint)"
			}

			return @{'TargetHosts' = $webMetadata[$Context.EntryPoint].TargetHosts}
		}
	}
}

function Get-ServiceNameMetadata ($Context) {
	switch ($Context.EntryPoint) {
		'ValidationRules.Replication.Host' {
			return @{
				'ServiceName' = 'ValidationRules.Replication.Host'
				'ServiceDisplayName' = '2GIS NuClear River ValidationRules Replication Host Service'
			}
		}
		default {
			return @{}
		}
	}
}

function Get-WinServiceMetadata ($Context) {

	$metadata = @{}
	$metadata += Get-TargetHostsMetadata $Context
	$metadata += Get-QuartzConfigMetadata $Context
	$metadata += Get-ServiceNameMetadata $Context
	$metadata += Get-TransformMetadata $Context
	$metadata += Get-ServiceBusMetadata $Context

	return @{ "$($Context.EntryPoint)" = $metadata }
}

Export-ModuleMember -Function Get-WinServiceMetadata
