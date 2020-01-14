Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$PSScriptRoot\metadata.transform.psm1" -DisableNameChecking

function Get-TargetHostsMetadata ($Context) {

	switch ($Context.EnvType) {
		'Test' {
			switch ($Context.Country) {
				'Russia' {
					return @{ 'TargetHosts' = @('uk-erm-test01') }
				}
				default {
					return @{ 'TargetHosts' = @('uk-erm-test02') }
				}
			}
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

		switch ($Context.EntryPoint) {
			'ValidationRules.Querying.Host' { $prefix = "validation$($Context['Index']).api" }
			default {
				return @{ }
			}
		}

		$envTypeLower = $Context.EnvType.ToLowerInvariant()
		$domain = $DomainNames[$Context.Country]

		switch ($Context.EnvType) {
			'Production' {
				return @{ 'IisAppPath' = "$prefix.prod.erm.2gis.$domain" }
			}
			default {
				return @{ 'IisAppPath' = "$prefix.$envTypeLower.erm.2gis.$domain" }
			}
		}
	}

	function Get-IisAppPoolMetadata ($Context) {

		switch ($Context.EnvType) {
			{ @('Production', 'Load') -contains $_ } {
				switch ($Context.Country) {
					'Russia' {
						$appPoolName = "$($Context.EntryPoint) ($($Context.EnvironmentName))"
					}
					default {
						$appPoolName = "ERM ($($Context.EnvironmentName))"
					}
				}
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

	function Get-IisMetadata ($Context) {
		$metadata = @{ }
		$metadata += Get-IisAppPathMetadata $Context
		$metadata += Get-IisAppPoolMetadata $Context

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