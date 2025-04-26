using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace API.MCP.Tools
{
    [McpServerToolType]
    public class ReviewAnalysisTool
    {
        private readonly DPContext _context;
        private readonly IMcpServerExchange _exchange;
        
        public ReviewAnalysisTool(DPContext context, IMcpServerExchange exchange)
        {
            _context = context;
            _exchange = exchange;
        }
        
        [McpServerTool, Description("Phân tích đánh giá sản phẩm")]
        public async Task<object> AnalyzeProductReviews(int productId)
        {
            var comments = await _context.UserComments
                .Where(c => c.IdSanPham == productId)
                .Select(c => c.Content)
                .ToListAsync();
                
            if (comments == null || comments.Count == 0)
                return new { error = "Sản phẩm chưa có đánh giá" };
                
            // Kiểm tra xem LLM có sẵn không
            if (_exchange.GetClientCapabilities()?.Sampling != null)
            {
                try {
                    // Chuẩn bị dữ liệu đánh giá để gửi tới LLM
                    string reviewsText = string.Join("\n- ", comments);
                    reviewsText = "- " + reviewsText;
                    
                    var analysisRequest = new ChatMessage[]
                    {
                        new ChatMessage(ChatRole.User, $"Phân tích các đánh giá sản phẩm sau và tóm tắt điểm mạnh, điểm yếu và xu hướng chung:\n\n{reviewsText}")
                    };
                    
                    var result = await _exchange.AsSamplingChatClient().GetResponseAsync(
                        analysisRequest,
                        new ChatOptions
                        {
                            SystemPrompt = "Bạn là một chuyên gia phân tích đánh giá sản phẩm. Hãy phân tích các đánh giá và trả về JSON có cấu trúc sau: { \"strengths\": [\"điểm mạnh 1\", \"điểm mạnh 2\"...], \"weaknesses\": [\"điểm yếu 1\", \"điểm yếu 2\"...], \"trends\": [\"xu hướng 1\", \"xu hướng 2\"...], \"sentiment\": \"positive/negative/neutral\", \"summary\": \"tóm tắt ngắn gọn\" }"
                        });
                    
                    return new { 
                        id = productId,
                        totalComments = comments.Count,
                        analysis = result
                    };
                }
                catch (Exception ex)
                {
                    return new { 
                        id = productId,
                        error = $"Lỗi khi phân tích đánh giá: {ex.Message}" 
                    };
                }
            }
            else
            {
                // Fallback khi không có LLM
                return new { 
                    id = productId,
                    totalComments = comments.Count,
                    comments = comments,
                    note = "Phân tích tự động không khả dụng do không có LLM"
                };
            }
        }
        
        [McpServerTool, Description("Phân tích cảm xúc của khách hàng từ đánh giá")]
        public async Task<object> AnalyzeSentiment(int productId)
        {
            var comments = await _context.UserComments
                .Where(c => c.IdSanPham == productId)
                .Select(c => new { 
                    c.Content, 
                    UserName = _context.AppUsers.Where(u => u.Id == c.IdUser).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    c.NgayComment
                })
                .ToListAsync();
                
            if (comments == null || comments.Count == 0)
                return new { error = "Sản phẩm chưa có đánh giá" };
                
            // Kiểm tra xem LLM có sẵn không
            if (_exchange.GetClientCapabilities()?.Sampling != null)
            {
                try {
                    // Chuẩn bị dữ liệu đánh giá để gửi tới LLM
                    string reviewsText = JsonSerializer.Serialize(comments);
                    
                    var sentimentRequest = new ChatMessage[]
                    {
                        new ChatMessage(ChatRole.User, $"Phân tích cảm xúc và tình cảm của khách hàng từ các đánh giá sau:\n\n{reviewsText}")
                    };
                    
                    var result = await _exchange.AsSamplingChatClient().GetResponseAsync(
                        sentimentRequest,
                        new ChatOptions 
                        {
                            SystemPrompt = "Bạn là một chuyên gia phân tích cảm xúc từ đánh giá khách hàng. Hãy đánh giá mức độ hài lòng, phân loại cảm xúc và đề xuất cải tiến dựa trên phản hồi. Trả về JSON có cấu trúc: { \"overallSentiment\": \"positive/negative/neutral\", \"sentimentScore\": number từ 1-10, \"emotionalKeywords\": [\"từ khóa 1\", \"từ khóa 2\"...], \"suggestionForImprovement\": \"đề xuất cải tiến\" }"
                        });
                    
                    return new { 
                        id = productId,
                        totalComments = comments.Count,
                        sentiment = result
                    };
                }
                catch (Exception ex)
                {
                    return new { 
                        id = productId,
                        error = $"Lỗi khi phân tích cảm xúc: {ex.Message}" 
                    };
                }
            }
            else
            {
                // Fallback khi không có LLM
                return new { 
                    id = productId,
                    totalComments = comments.Count,
                    comments = comments,
                    note = "Phân tích cảm xúc không khả dụng do không có LLM"
                };
            }
        }
    }
} 