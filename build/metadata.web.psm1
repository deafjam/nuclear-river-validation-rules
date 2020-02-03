Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$PSScriptRoot\metadata.transform.psm1" -DisableNameChecking

function Get-TargetHostsMetadata ($Context) {

	switch ($Context.EnvType) {
		'Test' {
			return @{ 'TargetHosts' = @('uk-erm-test01') }
		}
		'Edu' {
			return @{ 'TargetHosts' = @('uk-erm-edu03') }
		}
		'Business' {
			return @{ 'TargetHosts' = @('uk-erm-edu03') }
		}
		'Production' {
			return @{
			'TargetHosts' = @('uk-erm-iis03', 'uk-erm-iis01', 'uk-erm-iis02', 'uk-erm-iis04')
				'HAProxyUris' = @('tcp://uk-erm-hap01:2000', 'tcp://uk-erm-hap02:2000')
				# интервал 5 минут
				'Attempts' = 30
				'Interval' = 10
			}
		}
		'Load' {
			return @{
				'TargetHosts' = @('uk-erm-iis12', 'uk-erm-iis11', 'uk-erm-iis10')
				'HAProxyUris' = @('tcp://uk-erm-hap10:2000', 'tcp://uk-erm-hap11:2000')
				# интервал 5 минут
				'Attempts' = 30
				'Interval' = 10
			}
		}
		'Appveyor' {
			return @{ 'TargetHosts' = @() }
		}
		default {
			throw "Unknown environment type '$($Context.EnvType)'"
		}
	}
}

function Get-ValidateWebsiteMetadata ($Context) {
	return @{ 'ValidateUriPath' = 'healthcheck' }
}

function Get-IisAppPathMetadata ($Context) {
	if($Context.EntryPoint -ne 'ValidationRules.Querying.Host') {
		return @{ }
	}

	$mainDomain = $DomainNames["Russia"]
	return @{ 'IisAppPath' = Get-WebsiteDomain $Context $mainDomain }
}

function Get-IisAppPoolMetadata ($Context) {

	switch ($Context.EnvType) {
		{ @('Production', 'Load') -contains $_ } {
			$appPoolName = "$($Context.EntryPoint) ($($Context.EnvironmentName))"
		}
		default {
			$appPoolName = "ERM ($($Context.EnvironmentName))"
		}
	}

	return @{ 'AppPool' = @{
			'Name' = $appPoolName
		}
	}
}

function Get-IisAppAliasesMetadata($Context) {
	$aliases = @()
	foreach ($country in ($DomainNames.Keys | where { $_ -ne "Russia" })) {
		$hostName = Get-WebsiteDomain $Context $DomainNames[$country]
		$aliases += @{'Protocol' = "http"; 'Binding' = "*:80:$hostName"}
		$aliases += @{'Protocol' = "https"; 'Binding' = "*:443:$hostName"}
	}
	return @{ 'Aliases' = $aliases }
}

function Get-WebsiteDomain($Context, $domain) {
	switch ($Context.EnvType) {
		'Production' {
			return "validation.api.prod.erm.2gis.$mainDomain"
		}
		default {
			$envIndex = $Context['Index']
			$envTypeLower = $Context.EnvType.ToLowerInvariant()
			return "validation$envIndex.api.$envTypeLower.erm.2gis.$domain"
		}
	}
}

function Get-IisMetadata ($Context) {
	if($Context["Country"] -ne "Russia") {
		throw "Web app deployment supported only with 'Russia' parameter"
	}

	$metadata = @{ }
	$metadata += Get-IisAppPathMetadata $Context
	$metadata += Get-IisAppPoolMetadata $Context
	$metadata += Get-IisAppAliasesMetadata $Context

	return @{ 'IIS' = $metadata }
}

function Get-WebMetadata ($Context) {

	$metadata = @{ }
	$metadata += Get-ValidateWebsiteMetadata $Context
	$metadata += Get-TargetHostsMetadata $Context
	$metadata += Get-IisMetadata $Context

	$metadata += Get-TransformMetadata $Context

	return @{ "$($Context.EntryPoint)" = $metadata }
}

Export-ModuleMember -Function Get-WebMetadata
