using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;

namespace _02_ReceiveEvents
{
    public static class Utils
    {

        public static async Task<EventHubConsumerClient> InitializeConsumerClient()
        {
            // Retrieve connection string and Event Hub name from environment variables
            string eventHubConnectionString = Environment.GetEnvironmentVariable(Constants.EventHubConnectionStringEnv);
            string eventHubName = Environment.GetEnvironmentVariable(Constants.EventHubNameEnv);
            string consumerGroup = Constants.DefaultConsumerGroup;

            if (string.IsNullOrEmpty(eventHubConnectionString) || string.IsNullOrEmpty(eventHubName))
            {
                throw new InvalidOperationException("Event Hub connection string or name is not set in the environment variables.");
            }

            // Create and return the consumer client
            Console.WriteLine("Initializing Event Hub Consumer Client...");
            return new EventHubConsumerClient(consumerGroup, eventHubConnectionString, eventHubName);
        }

        // Processes events from the Event Hub.
        public static async Task ReceiveEvents(EventHubConsumerClient consumerClient, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Listening for events in the consumer group: {Constants.DefaultConsumerGroup}");

            try
            {
                await foreach (PartitionEvent partitionEvent in consumerClient.ReadEventsAsync(cancellationToken))
                {
                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    Console.WriteLine($"Received event from partition {partitionEvent.Partition.PartitionId}: {data}");
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Event receiving process was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while receiving events: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
