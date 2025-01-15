using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;

namespace _02_ReceiveEvents
{
    public static class Utils
    {
        public static async Task<EventHubConsumerClient> InitializeConsumerClient()
        {
            // Retrieve Event Hub namespace and name from environment variables
            string eventHubNamespace = Environment.GetEnvironmentVariable(Constants.EventHubNamespaceEnv);
            string eventHubName = Environment.GetEnvironmentVariable(Constants.EventHubNameEnv);
            string consumerGroup = Constants.DefaultConsumerGroup;

            if (string.IsNullOrEmpty(eventHubNamespace) || string.IsNullOrEmpty(eventHubName))
            {
                throw new InvalidOperationException("Event Hub namespace or name is not set in the environment variables.");
            }

            // Create a fully qualified namespace (FQDN)
            string fullyQualifiedNamespace = $"{eventHubNamespace}.servicebus.windows.net";

            // Use DefaultAzureCredential for authentication
            Console.WriteLine("Initializing Event Hub Consumer Client using Managed Identity...");
            var credential = new DefaultAzureCredential();

            // Create and return the consumer client
            return new EventHubConsumerClient(consumerGroup, fullyQualifiedNamespace, eventHubName, credential);
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
