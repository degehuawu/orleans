using System;
using System.Threading.Tasks;
using Orleans.Runtime.Configuration;
using Orleans.Streams;
using Orleans.Runtime;
using Orleans.Providers.Streams.Common;

namespace Orleans.Providers.Streams.AzureQueue
{
    /// <summary> Factory class for Azure Queue based stream provider.</summary>
    public class AzureQueueAdapterFactory : IQueueAdapterFactory
    {
        private const int DEFAULT_CACHE_SIZE = 4096;
        private const string NUM_QUEUES_PARAM = "NumQueues";

        /// <summary> Default number oi\f Azure Queue used in this stream provider.</summary>
        public const int DEFAULT_NUM_QUEUES = 8; // keep as power of 2.
        
        private string deploymentId;
        private string dataConnectionString;
        private string providerName;
        private int cacheSize;
        private int numQueues;
        private HashRingBasedStreamQueueMapper streamQueueMapper;
        private IQueueAdapterCache adapterCache;

        /// <summary>"DataConnectionString".</summary>
        public const string DATA_CONNECTION_STRING = "DataConnectionString";
        /// <summary>"DeploymentId".</summary>
        public const string DEPLOYMENT_ID = "DeploymentId";

        /// <summary> Init the factory.</summary>
        public virtual void Init(IProviderConfiguration config, string providerName, Logger logger, IServiceProvider serviceProvider)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (!config.Properties.TryGetValue(DATA_CONNECTION_STRING, out dataConnectionString))
                throw new ArgumentException(String.Format("{0} property not set", DATA_CONNECTION_STRING));
            if (!config.Properties.TryGetValue(DEPLOYMENT_ID, out deploymentId))
                throw new ArgumentException(String.Format("{0} property not set", DEPLOYMENT_ID));

            cacheSize = SimpleQueueAdapterCache.ParseSize(config, DEFAULT_CACHE_SIZE);

            string numQueuesString;
            numQueues = DEFAULT_NUM_QUEUES;
            if (config.Properties.TryGetValue(NUM_QUEUES_PARAM, out numQueuesString))
            {
                if (!int.TryParse(numQueuesString, out numQueues))
                    throw new ArgumentException(String.Format("{0} invalid.  Must be int", NUM_QUEUES_PARAM));
            }

            this.providerName = providerName;
            streamQueueMapper = new HashRingBasedStreamQueueMapper(numQueues, providerName);
            adapterCache = new SimpleQueueAdapterCache(cacheSize, logger);
        }

        /// <summary>Creates the Azure Queue based adapter.</summary>
        public virtual Task<IQueueAdapter> CreateAdapter()
        {
            var adapter = new AzureQueueAdapter(streamQueueMapper, dataConnectionString, deploymentId, providerName);
            return Task.FromResult<IQueueAdapter>(adapter);
        }

        /// <summary>Creates the adapter cache.</summary>
        public virtual IQueueAdapterCache GetQueueAdapterCache()
        {
            return adapterCache;
        }

        /// <summary>Creates the factory stream queue mapper.</summary>
        public IStreamQueueMapper GetStreamQueueMapper()
        {
            return streamQueueMapper;
        }

        /// <summary>
        /// Creates a delivery failure handler for the specified queue.
        /// </summary>
        /// <param name="queueId"></param>
        /// <returns></returns>
        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
        {
            return Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler(false));
        }
    }
}
