using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Server;
using System.Text.Json;
using API.MCP.Extensions;
using API.MCP.Services;

namespace API.MCP
{
    public class AIRequestHandler
    {
        private readonly McpClient _mcpClient;
        private readonly ModelContextProtocol.Server.IMcpServerExchange _exchange;
        private readonly ILLMService _llmService;

        public AIRequestHandler(
            McpClient mcpClient, 
            ModelContextProtocol.Server.IMcpServerExchange exchange,
            ILLMService llmService)
        {
            _mcpClient = mcpClient;
            _exchange = exchange;
            _llmService = llmService;
        }

        // Phương thức này sẽ phân tích yêu cầu của người dùng và quyết định công cụ MCP nào sẽ được sử dụng
        // public async Task<object> ProcessUserRequest(string userQuery)
        // {
        //     try
        //     {
        //         // Phân tích yêu cầu người dùng để xác định loại yêu cầu
        //         var requestType = await AnalyzeRequestType(userQuery);
                
        //         // Dựa vào loại yêu cầu, gọi công cụ MCP tương ứng
        //         // return await ExecuteMcpTool(requestType, userQuery);
        //     }
        //     catch (Exception ex)
        //     {
        //         return new { success = false, error = ex.Message };
        //     }
        // }

        // Phân tích yêu cầu người dùng để xác định sử dụng công cụ nào
        private async Task<string> AnalyzeRequestType(string userQuery)
        {
            // Sử dụng Gemini để phân tích yêu cầu
            try
            {
                var chatClient = _exchange.AsSamplingChatClient();
                
                var messages = new ChatMessage[]
                {
                    new ChatMessage(ChatRole.User, userQuery)
                };
                
                var options = new ChatOptions
                {
                    SystemPrompt = @"Bạn là AI phân tích yêu cầu. Phân loại yêu cầu của người dùng vào một trong các nhóm sau và trả về chỉ một từ khóa tương ứng:
                    - 'product_search': Tìm kiếm sản phẩm
                    - 'product_recommend': Gợi ý sản phẩm 
                    - 'review_analysis': Phân tích đánh giá
                    - 'market_trend': Phân tích xu hướng thị trường
                    - 'content_generation': Tạo nội dung
                    - 'order_status': Kiểm tra trạng thái đơn hàng"
                };
                
                var result = await chatClient.GetResponseAsync(messages, options);
                return result.Trim().ToLower();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini analysis error: {ex.Message}");
                // Fallback to rule-based analysis
            }
            
            // Fallback: Phương pháp đơn giản dựa trên từ khóa nếu Gemini không khả dụng hoặc gặp lỗi
            if (userQuery.Contains("tìm") || userQuery.Contains("kiếm") || userQuery.Contains("có sản phẩm"))
                return "product_search";
            
            if (userQuery.Contains("gợi ý") || userQuery.Contains("đề xuất") || userQuery.Contains("tương tự"))
                return "product_recommend";
                
            if (userQuery.Contains("đánh giá") || userQuery.Contains("nhận xét") || userQuery.Contains("review"))
                return "review_analysis";
                
            if (userQuery.Contains("xu hướng") || userQuery.Contains("trend") || userQuery.Contains("thị trường"))
                return "market_trend";
                
            if (userQuery.Contains("mô tả") || userQuery.Contains("nội dung") || userQuery.Contains("viết"))
                return "content_generation";
                
            if (userQuery.Contains("đơn hàng") || userQuery.Contains("trạng thái") || userQuery.Contains("order"))
                return "order_status";
                
            // Mặc định trả về tìm kiếm sản phẩm
            return "product_search";
        }

        // Thực thi công cụ MCP phù hợp dựa trên phân loại yêu cầu
        // private async Task<object> ExecuteMcpTool(string requestType, string userQuery)
        // {
        //     switch (requestType)
        //     {
        //         case "product_search":
        //             return await _mcpClient.CallTool("searchproducts", new Dictionary<string, object>
        //             {
        //                 { "query", await ExtractSearchQuery(userQuery) }
        //             });

        //         case "product_recommend":
        //             // Phân tích xem đang yêu cầu gợi ý dựa trên sản phẩm hay người dùng
        //             if (userQuery.Contains("tương tự") || userQuery.Contains("giống"))
        //             {
        //                 var productId = ExtractProductId(userQuery);
        //                 return await _mcpClient.CallTool("findsimilarproducts", new Dictionary<string, object>
        //                 {
        //                     { "productId", productId },
        //                     { "limit", 5 }
        //                 });
        //             }
        //             else
        //             {
        //                 var userId = ExtractUserId(userQuery);
        //                 return await _mcpClient.CallTool("recommendforuser", new Dictionary<string, object>
        //                 {
        //                     { "userId", userId },
        //                     { "limit", 5 }
        //                 });
        //             }

