using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Producer;

namespace _01_SendEvents
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Starting Event Hub Producer service. Messages will be sent periodically.");

            while (true)
            {
                try
                {
                    Console.WriteLine("Initializing Event Hub Producer Client...");
                    
                    // Initialize the producer client
                    await using EventHubProducerClient producerClient = await Utils.InitializeProducerClient();
                    Console.WriteLine("Producer client created successfully.");

                    // Send events
                    await Utils.SendEvents(producerClient);

                    // Wait for the next iteration
                    Console.WriteLine($"Waiting for {Constants.EventSendDelay.TotalMinutes} minutes before sending the next batch...");
                    await Task.Delay(Constants.EventSendDelay);
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
}
