using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using API.Data;
using API.MCP.Tools;
using API.MCP.Services;
using API.MCP.Configuration;
using ModelContextProtocol.Server;

namespace API.MCP.Extensions
{
    public static class McpToolsExtension
    {
        public static IServiceCollection AddMarketTrendAnalysisTool(this IServiceCollection services)
        {
            return services.AddScoped<MarketTrendAnalysisTool>();
        }
        
        public static IServiceCollection AddReviewAnalysisTool(this IServiceCollection services)
        {
            return services.AddScoped<ReviewAnalysisTool>();
        }
        
        public static IServiceCollection AddContentGenerationTool(this IServiceCollection services)
        {
            return services.AddScoped<ContentGenerationTool>();
        }
        
        public static IServiceCollection AddProductRecommendationTool(this IServiceCollection services)
        {
            return services.AddScoped<ProductRecommendationTool>();
        }
        
        public static IServiceCollection AddProductQueryTool(this IServiceCollection services)
        {
            return services.AddScoped<ProductQueryTool>();
        }
        
        public static IServiceCollection AddSimpleMcpServerExchange(this IServiceCollection services)
        {
            // Register a simple mock implementation of IMcpServerExchange
            services.AddSingleton<IMcpServerExchange, SimpleMcpServerExchange>();
            return services;
        }
        
        public static IServiceCollection AddAllMcpTools(this IServiceCollection services, IConfiguration configuration = null)
        {
            if (configuration != null)
            {
                // Sử dụng McpConfig để đăng ký các công cụ
                McpConfig.RegisterMcpTools(services, configuration);
                return services;
            }
            else
            {
                // Fallback sang cách đăng ký trước đây
                return services
                    .AddMarketTrendAnalysisTool()
                    .AddReviewAnalysisTool()
                    .AddContentGenerationTool()
                    .AddProductRecommendationTool()
                    .AddProductQueryTool();
            }
        }
    }
    
    // Simple implementation of IMcpServerExchange
    public class SimpleMcpServerExchange : IMcpServerExchange
    {
        public ISamplingChatClient AsSamplingChatClient()
        {
            return new SimpleSamplingChatClient();
        }

        public ClientCapabilities GetClientCapabilities()
        {
            return new ClientCapabilities
            {
                Sampling = new SamplingCapability { Enabled = true }
            };
        }
    }
    
    // Simple implementation of ISamplingChatClient
    public class SimpleSamplingChatClient : ISamplingChatClient
    {
        public Task<string> GetResponseAsync(ChatMessage[] messages, ChatOptions options)
        {
            // In a real implementation, this would call out to an LLM
            return Task.FromResult("This is a simple response from the sampling chat client.");
        }
    }
} 