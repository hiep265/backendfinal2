using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using API.MCP;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IInventoryTool _inventoryTool;
        private readonly string _hardcodedUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=AIzaSyCBmGo3mAG5hJQs-KYWL0qQC8YO6yBd5Pg";

        public ChatController(HttpClient httpClient, IInventoryTool inventoryTool)
        {
            _httpClient = httpClient;
            _inventoryTool = inventoryTool;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            try
            {
                // 1. Tạo prompt cho Gemini
                var systemPrompt = @"Bạn là một trợ lý AI thông minh. 
Khi người dùng hỏi về tồn kho sản phẩm, hãy trích xuất tên sản phẩm và gọi tool CheckInventory.
Ví dụ:
- Nếu người dùng hỏi: 'Cái áo thun này còn không?'
- Bạn sẽ gọi: CheckInventory('áo thun')
- Nếu người dùng hỏi: 'Còn quần jean size M không?'
- Bạn sẽ gọi: CheckInventory('quần jean')";

                // 2. Gọi Gemini API
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = $"{systemPrompt}\n\nUser: {request.Message}" }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(_hardcodedUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Gemini API error: {response.StatusCode}, Details: {errorContent}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseString);

                // 3. Phân tích response từ Gemini
                if (responseObject.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var text))
                {
                    var geminiResponse = text.GetString();

                    // 4. Nếu Gemini quyết định gọi tool CheckInventory
                    if (geminiResponse.Contains("CheckInventory"))
                    {
                        var productName = ExtractProductName(geminiResponse);
                        var result = await _inventoryTool.CheckInventoryAsync(productName);
                        return Ok(new { message = result });
                    }

                    return Ok(new { message = geminiResponse });
                }

                throw new Exception("Không thể phân tích phản hồi từ Gemini");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private string ExtractProductName(string response)
        {
            // Tìm chuỗi nằm giữa CheckInventory(' và ')
            var start = response.IndexOf("CheckInventory('") + "CheckInventory('".Length;
            var end = response.IndexOf("')", start);
            if (start >= 0 && end >= 0)
            {
                return response.Substring(start, end - start);
            }

            // Nếu không tìm thấy, lấy từ cuối cùng của câu hỏi
            return response.Split(' ').Last();
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
} 