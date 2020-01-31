﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

using Jobs.RemoteControl.PortResolver;
using Jobs.RemoteControl.Provider;
using Jobs.RemoteControl.Registrar;
using Jobs.RemoteControl.Settings;

using LinqToDB.Mapping;

using Microsoft.Practices.Unity;

using NuClear.Aggregates.Storage.DI.Unity;
using NuClear.Assembling.TypeProcessing;
using NuClear.ValidationRules.OperationsProcessing;
using NuClear.ValidationRules.OperationsProcessing.Contexts;
using NuClear.ValidationRules.OperationsProcessing.Transports;
using NuClear.ValidationRules.Replication.Host.Factories;
using NuClear.ValidationRules.Replication.Host.Factories.Messaging.Processor;
using NuClear.ValidationRules.Replication.Host.Factories.Messaging.Receiver;
using NuClear.ValidationRules.Replication.Host.Factories.Messaging.Transformer;
using NuClear.ValidationRules.Replication.Host.Factories.Replication;
using NuClear.ValidationRules.Replication.Host.Settings;
using NuClear.ValidationRules.Storage;
using NuClear.DI.Unity.Config;
using NuClear.DI.Unity.Config.RegistrationResolvers;
using NuClear.Jobs;
using NuClear.Jobs.Schedulers;
using NuClear.Jobs.Schedulers.Exporter;
using NuClear.Jobs.Unity;
using NuClear.Messaging.API.Flows;
using NuClear.Messaging.API.Processing.Actors.Accumulators;
using NuClear.Messaging.API.Processing.Actors.Handlers;
using NuClear.Messaging.API.Processing.Actors.Transformers;
using NuClear.Messaging.API.Processing.Actors.Validators;
using NuClear.Messaging.API.Processing.Audit;
using NuClear.Messaging.API.Processing.Processors;
using NuClear.Messaging.API.Processing.Stages;
using NuClear.Messaging.API.Receivers;
using NuClear.Messaging.DI.Factories.Unity.Accumulators;
using NuClear.Messaging.DI.Factories.Unity.Common;
using NuClear.Messaging.DI.Factories.Unity.Handlers;
using NuClear.Messaging.DI.Factories.Unity.Processors;
using NuClear.Messaging.DI.Factories.Unity.Processors.Resolvers;
using NuClear.Messaging.DI.Factories.Unity.Receivers;
using NuClear.Messaging.DI.Factories.Unity.Receivers.Resolvers;
using NuClear.Messaging.DI.Factories.Unity.Stages;
using NuClear.Messaging.DI.Factories.Unity.Transformers;
using NuClear.Messaging.DI.Factories.Unity.Transformers.Resolvers;
using NuClear.Messaging.DI.Factories.Unity.Validators;
using NuClear.Messaging.Transports.Kafka;
using NuClear.Messaging.Transports.ServiceBus;
using NuClear.Messaging.Transports.ServiceBus.API;
using NuClear.Messaging.Transports.ServiceBus.LockRenewer;
using NuClear.Metamodeling.Domain.Processors.Concrete;
using NuClear.Metamodeling.Processors;
using NuClear.Metamodeling.Provider;
using NuClear.Metamodeling.Provider.Sources;
using NuClear.Metamodeling.Validators;
using NuClear.Model.Common.Entities;
using NuClear.Model.Common.Operations.Identity;
using NuClear.OperationsLogging;
using NuClear.OperationsLogging.API;
using NuClear.OperationsLogging.Transports.ServiceBus;
using NuClear.OperationsLogging.Transports.ServiceBus.Serialization.ProtoBuf;
using NuClear.OperationsProcessing.Transports.Kafka;
using NuClear.OperationsProcessing.Transports.ServiceBus.Primary;
using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Replication.Core.Settings;
using NuClear.Replication.OperationsProcessing.Metadata;
using NuClear.Replication.OperationsProcessing.Transports;
using NuClear.Replication.OperationsProcessing.Transports.ServiceBus;
using NuClear.Replication.OperationsProcessing.Transports.ServiceBus.Factories;
using NuClear.River.Hosting.Common.Identities.Connections;
using NuClear.River.Hosting.Common.Settings;
using NuClear.Security;
using NuClear.Security.API.Auth;
using NuClear.Security.API.Context;
using NuClear.Settings.API;
using NuClear.Settings.Unity;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Writings;
using NuClear.Storage.Core;
using NuClear.Storage.LinqToDB;
using NuClear.Storage.LinqToDB.Connections;
using NuClear.Storage.LinqToDB.Writings;
using NuClear.Storage.Readings;
using NuClear.Telemetry;
using NuClear.Tracing.API;
using NuClear.ValidationRules.Hosting.Common;
using NuClear.ValidationRules.Hosting.Common.Identities.Connections;
using NuClear.ValidationRules.Hosting.Common.Settings.Kafka;
using NuClear.ValidationRules.OperationsProcessing.AggregatesFlow;
using NuClear.ValidationRules.OperationsProcessing.Facts.Ams;
using NuClear.ValidationRules.OperationsProcessing.Facts.Ruleset;
using NuClear.ValidationRules.Storage.Model.Facts;
using NuClear.ValidationRules.Replication.Accessors;
using NuClear.ValidationRules.Replication.Accessors.Rulesets;
using NuClear.ValidationRules.Replication.Host.Customs;
using NuClear.ValidationRules.Replication.Host.Jobs;
using NuClear.ValidationRules.Storage.FieldComparer;

