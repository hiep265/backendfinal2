using System;
using System.Threading.Tasks;
using API.MCP.Configuration;
using API.MCP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace API.MCP.Services
{
    /// <summary>
    /// Interface for LLM services
    /// </summary>
    public interface ILLMService
    {
        /// <summary>
        /// Generates text completion based on a prompt
        /// </summary>
        /// <param name="prompt">The prompt to send to the LLM</param>
        /// <param name="systemPrompt">Optional system prompt for context</param>
        /// <returns>Generated text response</returns>
        Task<string> GenerateCompletionAsync(string prompt, string systemPrompt = null);

        /// <summary>
        /// Generates text completion based on conversation history
        /// </summary>
        /// <param name="messages">Array of chat messages</param>
        /// <param name="options">Chat options</param>
        /// <returns>Generated text response</returns>
        Task<string> GenerateCompletionAsync(ChatMessage[] messages, API.MCP.Models.ChatOptions options);

        /// <summary>
        /// Processes an LLM request
        /// </summary>
        /// <param name="request">The LLM request to process</param>
        /// <returns>Generated text response</returns>
        Task<string> ProcessAsync(LLMRequest request);
    }

    /// <summary>
    /// Factory to create appropriate LLM service instance based on the configuration
    /// </summary>
    public class LLMServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly LLMConfig _config;
        private readonly ILogger<LLMServiceFactory> _logger;

        public LLMServiceFactory(
            IServiceProvider serviceProvider,
            IOptions<LLMConfig> config,
            ILogger<LLMServiceFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _config = config.Value;
            _logger = logger;
        }

        /// <summary>
        /// Creates an instance of ILLMService (currently only supports Gemini)
        /// </summary>
        /// <returns>An implementation of ILLMService</returns>
        public ILLMService CreateService()
        {
            _logger.LogInformation($"Creating LLM service for provider: {_config.Provider}");
            
            if (_config.Provider.ToLower() != "gemini")
            {
                _logger.LogWarning($"Provider {_config.Provider} not supported, defaulting to Gemini");
            }
            
            return _serviceProvider.GetService(typeof(GeminiService)) as ILLMService;
        }
    }

    /// <summary>
    /// Default LLM service implementation that routes requests to the appropriate provider
    /// </summary>
    public class LLMService : ILLMService
    {
        private readonly LLMServiceFactory _factory;
        private readonly ILogger<LLMService> _logger;

        public LLMService(LLMServiceFactory factory, ILogger<LLMService> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<string> GenerateCompletionAsync(string prompt, string systemPrompt = null)
        {
            try
            {
                var service = _factory.CreateService();
                return await service.GenerateCompletionAsync(prompt, systemPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating completion with prompt");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string> GenerateCompletionAsync(ChatMessage[] messages, API.MCP.Models.ChatOptions options)
        {
            try
            {
                var service = _factory.CreateService();
                return await service.GenerateCompletionAsync(messages, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating completion with chat messages");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string> ProcessAsync(LLMRequest request)
        {
            try
            {
                var service = _factory.CreateService();
                return await service.ProcessAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing LLM request");
                throw;
            }
        }
    }
} 