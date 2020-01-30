Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

$topicProperties = @{
	'Properties' = @{
		'EnableBatchedOperations' = $true
		'SupportOrdering' = $true
		'RequiresDuplicateDetection' = $true
	}
}

$subscriptionProperties = @{
	'Properties' = @{
		'EnableBatchedOperations' = $true
		'MaxDeliveryCount' = 0x7fffffff
		'LockDuration' = New-TimeSpan -Minutes 5
	}
}

function Get-ServiceBusMetadata ($Context) {

	$metadata = @{}
	$metadata += Get-TopicsMetadata $Context

	return @{ 'ServiceBus' = $metadata}
}

function Get-TopicsMetadata ($Context) {

	$metadata = @{}

	switch ($Context.EntryPoint) {

		'ValidationRules.Replication.Host' {

			$metadata = @{
				'CreateTopics' = @{

					'ErmEventsFlowTopic' = @{
						'Name' = 'topic.performedoperations'
						'ConnectionStringName' = 'ServiceBus'
					} + $topicProperties

				}

				'CreateSubscriptions' = @{

					'ErmEventsFlowSubscription' = @{
						'TopicName' = 'topic.performedoperations'
						'Name' = '6A75B8B4-74A6-4523-9388-84E4DFFD5B06'
						'ConnectionStringName' = 'ServiceBus'
					} + $subscriptionProperties

				}

				'DeleteTopics' = @{}
				'DeleteSubscriptions' = @{}
			}
		}
	}

	return $metadata
}

Export-ModuleMember -Function Get-ServiceBusMetadata
