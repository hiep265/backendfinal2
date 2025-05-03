using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using API.MCP;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly AIRequestHandler _aiHandler;

        public AIController(AIRequestHandler aiHandler)
        {
            _aiHandler = aiHandler;
        }

        [HttpPost("query")]
        // public async Task<IActionResult> ProcessQuery([FromBody] UserQueryRequest request)
        // {
        //     try
        //     {
        //         var result = await _aiHandler.ProcessUserRequest(request.Query);
        //         return Ok(new { success = true, result });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { success = false, error = ex.Message });
        //     }
        // }
        
        // Cung cấp trợ giúp về cách sử dụng API AI
        [HttpGet("help")]
        public IActionResult GetHelp()
        {
            var examples = new[]
            {
                "Tìm áo sơ mi nam màu xanh",
                "Gợi ý sản phẩm tương tự sản phẩm 123",
                "Phân tích đánh giá cho sản phẩm 456",
                "Phân tích xu hướng thị trường thời trang",
                "Tạo mô tả sản phẩm cho sản phẩm 789",
                "Kiểm tra trạng thái đơn hàng 101112"
            };
            
            return Ok(new { 
                message = "Gửi yêu cầu đến /api/ai/query với nội dung trong định dạng JSON {\"query\": \"nội dung yêu cầu\"}",
                examples
            });
        }
    }

    public class UserQueryRequest
    {
        public string Query { get; set; }
    }
} 