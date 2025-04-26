using System.Collections.Generic;
using ModelContextProtocol.Server;

namespace API.MCP.Models
{
    /// <summary>
    /// Represents a request to an LLM service
    /// </summary>
    public class LLMRequest
    {
        /// <summary>
        /// The prompt to send to the LLM
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// Optional system prompt for context
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// List of chat messages for conversation-based LLMs
        /// </summary>
        public List<ChatMessage> Messages { get; set; }

        /// <summary>
        /// Provider-specific options
        /// </summary>
        public Dictionary<string, object> Options { get; set; }

        /// <summary>
        /// The provider to use (e.g., "OpenAI", "Gemini", "Claude")
        /// </summary>
        public string Provider { get; set; }
    }
} 