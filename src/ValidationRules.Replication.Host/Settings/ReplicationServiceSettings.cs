﻿using System.Collections.Specialized;
using System.Configuration;

using Jobs.RemoteControl.Settings;

using NuClear.OperationsLogging.Transports.ServiceBus;
using NuClear.Replication.Core.Settings;
using NuClear.River.Hosting.Common.Identities.Connections;
using NuClear.River.Hosting.Common.Settings;
using NuClear.Settings;
using NuClear.Settings.API;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.Telemetry.Logstash;
using NuClear.ValidationRules.Hosting.Common.Identities.Connections;
using NuClear.ValidationRules.Hosting.Common.Settings;
using NuClear.ValidationRules.Hosting.Common.Settings.Connections;
using Quartz.Impl;

namespace NuClear.ValidationRules.Replication.Host.Settings
{
    public sealed class ReplicationServiceSettings : SettingsContainerBase, IReplicationSettings, ISqlStoreSettingsAspect
    {
        private readonly IntSetting _replicationBatchSize = ConfigFileSetting.Int.Required("ReplicationBatchSize");
        private readonly IntSetting _sqlCommandTimeout = ConfigFileSetting.Int.Required("SqlCommandTimeout");

        public ReplicationServiceSettings()
        {
            var connectionStrings = ConnectionStrings.For(ErmConnectionStringIdentity.Instance,
                                                          KafkaConnectionStringIdentity.Instance,
                                                          ValidationRulesConnectionStringIdentity.Instance,
                                                          ServiceBusConnectionStringIdentity.Instance,
                                                          InfrastructureConnectionStringIdentity.Instance,
                                                          LoggingConnectionStringIdentity.Instance);
            var connectionStringSettings = new ConnectionStringSettingsAspect(connectionStrings);

            var quartzProperties = (NameValueCollection)ConfigurationManager.GetSection(StdSchedulerFactory.ConfigurationSectionName);

            Aspects.Use(connectionStringSettings)
                   .Use<BusinessModelSettingsAspect>()
                   .Use<ServiceBusMessageLockRenewalSettings>()
                   .Use<EnvironmentSettingsAspect>()
                   .Use(new QuartzSettingsAspect(connectionStringSettings.GetConnectionString(InfrastructureConnectionStringIdentity.Instance)))
                   .Use(new ServiceBusReceiverSettingsAspect(connectionStringSettings.GetConnectionString(ServiceBusConnectionStringIdentity.Instance)))
                   .Use<ArchiveVersionsSettings>()
                   .Use<LogstashSettingsAspect>()
                   .Use(new TaskServiceRemoteControlSettings(quartzProperties));
        }

        public int ReplicationBatchSize => _replicationBatchSize.Value;

        public int SqlCommandTimeout => _sqlCommandTimeout.Value;
    }
}