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

					'ErmFactsTopic' = @{
						'Name' = 'topic.performedoperations'
						'ConnectionStringName' = 'ServiceBus'
					} + $topicProperties
					
					'AggregatesTopic' = @{
						'Name' = 'topic.river.validationrules.common'
						'ConnectionStringName' = 'ServiceBus'
					} + $topicProperties

					'MessagesTopic' = @{
						'Name' = 'topic.river.validationrules.messages'
						'ConnectionStringName' = 'ServiceBus'
					} + $topicProperties

				}

				'CreateSubscriptions' = @{
					
					'ErmFactsFlowSubscription' = @{
						'TopicName' = 'topic.performedoperations'
						'Name' = '6A75B8B4-74A6-4523-9388-84E4DFFD5B06'
						'ConnectionStringName' = 'ServiceBus'
					} + $subscriptionProperties

					'AggregatesFlowSubscription' = @{
						'TopicName' = 'topic.river.validationrules.common'
						'Name' = 'CB1434CA-D575-4470-8616-4F08D074C8DA'
						'ConnectionStringName' = 'ServiceBus'
					} + $subscriptionProperties

					'MessageFlowSubscription' = @{
						'TopicName' = 'topic.river.validationrules.messages'
						'Name' = '2B3D30F7-6E59-4510-B680-D7FDD9DEFE0F'
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