using Quartz.Spi;
using Schema = NuClear.ValidationRules.Storage.Schema;
using Version = NuClear.ValidationRules.Storage.Model.Messages.Version;

namespace NuClear.ValidationRules.Replication.Host.DI
{
    public static class Bootstrapper
    {
        public static IUnityContainer ConfigureUnity(ISettingsContainer settingsContainer, ITracer tracer)
        {
            IUnityContainer container = new UnityContainer();
            var massProcessors = new IMassProcessor[]
                                 {
                                     new TaskServiceJobsMassProcessor(container),
                                 };
            var storageSettings = settingsContainer.AsSettings<ISqlStoreSettingsAspect>();

            var connectionStringSettings = settingsContainer.AsSettings<IConnectionStringSettings>();

            container.AttachQueryableContainerExtension()
                     .UseParameterResolvers(ParameterResolvers.Defaults)
                     .ConfigureMetadata()
                     .ConfigureSettingsAspects(settingsContainer)
                     .ConfigureTracing(tracer)
                     .ConfigureSecurityAspects()
                     .ConfigureQuartz()
                     .ConfigureOperationsProcessing(connectionStringSettings)
                     .ConfigureStorage(storageSettings, EntryPointSpecificLifetimeManagerFactory)
                     .ConfigureReplication(EntryPointSpecificLifetimeManagerFactory);

            ReplicationRoot.Instance.PerformTypesMassProcessing(massProcessors, true, typeof(object));

            LinqToDB.Common.Configuration.Linq.OptimizeJoins = false;

            return container;
        }

        private static LifetimeManager EntryPointSpecificLifetimeManagerFactory()
        {
            return Lifetime.PerScope;
        }

        private static IUnityContainer ConfigureTracing(this IUnityContainer container, ITracer tracer)
        {
            return container.RegisterInstance(tracer);
        }

        private static IUnityContainer ConfigureMetadata(this IUnityContainer container)
        {
            // provider
            container.RegisterType<IMetadataProvider, MetadataProvider>(Lifetime.Singleton);

            // processors
            container.RegisterOne2ManyTypesPerTypeUniqueness<IMetadataProcessor, TunedReferencesEvaluatorProcessor>(Lifetime.Singleton);
            container.RegisterOne2ManyTypesPerTypeUniqueness<IMetadataProcessor, Feature2PropertiesLinkerProcessor>(Lifetime.Singleton);

            // validators
            container.RegisterType<IMetadataValidatorsSuite, MetadataValidatorsSuite>(Lifetime.Singleton);

            // register matadata sources without massprocessor
            container.RegisterOne2ManyTypesPerTypeUniqueness(typeof(IMetadataSource), typeof(FlowMetadataSource), Lifetime.Singleton);

            return container;
        }

        private static IUnityContainer ConfigureSecurityAspects(this IUnityContainer container)
        {
            return container
                .RegisterType<IUserAuthorizationService, Security.UserAuthorizationService>(Lifetime.PerScope)
                .RegisterType<IUserContextManager, UserContextManager>(Lifetime.PerScope)
                .RegisterInstance<IUserAuthenticationService>(new UserAuthenticationService(new[] { new WindowsIdentityExtractor() }))
                .RegisterType<IUserContextScopeChangesObserver, Security.TracerContextUserContextScopeChangesObserver>(Lifetime.Singleton);
        }

