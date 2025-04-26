using System;
using System.Threading.Tasks;

namespace ModelContextProtocol.Server
{
    public interface IMcpServerExchange
    {
        ClientCapabilities GetClientCapabilities();
        ISamplingChatClient AsSamplingChatClient();
    }

    public class ClientCapabilities
    {
        public SamplingCapability Sampling { get; set; }
    }

    public class SamplingCapability
    {
        public bool Enabled { get; set; }
    }

    public interface ISamplingChatClient
    {
        Task<string> GetResponseAsync(ChatMessage[] messages, ChatOptions options);
    }

    public class ChatMessage
    {
        public string Role { get; }
        public string Content { get; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    public static class ChatRole
    {
        public static string System => "system";
        public static string User => "user";
        public static string Assistant => "assistant";
    }

    public class ChatOptions
    {
        public string SystemPrompt { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class McpServerToolTypeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class McpServerToolAttribute : Attribute
    {
    }
} 