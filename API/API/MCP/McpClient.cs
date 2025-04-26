using ModelContextProtocol;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.MCP
{
    public class McpClient
    {
        private readonly IMcpClient _client;

        public McpClient(IMcpClient client)
        {
            _client = client;
        }

        public async Task<IList<McpClientTool>> ListAvailableTools()
        {
            try
            {
                return await _client.ListToolsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing tools: {ex.Message}");
                return Array.Empty<McpClientTool>();
            }
        }

        public async Task<dynamic> CallTool(string toolName, Dictionary<string, object> parameters)
        {
            try
            {
                return await _client.CallToolAsync(toolName, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling tool {toolName}: {ex.Message}");
                throw;
            }
        }

        // Example method for market analysis
        public async Task<string> AnalyzeMarketTrends(int daysLookback = 30)
        {
            try
            {
                var result = await CallTool("analyzeproducttrends", new Dictionary<string, object>
                {
                    { "daysLookback", daysLookback }
                });

                // Extract the text content from the response
                if (result.Content != null && result.Content.Count > 0)
                {
                    foreach (var content in result.Content)
                    {
                        if (content.GetType().Name.Contains("Text"))
                        {
                            // Use reflection to safely get the Text property
                            var textProperty = content.GetType().GetProperty("Text");
                            if (textProperty != null)
                            {
                                return textProperty.GetValue(content)?.ToString() ?? "";
                            }
                        }
                    }
                }

                return "No content returned from analysis";
            }
            catch (Exception ex)
            {
                return $"Failed to analyze market trends: {ex.Message}";
            }
        }

        // Example method for seasonal trends
        public async Task<string> AnalyzeSeasonalTrends(int monthsLookback = 12)
        {
            try
            {
                var result = await CallTool("analyzeseasonaltrends", new Dictionary<string, object>
                {
                    { "monthsLookback", monthsLookback }
                });

                // Extract the text content from the response
                if (result.Content != null && result.Content.Count > 0)
                {
                    foreach (var content in result.Content)
                    {
                        if (content.GetType().Name.Contains("Text"))
                        {
                            // Use reflection to safely get the Text property
                            var textProperty = content.GetType().GetProperty("Text");
                            if (textProperty != null)
                            {
                                return textProperty.GetValue(content)?.ToString() ?? "";
                            }
                        }
                    }
                }

                return "No content returned from seasonal analysis";
            }
            catch (Exception ex)
            {
                return $"Failed to analyze seasonal trends: {ex.Message}";
            }
        }

        // Example method for product correlation
        public async Task<string> AnalyzeProductCorrelations(int topCount = 10, int daysLookback = 90)
        {
            try
            {
                var result = await CallTool("analyzeproductcorrelations", new Dictionary<string, object>
                {
                    { "topCount", topCount },
                    { "daysLookback", daysLookback }
                });

                // Extract the text content from the response
                if (result.Content != null && result.Content.Count > 0)
                {
                    foreach (var content in result.Content)
                    {
                        if (content.GetType().Name.Contains("Text"))
                        {
                            // Use reflection to safely get the Text property
                            var textProperty = content.GetType().GetProperty("Text");
                            if (textProperty != null)
                            {
                                return textProperty.GetValue(content)?.ToString() ?? "";
                            }
                        }
                    }
                }

                return "No content returned from correlation analysis";
            }
            catch (Exception ex)
            {
                return $"Failed to analyze product correlations: {ex.Message}";
            }
        }
    }
} 