        private static IUnityContainer ConfigureQuartz(
            this IUnityContainer container)
        {
            return container.ConfigureQuartzRemoteControl()
                .RegisterType<IJobFactory, JobFactory>(Lifetime.Singleton, new InjectionConstructor(container.Resolve<UnityJobFactory>(), container.Resolve<ITracer>()))
                .RegisterType<IJobStoreFactory, JobStoreFactory>(Lifetime.Singleton)
                .RegisterType<ISchedulerManager, SchedulerManager>("default", Lifetime.Singleton)
                .RegisterType<ISchedulerManager, KafkaFactsImportShedulerManager>("kafka", Lifetime.Singleton)
                .RegisterType<IJobExecutionObserver, VrTracingJobExecutionObserver>(Lifetime.Singleton);
        }

        private static IUnityContainer ConfigureQuartzRemoteControl(
            this IUnityContainer container)
        {
            var environmentSettings = container.Resolve<IEnvironmentSettings>();
            var connectionStringSettings = container.Resolve<IConnectionStringSettings>();
            var taskServiceRemoteControlSettings = container.Resolve<ITaskServiceRemoteControlSettings>();

            if (taskServiceRemoteControlSettings.RemoteControlEnabled)
            {
                connectionStringSettings.AllConnectionStrings.TryGetValue(InfrastructureConnectionStringIdentity.Instance, out var quartzStoreConnectionString);

                container.RegisterType<ISchedulerExporterProvider, SchedulerExporterProvider>(Lifetime.Singleton,
                             new InjectionConstructor(environmentSettings.EnvironmentName,
                                 typeof(ITaskServiceRemoteControlSettings),
                                 typeof(ISchedulerExportPortResolver),
                                 typeof(ISchedulerExporterRegistrar),
                                 typeof(ITracer)))
                         .RegisterType<ISchedulerExportPortResolver, SchedulerExportPortResolver>(Lifetime.Singleton)
                         .RegisterType<ISchedulerExporterRegistrar, MsSQLPersistenceSchedulerExporterRegistrar>(Lifetime.Singleton,
                             new InjectionConstructor(quartzStoreConnectionString));
            }
            else
            {
                container.RegisterType<ISchedulerExporterProvider, AlwaysOffSchedulerExporterProvider>(Lifetime.Singleton);
            }

            return container;
        }

