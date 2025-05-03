using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using API.MCP;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class McpClientController : ControllerBase
    {
        private readonly ILogger<McpClientController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMcpClient _mcpClient;
        private readonly McpClient _client;

        public McpClientController(ILogger<McpClientController> logger, HttpClient httpClient, IMcpClient mcpClient, McpClient client)
        {
            _logger = logger;
            _httpClient = httpClient;
            _mcpClient = mcpClient;
            _client = client;
        }

        // Comment out unused endpoints
        /*
        [HttpGet("market-trends")]
        public async Task<IActionResult> GetMarketTrends([FromQuery] int daysLookback = 30)
        {
            try
            {
                // Create client that connects to local MCP server
                var mcpClient = await ConnectToMcpServer();

                // Create wrapper for convenience
                var client = new McpClient(mcpClient);

                // Get analysis
                var result = await client.AnalyzeMarketTrends(daysLookback);
                
                return Ok(new { success = true, result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("seasonal-trends")]
        public async Task<IActionResult> GetSeasonalTrends([FromQuery] int monthsLookback = 12)
        {
            try
            {
                // Create client that connects to local MCP server
                var mcpClient = await ConnectToMcpServer();

                // Create wrapper for convenience
                var client = new McpClient(mcpClient);

                // Get analysis
                var result = await client.AnalyzeSeasonalTrends(monthsLookback);
                
                return Ok(new { success = true, result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("product-correlations")]
        public async Task<IActionResult> GetProductCorrelations([FromQuery] int topCount = 10, [FromQuery] int daysLookback = 90)
        {
            try
            {
                // Create client that connects to local MCP server
                var mcpClient = await ConnectToMcpServer();

                // Create wrapper for convenience
                var client = new McpClient(mcpClient);

                // Get analysis
                var result = await client.AnalyzeProductCorrelations(topCount, daysLookback);
                
                return Ok(new { success = true, result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        */

        // [HttpGet("list-tools")]
        // public async Task<IActionResult> ListTools()
        // {
        //     try
        //     {
        //         var tools = await _client.ListAvailableTools();
        //         return Ok(tools);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error listing tools");
        //         return StatusCode(500, new { error = "Internal server error" });
        //     }
        // }

        // [HttpPost("ask")]
        // public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        // {
        //     try
        //     {
        //         var tools = await _client.ListAvailableTools();
        //         var response = await _client.AskWithTools(request.Message, tools);
        //         return Ok(new { response });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error processing chat request");
        //         return StatusCode(500, new { error = "Internal server error" });
        //     }
        // }

        // Comment out unused method
        /*
        private async Task<IMcpClient> ConnectToMcpServer()
        {
            // Create a transport that uses stdio to communicate with a local MCP server
            var transportOptions = new StdioClientTransportOptions
            {
                Name = "McpServer",
                Command = "dotnet",
                Arguments = new[] { "run", "--project", "../API/API" }
            };
                
            var transport = new StdioClientTransport(transportOptions);
            return await McpClientFactory.CreateAsync(transport);
        }
        */
    }

    // public class ChatRequest
    // {
    //     public string Message { get; set; }
    // }
} 