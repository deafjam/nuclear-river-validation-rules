﻿using System;

using NuClear.Messaging.API.Flows;
using NuClear.Messaging.Transports.ServiceBus.API;
using NuClear.Replication.OperationsProcessing.Transports.ServiceBus.Factories;
using NuClear.River.Hosting.Common.Identities.Connections;
using NuClear.Settings;
using NuClear.Settings.API;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.ValidationRules.OperationsProcessing.AggregatesFlow;
using NuClear.ValidationRules.OperationsProcessing.Facts.Erm;
using NuClear.ValidationRules.OperationsProcessing.MessagesFlow;

namespace NuClear.ValidationRules.Replication.Host.Factories
{
    public sealed class ServiceBusSettingsFactory : IServiceBusSettingsFactory
    {
        private readonly string _serviceBusConnectionString;

        private readonly StringSetting _ermFactsTopic = ConfigFileSetting.String.Required("ErmFactsTopic");
        private readonly StringSetting _aggregatesTopic = ConfigFileSetting.String.Optional("AggregatesTopic", "topic.river.validationrules.common");
        private readonly StringSetting _messagesTopic = ConfigFileSetting.String.Optional("MessagesTopic", "topic.river.validationrules.messages");

        public ServiceBusSettingsFactory(IConnectionStringSettings connectionStringSettings)
        {
            _serviceBusConnectionString = connectionStringSettings.GetConnectionString(ServiceBusConnectionStringIdentity.Instance);
        }

        public IServiceBusMessageReceiverSettings CreateReceiverSettings(IMessageFlow messageFlow)
        {
            if (ErmFactsFlow.Instance.Equals(messageFlow))
                return new Settings
                {
                    ConnectionString = _serviceBusConnectionString,
                    TransportEntityPath = _ermFactsTopic.Value,
                };

            if (AggregatesFlow.Instance.Equals(messageFlow))
                return new Settings
                {
                    ConnectionString = _serviceBusConnectionString,
                    TransportEntityPath = _aggregatesTopic.Value,
                };

            if (MessagesFlow.Instance.Equals(messageFlow))
                return new Settings
                {
                    ConnectionString = _serviceBusConnectionString,
                    TransportEntityPath = _messagesTopic.Value,
                };

            throw new ArgumentException($"Flow '{messageFlow.Description}' settings for MS ServiceBus are undefined");
        }

        public IServiceBusMessageSenderSettings CreateSenderSettings(IMessageFlow messageFlow)
        {
            if (AggregatesFlow.Instance.Equals(messageFlow))
                return new Settings
                {
                    ConnectionString = _serviceBusConnectionString,
                    TransportEntityPath = _aggregatesTopic.Value,
                };

            if (MessagesFlow.Instance.Equals(messageFlow))
                return new Settings
                {
                    ConnectionString = _serviceBusConnectionString,
                    TransportEntityPath = _messagesTopic.Value,
                };

            throw new ArgumentException($"Flow '{messageFlow.Description}' settings for MS ServiceBus are undefined");
        }

        private class Settings : IServiceBusMessageReceiverSettings, IServiceBusMessageSenderSettings
        {
            public string TransportEntityPath { get; set; }
            public string ConnectionString { get; set; }
            public int ConnectionsCount { get; } = 1;
            public bool UseTransactions { get; } = true;
        }
    }
}