        private static IUnityContainer ConfigureOperationsProcessing(
            this IUnityContainer container,
            IConnectionStringSettings connectionStringSettings)
        {

#if DEBUG
            container.RegisterType<ITelemetryPublisher, DebugTelemetryPublisher>(Lifetime.Singleton);
#else
            container.RegisterType<ITelemetryPublisher, CachingTelemetryPublisherDecorator<LogstashTelemetryPublisher>>(Lifetime.Singleton);
#endif

            // primary
            container.RegisterInstance(new EntityTypeMappingRegistryBuilder().Create<ErmSubDomain>(), Lifetime.Singleton)
                     .RegisterType<IOperationIdentityRegistry, EmptyOperationIdentityRegistry>(Lifetime.Singleton)
                     .RegisterTypeWithDependencies(typeof(ServiceBusMessageReceiverTelemetryDecorator), Lifetime.PerScope, null)
                     .RegisterOne2ManyTypesPerTypeUniqueness<IRuntimeTypeModelConfigurator, ProtoBufTypeModelForTrackedUseCaseConfigurator<ErmSubDomain>>(Lifetime.Singleton)
                     .RegisterTypeWithDependencies(typeof(BinaryEntireBrokeredMessage2TrackedUseCaseTransformer), Lifetime.Singleton, null)
                     .RegisterType<IXmlEventSerializer, XmlEventSerializer>();

            // final
            container.RegisterTypeWithDependencies(typeof(AggregatesFlowHandler), Lifetime.PerResolve, null);

            container.RegisterType<IEventLoggingStrategyProvider, UnityEventLoggingStrategyProvider>()
                     .RegisterType<IEvent2BrokeredMessageConverter<IEvent>, Event2BrokeredMessageConverter>()
                     .RegisterType<IEventLogger, SequentialEventLogger>()
                     .RegisterType<IServiceBusMessageSender, BatchingServiceBusMessageSender>();

            var kafkaSettingsFactory = new KafkaSettingsFactory(connectionStringSettings);

            // kafka receiver
            container
                .RegisterType<BatchingKafkaReceiverTelemetryDecorator<AmsFactsFlowTelemetryPublisher>>(new InjectionConstructor(new ResolvedParameter<KafkaReceiver>(nameof(AmsFactsFlow)),
                                                                                                                                typeof(AmsFactsFlowTelemetryPublisher)))
                .RegisterType<BatchingKafkaReceiverTelemetryDecorator<RulesetFactsFlowTelemetryPublisher>>(new InjectionConstructor(new ResolvedParameter<KafkaReceiver>(nameof(RulesetFactsFlow)),
                                                                                                                                typeof(RulesetFactsFlowTelemetryPublisher)))

                .RegisterType<KafkaReceiver>(nameof(AmsFactsFlow), Lifetime.Singleton)
                .RegisterType<KafkaReceiver>(nameof(RulesetFactsFlow), Lifetime.Singleton)
                .RegisterType<IKafkaMessageFlowReceiverFactory, KafkaMessageFlowReceiverFactory>(Lifetime.Singleton)
                .RegisterInstance<IKafkaSettingsFactory>(kafkaSettingsFactory)
                .RegisterType<KafkaMessageFlowInfoProvider>(Lifetime.Singleton);

            return container.RegisterInstance<IParentContainerUsedRegistrationsContainer>(new ParentContainerUsedRegistrationsContainer(), Lifetime.Singleton)
                            .RegisterType(typeof(ServiceBusMessageFlowReceiver), Lifetime.Singleton)
                            .RegisterType<IServiceBusLockRenewer, NullServiceBusLockRenewer>(Lifetime.Singleton)
                            .RegisterType<IServiceBusSettingsFactory, ServiceBusSettingsFactory>(Lifetime.Singleton)
                            .RegisterType<IServiceBusMessageFlowReceiverFactory, ServiceBusMessageFlowReceiverFactory>(Lifetime.PerScope)
                            .RegisterType<IMessageProcessingStagesFactory, UnityMessageProcessingStagesFactory>(Lifetime.PerScope)
                            .RegisterType<IMessageFlowProcessorFactory, UnityMessageFlowProcessorFactory>(Lifetime.PerScope)
                            .RegisterType<IMessageReceiverFactory, UnityMessageReceiverFactory>(Lifetime.PerScope)

                            .RegisterOne2ManyTypesPerTypeUniqueness<IMessageFlowProcessorResolveStrategy, PrimaryProcessorResolveStrategy>(Lifetime.Singleton)
                            .RegisterOne2ManyTypesPerTypeUniqueness<IMessageFlowProcessorResolveStrategy, FinalProcessorResolveStrategy>(Lifetime.PerScope)

                            .RegisterOne2ManyTypesPerTypeUniqueness<IMessageFlowReceiverResolveStrategy, MessageFlowReceiverResolveStrategy>(Lifetime.PerScope)

                            .RegisterType<IMessageValidatorFactory, UnityMessageValidatorFactory>(Lifetime.PerScope)
                            .RegisterType<IMessageTransformerFactory, UnityMessageTransformerFactory>(Lifetime.PerScope)

                            .RegisterOne2ManyTypesPerTypeUniqueness<IMessageTransformerResolveStrategy, PrimaryMessageTransformerResolveStrategy>(Lifetime.PerScope)
                            .RegisterType<IMessageProcessingHandlerFactory, UnityMessageProcessingHandlerFactory>(Lifetime.PerScope)
                            .RegisterType<IMessageProcessingContextAccumulatorFactory, UnityMessageProcessingContextAccumulatorFactory>(Lifetime.PerScope)

                            .RegisterType<IMessageFlowProcessingObserver, NullMessageFlowProcessingObserver>(Lifetime.Singleton);
        }

        private static IUnityContainer ConfigureStorage(this IUnityContainer container, ISqlStoreSettingsAspect storageSettings, Func<LifetimeManager> entryPointSpecificLifetimeManagerFactory)
        {
            // разрешаем update на таблицу состоящую только из Primary Keys
            LinqToDB.Common.Configuration.Linq.IgnoreEmptyUpdate = true;

            var transactionOptions = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.Zero };

