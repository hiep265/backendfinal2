using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using API.MCP.Configuration;
using API.MCP.Services;
using ModelContextProtocol.Server;

namespace API.MCP.Extensions
{
    public static class AIServicesExtension
    {
        public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure LLM services
            services.AddOptions<LLMConfig>()
                .BindConfiguration("LLMConfig");
            
            // Register LLMConfig as a singleton service
            var llmConfig = new LLMConfig();
            configuration.GetSection("LLMConfig").Bind(llmConfig);
            services.AddSingleton(llmConfig);
            
            // Register GeminiService and its dependencies
            services.AddHttpClient<GeminiService>();
            services.AddScoped<GeminiService>();
            
            // Register LLM service system
            services.AddScoped<LLMServiceFactory>();
            services.AddScoped<ILLMService, LLMService>();
            
            // Register ModelContextProtocol server exchange
            services.AddScoped<IMcpServerExchange, LLMMcpServerExchange>();
            
            // Register the AIRequestHandler
            services.AddScoped<AIRequestHandler>();

            return services;
        }
    }
} 