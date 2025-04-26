using System;

namespace API.MCP.Configuration
{
    public class LLMConfig
    {
        /// <summary>
        /// The LLM provider (OpenAI, Claude, or Gemini)
        /// </summary>
        public string Provider { get; set; } = "Gemini";

        /// <summary>
        /// API key for the LLM provider
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Base URL for the API (e.g., https://api.openai.com)
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Model name to use (e.g., gpt-4o, claude-3-opus-20240229)
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Temperature setting for response generation (0.0-1.0)
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Maximum tokens to generate in the response
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// Timeout in seconds for the API request
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;
    }
} 