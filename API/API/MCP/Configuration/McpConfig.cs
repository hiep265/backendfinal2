using API.MCP.Extensions;
using API.MCP.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace API.MCP.Configuration
{
    public static class McpConfig
    {
        // Cấu hình đăng ký các công cụ MCP
        public static void RegisterMcpTools(IServiceCollection services, IConfiguration configuration)
        {
            // Đăng ký tất cả các công cụ MCP
            services.AddMarketTrendAnalysisTool();
            services.AddReviewAnalysisTool();
            services.AddContentGenerationTool();
            services.AddProductRecommendationTool();
            services.AddProductQueryTool();
            
            // Đăng ký dịch vụ AI
            services.AddAIServices(configuration);
        }
    }
} 