            var schemaMapping = new Dictionary<string, MappingSchema>
                                {
                                    { Scope.Erm, Schema.Erm },
                                    { Scope.Facts, Schema.Facts },
                                    { Scope.Aggregates, Schema.Aggregates },
                                    { Scope.Messages, Schema.Messages },
                                };

            return container
                .RegisterType<IPendingChangesHandlingStrategy, NullPendingChangesHandlingStrategy>(Lifetime.Singleton)
                .RegisterType<IStorageMappingDescriptorProvider, StorageMappingDescriptorProvider>(Lifetime.Singleton)
                .RegisterType<IEntityContainerNameResolver, DefaultEntityContainerNameResolver>(Lifetime.Singleton)
                .RegisterType<IManagedConnectionStateScopeFactory, ManagedConnectionStateScopeFactory>(Lifetime.Singleton)
                .RegisterType<IDomainContextScope, DomainContextScope>(entryPointSpecificLifetimeManagerFactory())
                .RegisterType<ScopedDomainContextsStore>(entryPointSpecificLifetimeManagerFactory())
                .RegisterType<IReadableDomainContext, CachingReadableDomainContext>(entryPointSpecificLifetimeManagerFactory())
                .RegisterInstance<ILinqToDbModelFactory>(
                    new LinqToDbModelFactory(schemaMapping, transactionOptions, storageSettings.SqlCommandTimeout), Lifetime.Singleton)
                .RegisterInstance<IEqualityComparerFactory>(
                    new EqualityComparerFactory(new LinqToDbPropertyProvider(schemaMapping.Values.ToArray()), new DateTimeComparer(), new XDocumentComparer()), Lifetime.Singleton)
                .RegisterType<IWritingStrategyFactory, WritingStrategyFactory>()
                .RegisterType<IReadableDomainContextFactory, LinqToDBDomainContextFactory>(entryPointSpecificLifetimeManagerFactory())
                .RegisterType<IModifiableDomainContextFactory, LinqToDBDomainContextFactory>(entryPointSpecificLifetimeManagerFactory())
                .RegisterType<IQuery, Query>(entryPointSpecificLifetimeManagerFactory())
                .RegisterType(typeof(IRepository<>), typeof(LinqToDBRepository<>), entryPointSpecificLifetimeManagerFactory())
                .RegisterType<IReadableDomainContextProvider, ReadableDomainContextProvider>(entryPointSpecificLifetimeManagerFactory())
                .RegisterType<IModifiableDomainContextProvider, ModifiableDomainContextProvider>(entryPointSpecificLifetimeManagerFactory())
                .RegisterType(typeof(IBulkRepository<>), typeof(BulkRepository<>), entryPointSpecificLifetimeManagerFactory())
                .ConfigureReadWriteModels();
        }

