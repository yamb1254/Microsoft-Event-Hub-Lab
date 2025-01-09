using System;

namespace _01_SendEvents
{
    public static class Constants
    {
        // Environment variable names
        public const string EventHubConnectionStringEnv = "EVENT_HUB_CONNECTION_STRING";
        public const string EventHubNameEnv = "EVENT_HUB_NAME";

        // Default messages to send
        public static readonly string[] DefaultMessages = { "First event", "Second event", "Third event" };

        // Delay between sending events
        public static readonly TimeSpan EventSendDelay = TimeSpan.FromHours(1);
    }
}
