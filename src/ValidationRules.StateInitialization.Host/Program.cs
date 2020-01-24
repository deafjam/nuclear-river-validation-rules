using Confluent.Kafka;
using NuClear.Assembling.TypeProcessing;
using NuClear.Messaging.API.Flows;
using NuClear.Replication.Core;
using NuClear.River.Hosting.Common.Settings;
using NuClear.StateInitialization.Core.Actors;
using NuClear.Tracing.API;
using NuClear.Tracing.Environment;
using NuClear.Tracing.Log4Net.Config;
using NuClear.ValidationRules.Hosting.Common;
using NuClear.ValidationRules.Hosting.Common.Identities.Connections;
using NuClear.ValidationRules.Hosting.Common.Settings;
using NuClear.ValidationRules.Hosting.Common.Settings.Kafka;
using NuClear.ValidationRules.OperationsProcessing.Facts.AmsFactsFlow;
using NuClear.ValidationRules.OperationsProcessing.Facts.RulesetFactsFlow;
using NuClear.ValidationRules.StateInitialization.Host.Assembling;
using NuClear.ValidationRules.StateInitialization.Host.Kafka;
using NuClear.ValidationRules.StateInitialization.Host.Kafka.Ams;
using NuClear.ValidationRules.StateInitialization.Host.Kafka.Rulesets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NuClear.ValidationRules.StateInitialization.Host
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            StateInitializationRoot.Instance.PerformTypesMassProcessing(Array.Empty<IMassProcessor>(), true, typeof(object));

            var commands = new List<ICommand>();

            if (args.Any(x => x.Contains("-facts")))
            {
                commands.AddRange(BulkReplicationCommands.ErmToFacts);
                commands.Add(new KafkaReplicationCommand(AmsFactsFlow.Instance, BulkReplicationCommands.AmsToFacts));
                commands.Add(new KafkaReplicationCommand(RulesetFactsFlow.Instance, BulkReplicationCommands.RulesetsToFacts, 500));
                // TODO: отдельный schema init для erm\ams\ruleset facts
                commands.Add(SchemaInitializationCommands.Facts);
            }

            if (args.Contains("-aggregates"))
            {
                commands.Add(BulkReplicationCommands.FactsToAggregates);
                commands.Add(SchemaInitializationCommands.Aggregates);
            }

            if (args.Contains("-messages"))
            {
                commands.AddRange(BulkReplicationCommands.ErmToMessages);
                commands.Add(BulkReplicationCommands.AggregatesToMessages);
                commands.Add(SchemaInitializationCommands.Messages);
            }

            var connectionStringSettings = new TenantConnectionStringSettings();
            var environmentSettings = new EnvironmentSettingsAspect();
            var businessModelSettings = new BusinessModelSettingsAspect();

            var tracer = CreateTracer(environmentSettings);

            var kafkaSettingsFactory = new KafkaSettingsFactory(new Dictionary<IMessageFlow, string>
                {
                    {AmsFactsFlow.Instance, connectionStringSettings.GetConnectionString(AmsConnectionStringIdentity.Instance)},
                    {RulesetFactsFlow.Instance, connectionStringSettings.GetConnectionString(RulesetConnectionStringIdentity.Instance)}
                },
                environmentSettings);

            var kafkaMessageFlowReceiverFactory = new StateInitKafkaMessageFlowReceiverFactory(new NullTracer(), kafkaSettingsFactory);

            var bulkReplicationActor = new BulkReplicationActor(connectionStringSettings);
            var kafkaReplicationActor = new KafkaReplicationActor(connectionStringSettings,
                kafkaMessageFlowReceiverFactory,
                new KafkaMessageFlowInfoProvider(kafkaSettingsFactory),
                new IBulkCommandFactory<ConsumeResult<Ignore, byte[]>>[]
                {
                    new AmsFactsBulkCommandFactory(),
                    new RulesetFactsBulkCommandFactory(businessModelSettings)
                },
                tracer);

            var schemaInitializationActor = new SchemaInitializationActor(connectionStringSettings);

            var sw = Stopwatch.StartNew();
            schemaInitializationActor.ExecuteCommands(commands);
            bulkReplicationActor.ExecuteCommands(commands.Where(x => BulkReplicationCommands.ErmToFacts.Contains(x)).ToList());
            kafkaReplicationActor.ExecuteCommands(commands);
            bulkReplicationActor.ExecuteCommands(commands.Where(x => !BulkReplicationCommands.ErmToFacts.Contains(x)).ToList());

            var webAppSchemaHelper = new WebAppSchemaInitializationHelper(connectionStringSettings);
            if (args.Contains("-webapp"))
            {
                webAppSchemaHelper.CreateWebAppSchema(SchemaInitializationCommands.WebApp);
            }

            if (args.Contains("-webapp-drop"))
            {
                webAppSchemaHelper.DropWebAppSchema(SchemaInitializationCommands.WebApp);
            }

            Console.WriteLine($"Total time: {sw.ElapsedMilliseconds}ms");
        }

        private static ITracer CreateTracer(IEnvironmentSettings environmentSettings)
        {
            return Log4NetTracerBuilder.Use
                .ApplicationXmlConfig
                .Console
                .WithGlobalProperties(x =>
                    x.Property(TracerContextKeys.Tenant, environmentSettings.EnvironmentName)
                        .Property(TracerContextKeys.EntryPoint, environmentSettings.EntryPointName)
                        .Property(TracerContextKeys.EntryPointHost, NetworkInfo.ComputerFQDN)
                        .Property(TracerContextKeys.EntryPointInstanceId, Guid.NewGuid().ToString()))
                .Build;
        }
    }
}