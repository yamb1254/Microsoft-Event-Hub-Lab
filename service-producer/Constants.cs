using System;

public static class Constants
{
    public const string EventHubNamespaceEnv = "EVENT_HUB_NAMESPACE"; 
    public const string EventHubNameEnv = "EVENT_HUB_NAME";
    public static readonly TimeSpan EventSendDelay = TimeSpan.FromMinutes(1);
    public static readonly string[] DefaultMessages = { "Message 1", "Message 2", "Message 3" };
}