        private static IUnityContainer ConfigureReplication(this IUnityContainer container, Func<LifetimeManager> entryPointSpecificLifetimeManagerFactory)
        {
            return container
                   .RegisterAccessor<Account, AccountAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<AccountDetail, AccountDetailAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Bargain, BargainAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<BargainScanFile, BargainScanFileAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Bill, BillAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<BranchOffice, BranchOfficeAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<BranchOfficeOrganizationUnit, BranchOfficeOrganizationUnitAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Category, CategoryAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<CategoryOrganizationUnit, CategoryOrganizationUnitAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<CostPerClickCategoryRestriction, CostPerClickCategoryRestrictionAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Deal, DealAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Firm, FirmAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<FirmInactive, FirmInactiveAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<FirmAddress, FirmAddressAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<FirmAddressInactive, FirmAddressInactiveAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<FirmAddressCategory, FirmAddressCategoryAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<LegalPerson, LegalPersonAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<LegalPersonProfile, LegalPersonProfileAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<NomenclatureCategory, NomenclatureCategoryAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Order, OrderAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<OrderConsistency, OrderConsistencyAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<OrderWorkflow, OrderWorkflowAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<OrderItem, OrderItemAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<OrderPosition, OrderPositionAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<OrderPositionAdvertisement, OrderPositionAdvertisementAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<OrderPositionCostPerClick, OrderPositionCostPerClickAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<OrderScanFile, OrderScanFileAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Position, PositionAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<PositionChild, PositionChildAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Price, PriceAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<PricePosition, PricePositionAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Project, ProjectAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<ReleaseInfo, ReleaseInfoAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<ReleaseWithdrawal, ReleaseWithdrawalAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<SalesModelCategoryRestriction, SalesModelCategoryRestrictionAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<Theme, ThemeAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<ThemeCategory, ThemeCategoryAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<ThemeOrganizationUnit, ThemeOrganizationUnitAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterAccessor<UnlimitedOrder, UnlimitedOrderAccessor>(entryPointSpecificLifetimeManagerFactory)
                   
                   // TODO: все эти сущности обладают Id, поэтому вместо тупого delete all\insert all
                   // тут можно использовать интеллектуальный SyncDataObjectsActor, но проблема в том что нужен рефакторинг
                   // т.к. SyncDataObjectsActor не может работать с объектами, прилетевшими чисто по шине
                   .RegisterMemoryAccessor<Advertisement, AdvertisementAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterMemoryAccessor<Ruleset, RulesetAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterMemoryAccessor<Ruleset.AssociatedRule, RulesetAssociatedRuleAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterMemoryAccessor<Ruleset.DeniedRule, RulesetDeniedRuleAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterMemoryAccessor<Ruleset.QuantitativeRule, RulesetQuantitativeRuleAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterMemoryAccessor<Ruleset.RulesetProject, RulesetProjectAccessor>(entryPointSpecificLifetimeManagerFactory)
                   .RegisterMemoryAccessor<Version.ErmState, Messages.ErmStateAccessor>(entryPointSpecificLifetimeManagerFactory)

                   .RegisterType<IDataObjectsActorFactory, UnityDataObjectsActorFactory>(entryPointSpecificLifetimeManagerFactory())
                   .RegisterType<IAggregateActorFactory, UnityAggregateActorFactory>(entryPointSpecificLifetimeManagerFactory());
        }

        private static IUnityContainer RegisterAccessor<TFact, TAccessor>(this IUnityContainer container, Func<LifetimeManager> entryPointSpecificLifetimeManagerFactory)
            where TAccessor : IStorageBasedDataObjectAccessor<TFact>, IDataChangesHandler<TFact>
            => container
                .RegisterType<IStorageBasedDataObjectAccessor<TFact>, TAccessor>(entryPointSpecificLifetimeManagerFactory())
                .RegisterType<IDataChangesHandler<TFact>, TAccessor>(entryPointSpecificLifetimeManagerFactory());

        private static IUnityContainer RegisterMemoryAccessor<TFact, TAccessor>(this IUnityContainer container, Func<LifetimeManager> entryPointSpecificLifetimeManagerFactory)
            where TAccessor : IMemoryBasedDataObjectAccessor<TFact>, IDataChangesHandler<TFact>
            => container
                .RegisterType<IMemoryBasedDataObjectAccessor<TFact>, TAccessor>(entryPointSpecificLifetimeManagerFactory())
                .RegisterType<IDataChangesHandler<TFact>, TAccessor>(entryPointSpecificLifetimeManagerFactory());

        private static IUnityContainer ConfigureReadWriteModels(this IUnityContainer container)
        {
            var readConnectionStringNameMap = new Dictionary<string, IConnectionStringIdentity>
                {
                    { Scope.Erm, ErmConnectionStringIdentity.Instance },
                    { Scope.Facts, ValidationRulesConnectionStringIdentity.Instance },
                    { Scope.Aggregates, ValidationRulesConnectionStringIdentity.Instance },
                    { Scope.Messages, ValidationRulesConnectionStringIdentity.Instance },
                };

            var writeConnectionStringNameMap = new Dictionary<string, IConnectionStringIdentity>
                {
                    { Scope.Facts, ValidationRulesConnectionStringIdentity.Instance },
                    { Scope.Aggregates, ValidationRulesConnectionStringIdentity.Instance },
                    { Scope.Messages, ValidationRulesConnectionStringIdentity.Instance },
                };

            return container.RegisterInstance<IConnectionStringIdentityResolver>(new ConnectionStringIdentityResolver(readConnectionStringNameMap, writeConnectionStringNameMap));
        }

        private static class Scope
        {
            public const string Erm = "Erm";
            public const string Facts = "Facts";
            public const string Aggregates = "Aggregates";
            public const string Messages = "Messages";
        }

        private sealed class VrTracingJobExecutionObserver : TracingJobExecutionObserver
        {
            public VrTracingJobExecutionObserver(ITracer tracer) : base(tracer)
            {
            }
        }
    }
}