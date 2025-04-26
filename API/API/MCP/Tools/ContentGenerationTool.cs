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
    public class ContentGenerationTool
    {
        private readonly DPContext _context;
        private readonly IMcpServerExchange _exchange;
        
        public ContentGenerationTool(DPContext context, IMcpServerExchange exchange)
        {
            _context = context;
            _exchange = exchange;
        }
        
        [McpServerTool, Description("Tạo mô tả sản phẩm tự động")]
        public async Task<object> GenerateProductDescription(int productId, string style = "marketing")
        {
            var product = await _context.SanPhams
                .Include(p => p.Loai)
                .Include(p => p.NhanHieu)
                .Where(p => p.Id == productId)
                .FirstOrDefaultAsync();
                
            if (product == null)
                return new { error = "Không tìm thấy sản phẩm" };
                
            // Sử dụng LLM để tạo mô tả
            string stylePrompt = "";
            switch (style.ToLower())
            {
                case "marketing":
                    stylePrompt = "hấp dẫn và thu hút khách hàng";
                    break;
                case "technical":
                    stylePrompt = "chi tiết kỹ thuật và thông số";
                    break;
                case "seo":
                    stylePrompt = "tối ưu hóa SEO với từ khóa liên quan";
                    break;
                default:
                    stylePrompt = "hấp dẫn và thu hút khách hàng";
                    break;
            }
            
            var prompt = $"Tạo mô tả sản phẩm {stylePrompt} cho '{product.Ten}', " +
                         $"là sản phẩm thuộc loại '{product.Loai?.Ten}', " +
                         $"thương hiệu '{product.NhanHieu?.Ten}'. " +
                         $"Giá bán: {product.GiaBan} VNĐ. " +
                         $"Thông tin thêm: {product.ThanhPhan}";
                         
            // Kiểm tra xem LLM có sẵn không
            if (_exchange.GetClientCapabilities()?.Sampling != null)
            {
                try {
                    var descriptionRequest = new ChatMessage[]
                    {
                        new ChatMessage(ChatRole.User, prompt)
                    };
                    
                    var result = await _exchange.AsSamplingChatClient().GetResponseAsync(
                        descriptionRequest,
                        new ChatOptions 
                        {
                            SystemPrompt = "Bạn là một chuyên gia viết mô tả sản phẩm thời trang. Hãy tạo mô tả hấp dẫn, thu hút và đầy đủ thông tin."
                        });
                    
                    return new { 
                        id = productId,
                        description = result,
                        generated = true 
                    };
                }
                catch (Exception ex)
                {
                    return new { 
                        id = productId,
                        error = $"Lỗi khi tạo mô tả: {ex.Message}",
                        generated = false 
                    };
                }
            }
            else
            {
                // Fallback khi không có LLM
                return new { 
                    id = productId,
                    description = $"Mô tả cho sản phẩm {product.Ten}. Giá: {product.GiaBan} VNĐ.",
                    generated = false,
                    reason = "LLM không khả dụng"
                };
            }
        }
        
        [McpServerTool, Description("Tạo nội dung SEO đa ngôn ngữ")]
        public async Task<object> GenerateSeoContent(int productId, string language = "vi")
        {
            var product = await _context.SanPhams
                .Include(p => p.Loai)
                .Include(p => p.NhanHieu)
                .Where(p => p.Id == productId)
                .FirstOrDefaultAsync();
                
            if (product == null)
                return new { error = "Không tìm thấy sản phẩm" };
                
            // Kiểm tra xem LLM có sẵn không
            if (_exchange.GetClientCapabilities()?.Sampling != null)
            {
                string langPrompt = language.ToLower() switch {
                    "en" => "English",
                    "fr" => "French",
                    "zh" => "Chinese",
                    "ja" => "Japanese",
                    _ => "Vietnamese"
                };
                
                var seoPrompt = $"Tạo nội dung SEO bằng tiếng {langPrompt} cho sản phẩm thời trang '{product.Ten}', " +
                             $"thuộc loại '{product.Loai?.Ten}', thương hiệu '{product.NhanHieu?.Ten}'. " +
                             $"Sản phẩm có giá {product.GiaBan} VNĐ. " +
                             $"Thông tin chi tiết: {product.MoTa}";
                
                try {
                    var seoRequest = new ChatMessage[]
                    {
                        new ChatMessage(ChatRole.User, seoPrompt)
                    };
                    
                    var result = await _exchange.AsSamplingChatClient().GetResponseAsync(
                        seoRequest,
                        new ChatOptions
                        {
                            SystemPrompt = $"Bạn là một chuyên gia SEO trong lĩnh vực thời trang. Hãy tạo nội dung SEO bằng tiếng {langPrompt} với các từ khóa phù hợp, metadata, và mô tả tối ưu cho công cụ tìm kiếm."
                        });
                    
                    return new { 
                        id = productId,
                        content = result,
                        language = language,
                        generated = true 
                    };
                }
                catch (Exception ex)
                {
                    return new { 
                        id = productId,
                        error = $"Lỗi khi tạo nội dung SEO: {ex.Message}",
                        generated = false 
                    };
                }
            }
            else
            {
                // Fallback khi không có LLM
                return new { 
                    id = productId,
                    content = $"SEO content for {product.Ten}",
                    language = language,
                    generated = false,
                    reason = "LLM không khả dụng"
                };
            }
        }
    }
} 