        //         case "review_analysis":
        //             var productIdForReview = ExtractProductId(userQuery);
        //             return await _mcpClient.CallTool("analyzeproductreviews", new Dictionary<string, object>
        //             {
        //                 { "productId", productIdForReview }
        //             });

        //         case "market_trend":
        //             return await _mcpClient.CallTool("analyzeproducttrends", new Dictionary<string, object>
        //             {
        //                 { "daysLookback", 30 }
        //             });

        //         case "content_generation":
        //             return await _mcpClient.CallTool("generateproductdescription", new Dictionary<string, object>
        //             {
        //                 { "productId", ExtractProductId(userQuery) }
        //             });

        //         case "order_status":
        //             return await _mcpClient.CallTool("checkorderstatus", new Dictionary<string, object>
        //             {
        //                 { "orderId", ExtractOrderId(userQuery) }
        //             });

        //         default:
        //             // Trường hợp không xác định được yêu cầu cụ thể
        //             return new { message = "Không xác định được yêu cầu. Vui lòng thử lại với câu hỏi cụ thể hơn." };
        //     }
        // }

        // Các phương thức hỗ trợ trích xuất dữ liệu từ câu hỏi
        private async Task<string> ExtractSearchQuery(string userQuery)
        {
            // Sử dụng Gemini để trích xuất từ khóa tìm kiếm
            try
            {
                var chatClient = _exchange.AsSamplingChatClient();
                
                var messages = new ChatMessage[]
                {
                    new ChatMessage(ChatRole.User, $"Trích xuất từ khóa tìm kiếm từ câu hỏi: \"{userQuery}\"")
                };
                
                var options = new ChatOptions
                {
                    SystemPrompt = "Bạn là một chuyên gia trích xuất thông tin. Hãy trích xuất chính xác từ khóa tìm kiếm từ câu hỏi của người dùng. Trả về chỉ các từ khóa, không bao gồm các từ ngữ dùng để hỏi hay yêu cầu."
                };
                
                var extractionResult = await chatClient.GetResponseAsync(messages, options);
                return extractionResult.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini extraction error: {ex.Message}");
                // Fallback to rule-based extraction
            }
            
            // Đơn giản hóa: lấy toàn bộ nội dung sau "tìm" hoặc "kiếm"
            if (userQuery.Contains("tìm "))
                return userQuery.Substring(userQuery.IndexOf("tìm ") + 4);
            
            if (userQuery.Contains("kiếm "))
                return userQuery.Substring(userQuery.IndexOf("kiếm ") + 5);
            
            return userQuery; // Trả về toàn bộ nếu không tìm thấy từ khóa
        }

        private int ExtractProductId(string userQuery)
        {
            // Giả sử: Trong thực tế cần phân tích NLP để tìm ID sản phẩm
            // Ví dụ đơn giản tìm số sau "sản phẩm", "SP", "ID"
            try {
                // Tìm ID sau các từ khóa
                string[] keywords = { "sản phẩm ", "SP", "ID" };
                foreach (var keyword in keywords)
                {
                    if (userQuery.Contains(keyword))
                    {
                        var start = userQuery.IndexOf(keyword) + keyword.Length;
                        var numStr = "";
                        for (int i = start; i < userQuery.Length; i++)
                        {
                            if (char.IsDigit(userQuery[i]))
                                numStr += userQuery[i];
                            else if (numStr.Length > 0)
                                break;
                        }
                        
                        if (int.TryParse(numStr, out int id))
                            return id;
                    }
                }
                
                return 1; // Mặc định trả về ID 1 nếu không tìm thấy
            }
            catch {
                return 1;
            }
        }

        private int ExtractUserId(string userQuery)
        {
            // Tương tự như ExtractProductId nhưng tìm ID người dùng
            // Đơn giản hóa: trả về mặc định ID 1
            return 1;
        }

        private int ExtractOrderId(string userQuery)
        {
            // Tương tự tìm ID đơn hàng
            try {
                string[] keywords = { "đơn hàng ", "đơn số ", "mã đơn " };
                foreach (var keyword in keywords)
                {
                    if (userQuery.Contains(keyword))
                    {
                        var start = userQuery.IndexOf(keyword) + keyword.Length;
                        var numStr = "";
                        for (int i = start; i < userQuery.Length; i++)
                        {
                            if (char.IsDigit(userQuery[i]))
                                numStr += userQuery[i];
                            else if (numStr.Length > 0)
                                break;
                        }
                        
                        if (int.TryParse(numStr, out int id))
                            return id;
                    }
                }
                
                return 1; // Mặc định trả về ID 1 nếu không tìm thấy
            }
            catch {
                return 1;
            }
        }
    }
} 