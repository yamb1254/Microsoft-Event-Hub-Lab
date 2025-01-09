using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;

namespace _02_ReceiveEvents
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Starting Event Hub Consumer service...");

            try
            {
                // Initialize the consumer client
                await using EventHubConsumerClient consumerClient = await Utils.InitializeConsumerClient();
                Console.WriteLine("Consumer client created successfully.");

                // Create a cancellation token to allow graceful shutdown
                using var cancellationTokenSource = new CancellationTokenSource();

                // Register a handler for SIGINT or SIGTERM to stop listening
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    Console.WriteLine("Stopping the listener...");
                    cancellationTokenSource.Cancel();
                    eventArgs.Cancel = true;
                };

                // Start receiving events
                await Utils.ReceiveEvents(consumerClient, cancellationTokenSource.Token);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Authorization Error: {ex.Message}");
                Console.WriteLine("Ensure that the connection string has the appropriate permissions.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
