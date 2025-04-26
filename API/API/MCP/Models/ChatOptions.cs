namespace API.MCP.Models
{
    /// <summary>
    /// Options for chat-based LLM requests
    /// </summary>
    public class ChatOptions
    {
        /// <summary>
        /// The temperature parameter for controlling randomness (0.0 to 1.0)
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        public int MaxTokens { get; set; } = 1000;

        /// <summary>
        /// Name of the model to use
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// System prompt for setting context
        /// </summary>
        public string SystemPrompt { get; set; }
    }
} 