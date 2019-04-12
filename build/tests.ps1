Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\unittests.psm1" -DisableNameChecking

Task Run-UnitTestsExplicit {

	# default 'Run-UnitTests' will include data-compare-test project
	# manually exclude data-compare-test project when calling just regular tests
	$projects = @(
		Get-Item (Get-ProjectFileName 'test' 'ValidationRules.Replication.StateInitialization.Tests')
		Get-Item (Get-ProjectFileName 'test' 'ValidationRules.Replication.Tests')
	)

	Run-UnitTests $projects
}

Task Run-DatabaseComparisonTests {
	$project = Find-Projects 'test' -Filter 'ValidationRules.Replication.DatabaseComparison.Tests.csproj'
	Run-UnitTests $project
}