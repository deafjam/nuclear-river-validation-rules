Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$PSScriptRoot\metadata.servicebus.psm1" -DisableNameChecking

$DomainNames = @{
	'Cyprus' = 'com.cy'
	'Czech' = 'cz'
	'Emirates' = 'ae'
	'Russia' = 'ru'
	'Ukraine' = 'ua'
	'Kazakhstan' = 'kz'
	'Kyrgyzstan' = 'kg'
	'Uzbekistan' = 'uz'
	'Azerbaijan' = 'az'
}

$DBSuffixes = @{
	'Cyprus' = 'CY'
	'Czech' = 'CZ'
	'Emirates' = 'AE'
	'Russia' = 'RU'
	'Ukraine' = 'UA'
	'Kazakhstan' = 'KZ'
	'Kyrgyzstan' = 'KG'
	'Uzbekistan' = 'UZ'
	'Azerbaijan' = 'AZ'
}

function Get-DBSuffix($Context){

	$countrySuffix = $DBSuffixes[$Context['Country']];

	switch($Context.EnvType){
		'Business' {
			$envTypeSuffix = $Context.EnvType
		}
		'Edu' {
			$envTypeSuffix = $Context.EnvType
		}
		default {
			$envTypeSuffix = $null
		}
	}

	return $envTypeSuffix + $countrySuffix + $Context['Index']
}

function Get-DBHostMetadata($Context){
	switch($Context.EnvType){
		'Test' {
			$dbHost = 'uk-erm-sql02'
		}
		'Business' {
			$dbHost = 'uk-erm-edu03'
		}
		'Edu' {
			$dbHost = 'uk-erm-edu03'
		}
		'Production' {
			$dbHost = 'uk-sql20\erm'
		}
		'Load' {
			$dbHost = 'uk-test-sql01\MSSQL2017'
		}
		'Appveyor' {
			$dbHost = '(local)\SQL2016'
		}
	}

	return @{ 'DBHost' = $dbHost }
}

function Get-KafkaMetadata($Context){
	
	$kafkaGroupId = "erm_vr_$($Context.EnvType.ToLowerInvariant())_$($Context.Country.ToLowerInvariant())"
	if ($Context['Index']){
		$kafkaGroupId += "_$($Context.Index)"
	}
	
	$metadata = @{
		'KafkaGroupId' = $kafkaGroupId 
	}
	
	switch($Context.EnvType){
		{$_ -in ('Test', 'Production', 'Load')} {
			$metadata += @{
				 'AmsFactsTopic' = 'ams_okapi_prod.am.validity'
				 'RulesetFactsTopic' = 'casino_staging_flowRulesets_compacted'
			 }
		}
		'Business' {
			$metadata += @{
				'AmsFactsTopic' = "ams_okapi_business$($Context['Index']).am.validity"
				'RulesetFactsTopic' = 'erm_business01_flowRulesets'
			}
		}
		'Edu' {
			$metadata += @{
				'AmsFactsTopic' = "ams_okapi_edu$($Context['Index']).am.validity"
				'RulesetFactsTopic' = 'casino_staging_flowRulesets_compacted'
			}
		}
		default {
			return @{}
		}
	}
	
	return $metadata; 
}

function Get-XdtMetadata($Context){
	$xdt = @('environments\Common\Erm.Release.config')

	switch($Context.EnvType){
		'Test' {
			$xdt += @("environments\Templates\Erm.Test.config")
		}
		'Production' {
			$xdt += @("environments\Erm.Production.config")
		}
		'Load' {
			$xdt += @("environments\Erm.Load.config")
		}
		default {
			$xdt += @("environments\Erm.config")
		}
	}

	return $xdt
}

function Get-RegexMetadata($Context){

	$regex = @{ '{EntryPoint}' = $Context['EntryPoint'] }

	if ($Context['Index']){
		$regex += @{ '{EnvNum}' = $Context['Index'] }
	}
	if ($Context['Country']){
		$regex += @{ '{Country}' = $Context['Country'] }
		$regex += @{ '{DBSuffix}' = (Get-DBSuffix $Context) }
	}
	if ($Context['EnvType']){
		$regex += @{ '{EnvType}' = $Context['EnvType'] }
	}

	$serviceBusMetadata = (Get-ServiceBusMetadata $Context)['ServiceBus']
	if ($serviceBusMetadata.Count -ne 0){
		if ($serviceBusMetadata['CreateTopics']){
			foreach($metadata in $serviceBusMetadata.CreateTopics.GetEnumerator()){
				$regex["{$($metadata.Key)}"] = $metadata.Value.Name
			}
		}

		if ($serviceBusMetadata['CreateSubscriptions']){
			foreach($metadata in $serviceBusMetadata.CreateSubscriptions.GetEnumerator()){
				$regex["{$($metadata.Key)}"] = $metadata.Value.Name
			}
		}
	}

	$keyValuePairs = @{}
	$keyValuePairs += Get-DBHostMetadata $Context
	$keyValuePairs += Get-KafkaMetadata $Context

	foreach($keyValuePair in $keyValuePairs.GetEnumerator()){
		$regex["{$($keyValuePair.Key)}"] = $keyValuePair.Value
	}

	return $regex
}

function Get-TransformMetadata ($Context) {

	return @{
		'Transform' = @{
			'Xdt' = Get-XdtMetadata $Context
			'Regex' = Get-RegexMetadata $Context
		}
	}
}

Export-ModuleMember -Function Get-TransformMetadata -Variable DomainNames