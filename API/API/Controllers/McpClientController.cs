using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using API.MCP;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class McpClientController : ControllerBase
    {
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

        [HttpGet("list-tools")]
        public async Task<IActionResult> ListTools()
        {
            try
            {
                // Create client that connects to local MCP server
                var mcpClient = await ConnectToMcpServer();

                // Create wrapper for convenience
                var client = new McpClient(mcpClient);

                // List available tools
                var tools = await client.ListAvailableTools();
                
                return Ok(new { success = true, tools });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

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
    }
} 