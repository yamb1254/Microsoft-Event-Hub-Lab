using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace _01_SendEvents
{
    public static class Utils
    {

        public static async Task<EventHubProducerClient> InitializeProducerClient()
        {
            // Retrieve connection string and Event Hub name from environment variables
            string eventHubConnectionString = Environment.GetEnvironmentVariable(Constants.EventHubConnectionStringEnv);
            string eventHubName = Environment.GetEnvironmentVariable(Constants.EventHubNameEnv);

            if (string.IsNullOrEmpty(eventHubConnectionString) || string.IsNullOrEmpty(eventHubName))
            {
                throw new InvalidOperationException("Event Hub connection string or name is not set in the environment variables.");
            }

            // Create and return the producer client
            return new EventHubProducerClient(eventHubConnectionString, eventHubName);
        }


        // Sends a batch of events to the Event Hub.
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
