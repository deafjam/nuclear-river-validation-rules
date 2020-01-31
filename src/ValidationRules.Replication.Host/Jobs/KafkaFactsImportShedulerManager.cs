using NuClear.Jobs.Schedulers;
using Quartz;
using Quartz.Impl;
using Quartz.Plugin.Xml;
using Quartz.Simpl;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace NuClear.ValidationRules.Replication.Host.Jobs
{
    // Kafka сама занимается балансировкой, поэтому smart scheduler, опирающийся на базу данных, не подойдёт
    // Нужно чтобы на каждой ноде был на подхвате в горячем резерве, один (не более, т.к. будут мешать дрг другу)
    // экземпляр для импорта из каждого конкретного топика kafka, чтобы добиться этого:
    // * тупой RAM-based sheduler(чтобы все jobs на всех нодах постоянно работали без координации между нодами средствами quartz)
    // * совсем без quartz обходиться
    internal sealed class KafkaFactsImportShedulerManager : ISchedulerManager
    {
        private const string SchedulerName = "KafkaFactsSheduler";

        // хотел назвать quartz.kafka.config, но стандартный Scheduler
        // процессит все файлы quartz*.config, поэтому пришлось изголяться
        // чтобы этот файл не попал под массовый процессинг
        private const string ConfigName = "kafka.quartz*.config";

        private readonly IJobFactory _jobFactory;

        public KafkaFactsImportShedulerManager(IJobFactory jobFactory)
        {
            _jobFactory = jobFactory;
        }

        public void Start()
        {
            var configFileNames = string.Join(",", Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, ConfigName));
            if(string.IsNullOrWhiteSpace(configFileNames))
                return;

            var instanceId = new SimpleInstanceIdGenerator().GenerateInstanceId();

            var threadPool = new SimpleThreadPool(threadCount: 2, threadPriority: ThreadPriority.Normal)
                {
                    InstanceName = SchedulerName
                };
            threadPool.Initialize();

            var jobStore = new RAMJobStore
            {
                InstanceName = SchedulerName,
                InstanceId = instanceId,
            };


            var jobInitializationPlugin = new ConfigFileProcessorPlugin
            {
                FileNames = configFileNames,
                ScanInterval = QuartzConfigFileScanInterval.DisableScanning
            };

            DirectSchedulerFactory.Instance.CreateScheduler(
                SchedulerName,
                instanceId,
                threadPool,
                jobStore,
                new Dictionary<string, ISchedulerPlugin>
                {
                    { SchedulerName, jobInitializationPlugin }
                },
                TimeSpan.Zero,
                TimeSpan.Zero);

            var scheduler = DirectSchedulerFactory.Instance.GetScheduler(SchedulerName);
            scheduler.JobFactory = _jobFactory;

            scheduler.Start();
        }

        public void Stop()
        {
            var scheduler = DirectSchedulerFactory.Instance.GetScheduler(SchedulerName);
            scheduler?.Shutdown(true);
        }

        // copy\paste from 'jobs' repo
        private sealed class ConfigFileProcessorPlugin : XMLSchedulingDataProcessorPlugin
        {
            public override void Initialize(string pluginName, IScheduler scheduler)
            {
                scheduler.Clear();
                base.Initialize(pluginName, scheduler);
            }
        }
    }
}
