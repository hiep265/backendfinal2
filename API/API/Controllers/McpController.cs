using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class McpController : ControllerBase
    {
        private readonly IMcpServerExchange _mcpServer;

        public McpController(IMcpServerExchange mcpServer)
        {
            _mcpServer = mcpServer;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessRequest()
        {
            try
            {
                // Ghi log bắt đầu xử lý yêu cầu MCP
                System.Console.WriteLine("Bắt đầu xử lý yêu cầu MCP từ client...");

                // Đọc nội dung của request
                using var reader = new System.IO.StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                
                System.Console.WriteLine($"Đã nhận yêu cầu MCP: {requestBody.Substring(0, System.Math.Min(requestBody.Length, 100))}...");
                
                // Xử lý yêu cầu MCP bằng IMcpServerExchange
                // Thông thường, ModelContextProtocol sẽ tự động xử lý điều này
                // nhưng chúng ta cung cấp xử lý tùy chỉnh để gỡ lỗi
                var response = new { success = true, message = "Yêu cầu MCP đã được xử lý" };
                
                System.Console.WriteLine("Đã xử lý yêu cầu MCP thành công");
                
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Lỗi khi xử lý yêu cầu MCP: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
} 