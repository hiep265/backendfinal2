using System;
using System.Threading.Tasks;
using API.MCP.Configuration;
using API.MCP.Services;
using API.MCP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace API.MCP.Services
{
    /// <summary>
    /// Implementation of IMcpServerExchange for LLM service requests
    /// </summary>
    public class LLMMcpServerExchange : ModelContextProtocol.Server.IMcpServerExchange
    {
        private readonly ILLMService _llmService;
        private readonly ILogger<LLMMcpServerExchange> _logger;

        public LLMMcpServerExchange(
            ILLMService llmService,
            ILogger<LLMMcpServerExchange> logger)
        {
            _llmService = llmService;
            _logger = logger;
        }

        /// <summary>
        /// Handles a request to the LLM service
        /// </summary>
        /// <param name="request">The request data</param>
        /// <returns>The response from the LLM service</returns>
        public async Task<object> HandleRequestAsync(object request)
        {
            try
            {
                if (request is not LLMRequest llmRequest)
                {
                    throw new ArgumentException($"Invalid request type: {request.GetType().Name}");
                }

                // Process the request directly with our service
                return await _llmService.ProcessAsync(llmRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing LLM request");
                throw;
            }
        }

        public ISamplingChatClient AsSamplingChatClient()
        {
            return new LLMSamplingChatClient(_llmService);
        }

        public ClientCapabilities GetClientCapabilities()
        {
            return new ClientCapabilities
            {
                Sampling = new SamplingCapability { Enabled = true }
            };
        }
    }
    
    /// <summary>
    /// Implementation of ISamplingChatClient using our LLM service
    /// </summary>
    public class LLMSamplingChatClient : ISamplingChatClient
    {
        private readonly ILLMService _llmService;
        
        public LLMSamplingChatClient(ILLMService llmService)
        {
            _llmService = llmService;
        }
        
        public async Task<string> GetResponseAsync(ChatMessage[] messages, ModelContextProtocol.Server.ChatOptions options)
        {
            try
            {
                // Create our own options since ModelContextProtocol.Server.ChatOptions 
                // might not have the same properties
                var chatOptions = new API.MCP.Models.ChatOptions
                {
                    Temperature = 0.7f,
                    MaxTokens = 1000
                };
                
                return await _llmService.GenerateCompletionAsync(messages, chatOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LLM error: {ex.Message}");
                return $"Error generating response: {ex.Message}";
            }
        }
    }
} 