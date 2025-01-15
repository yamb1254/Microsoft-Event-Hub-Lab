using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace _01_SendEvents
{
    public static class Utils
    {
        public static async Task<EventHubProducerClient> InitializeProducerClient()
        {
            // Retrieve the Event Hub namespace and name from environment variables
            string eventHubNamespace = Environment.GetEnvironmentVariable(Constants.EventHubNamespaceEnv);
            string eventHubName = Environment.GetEnvironmentVariable(Constants.EventHubNameEnv);

            if (string.IsNullOrEmpty(eventHubNamespace) || string.IsNullOrEmpty(eventHubName))
            {
                throw new InvalidOperationException("Event Hub namespace or name is not set in the environment variables.");
            }

            // Construct the fully qualified namespace (e.g., "<namespace>.servicebus.windows.net")
            string fullyQualifiedNamespace = $"{eventHubNamespace}.servicebus.windows.net";

            // Use DefaultAzureCredential to authenticate using Managed Identity
            var credential = new DefaultAzureCredential();

            // Create and return the producer client
            return new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, credential);
        }

        // Sends a batch of events to the Event Hub
        public static async Task SendEvents(EventHubProducerClient producerClient)
        {
            try
            {
                using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                foreach (var eventData in Constants.DefaultMessages)
                {
                    if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(eventData))))
                    {
                        Console.WriteLine($"Error: Event '{eventData}' could not be added to the batch. The batch might be full.");
                        return;
                    }
                }

                Console.WriteLine("All events added to the batch successfully.");
                await producerClient.SendAsync(eventBatch);
                Console.WriteLine($"A batch of events has been published successfully at {DateTime.UtcNow} UTC.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while sending events: {ex.Message}");
                throw;
            }
        }
    }
}
