using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace API.MCP.Tools
{
    [McpServerToolType]
    public class ProductRecommendationTool
    {
        private readonly DPContext _context;
        private readonly IMcpServerExchange _exchange;
        
        public ProductRecommendationTool(DPContext context, IMcpServerExchange exchange)
        {
            _context = context;
            _exchange = exchange;
        }
        
        [McpServerTool, Description("Gợi ý sản phẩm cho người dùng dựa trên lịch sử mua hàng")]
        public async Task<object> RecommendProducts(string userId, int limit = 5)
        {
            // Lấy lịch sử mua hàng của người dùng
            var purchaseHistory = await _context.HoaDons
                .Where(h => h.Id_User == userId)
                .Join(_context.ChiTietHoaDons,
                    hd => hd.Id,
                    ct => ct.Id_HoaDon,
                    (hd, ct) => new { hd, ct })
                .Select(x => new {
                    OrderDate = x.hd.NgayTao,
                    ProductId = x.ct.Id_SanPham
                })
                .ToListAsync();
                
            if (purchaseHistory == null || !purchaseHistory.Any())
            {
                // Nếu không có lịch sử, trả về các sản phẩm bán chạy
                var topProducts = await _context.SanPhams
                    .OrderByDescending(p => p.SanPhamBienThes.Count())
                    .Take(limit)
                    .Select(p => new {
                        p.Id,
                        p.Ten,
                        p.GiaBan,
                        HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName,
                        p.MoTa
                    })
                    .ToListAsync();
                    
                return new {
                    userId,
                    recommendationType = "trending",
                    products = topProducts,
                    note = "Gợi ý dựa trên sản phẩm bán chạy do người dùng chưa có lịch sử mua hàng"
                };
            }
            
            // Nếu có LLM, sử dụng để cá nhân hóa đề xuất
            if (_exchange.GetClientCapabilities()?.Sampling != null)
            {
                try
                {
                    // Lấy sản phẩm đã mua
                    var purchasedProductIds = purchaseHistory.Select(p => p.ProductId).Distinct().ToList();
                    
                    // Lấy thông tin chi tiết về các sản phẩm đã mua
                    var purchasedProducts = await _context.SanPhams
                        .Where(p => purchasedProductIds.Contains(p.Id))
                        .Select(p => new {
                            p.Id,
                            p.Ten,
                            CategoryId = p.Id_Loai,
                            p.GiaBan,
                            BrandName = p.NhanHieu.Ten,
                            p.MoTa
                        })
                        .ToListAsync();
                        
                    // Lấy danh mục loại sản phẩm
                    var categoryIds = purchasedProducts
                        .Select(p => p.CategoryId)
                        .Distinct()
                        .ToList();
                        
                    // Lấy tất cả sản phẩm có thể đề xuất (khác với đã mua)
                    var potentialRecommendations = await _context.SanPhams
                        .Where(p => !purchasedProductIds.Contains(p.Id) && categoryIds.Contains(p.Id_Loai))
                        .Select(p => new {
                            p.Id,
                            p.Ten,
                            CategoryId = p.Id_Loai,
                            p.GiaBan,
                            BrandName = p.NhanHieu.Ten,
                            p.MoTa,
                            HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName
                        })
                        .ToListAsync();
                        
                    if (!potentialRecommendations.Any())
                    {
                        // Nếu không có sản phẩm tiềm năng, mở rộng tìm kiếm không giới hạn danh mục
                        potentialRecommendations = await _context.SanPhams
                            .Where(p => !purchasedProductIds.Contains(p.Id))
                            .Select(p => new {
                                p.Id,
                                p.Ten,
                                CategoryId = p.Id_Loai,
                                p.GiaBan,
                                BrandName = p.NhanHieu.Ten,
                                p.MoTa,
                                HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName
                            })
                            .Take(20)
                            .ToListAsync();
                    }
                    
                    // Chuẩn bị dữ liệu để gửi tới LLM
                    var requestData = new {
                        purchasedProducts,
                        potentialRecommendations,
                        limit
                    };
                    
                    string requestJson = JsonSerializer.Serialize(requestData);
                    
                    var recommendationRequest = new ChatMessage[]
                    {
                        new ChatMessage(ChatRole.User, $"Gợi ý sản phẩm dựa trên lịch sử mua hàng:\n\n{requestJson}")
                    };
                    
                    var llmResult = await _exchange.AsSamplingChatClient().GetResponseAsync(
                        recommendationRequest,
                        new ChatOptions
                        {
                            SystemPrompt = "Bạn là một hệ thống gợi ý sản phẩm thông minh. Dựa trên lịch sử mua hàng của khách hàng, hãy gợi ý các sản phẩm phù hợp từ danh sách sản phẩm tiềm năng. Trả về JSON có cấu trúc: { \"recommendedProductIds\": [id1, id2, ...], \"reasoning\": \"lý do gợi ý\" }"
                        });
                    
                    // Phân tích kết quả từ LLM
                    try
                    {
                        var llmRecommendations = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(llmResult);
                        
                        if (llmRecommendations.TryGetValue("recommendedProductIds", out var productIdsElement) && productIdsElement.ValueKind == JsonValueKind.Array)
                        {
                            var recommendedIds = new List<int>();
                            foreach (var element in productIdsElement.EnumerateArray())
                            {
                                if (element.TryGetInt32(out var id))
                                {
                                    recommendedIds.Add(id);
                                }
                            }
                            
                            string reasoning = "";
                            if (llmRecommendations.TryGetValue("reasoning", out var reasoningElement) && reasoningElement.ValueKind == JsonValueKind.String)
                            {
                                reasoning = reasoningElement.GetString();
                            }
                            
                            // Lấy thông tin chi tiết về các sản phẩm được đề xuất
                            var recommendedProducts = await _context.SanPhams
                                .Where(p => recommendedIds.Contains(p.Id))
                                .Select(p => new {
                                    p.Id,
                                    p.Ten,
                                    p.GiaBan,
                                    HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName,
                                    p.MoTa,
                                    ThuongHieu = p.NhanHieu.Ten
                                })
                                .ToListAsync();
                                
                            return new {
                                userId,
                                recommendationType = "personalized",
                                products = recommendedProducts,
                                reasoning,
                                count = recommendedProducts.Count()
                            };
                        }
                    }
                    catch
                    {
                        // Fallback nếu không thể phân tích kết quả từ LLM
                    }
                }
                catch (Exception ex)
                {
                    return new {
                        userId, 
                        error = $"Lỗi khi tạo gợi ý: {ex.Message}"
                    };
                }
            }
            
            // Fallback: Dựa trên thuật toán đơn giản khi không có LLM hoặc LLM thất bại
            var categories = await _context.HoaDons
                .Where(h => h.Id_User == userId)
                .Join(_context.ChiTietHoaDons,
                    hd => hd.Id,
                    ct => ct.Id_HoaDon,
                    (hd, ct) => new { hd, ct })
                .Join(_context.SanPhams,
                    x => x.ct.Id_SanPham,
                    p => p.Id,
                    (x, p) => new { x.hd, x.ct, p })
                .Select(x => x.p.Id_Loai)
                .Distinct()
                .ToListAsync();
                
            var purchasedIds = purchaseHistory.Select(p => p.ProductId).Distinct().ToList();
            
            var recommendations = await _context.SanPhams
                .Where(p => !purchasedIds.Contains(p.Id) && categories.Contains(p.Id_Loai))
                .OrderByDescending(p => p.SanPhamBienThes.Count())
                .Take(limit)
                .Select(p => new {
                    p.Id,
                    p.Ten,
                    p.GiaBan,
                    HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName,
                    p.MoTa,
                    ThuongHieu = p.NhanHieu.Ten
                })
                .ToListAsync();
                
            if (recommendations.Count() < limit)
            {
                // Nếu không đủ đề xuất, bổ sung với sản phẩm bán chạy
                var additionalCount = limit - recommendations.Count();
                var additionalProducts = await _context.SanPhams
                    .Where(p => !purchasedIds.Contains(p.Id) && !recommendations.Any(r => r.Id == p.Id))
                    .OrderByDescending(p => p.SanPhamBienThes.Count())
                    .Take(additionalCount)
                    .Select(p => new {
                        p.Id,
                        p.Ten,
                        p.GiaBan,
                        HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName,
                        p.MoTa,
                        ThuongHieu = p.NhanHieu.Ten
                    })
                    .ToListAsync();
                    
                recommendations = recommendations.Concat(additionalProducts).ToList();
            }
            
            return new {
                userId,
                recommendationType = "category-based",
                products = recommendations,
                note = "Gợi ý dựa trên danh mục sản phẩm đã mua trước đây"
            };
        }
        
        [McpServerTool, Description("Gợi ý sản phẩm tương tự với sản phẩm đang xem")]
        public async Task<object> RecommendSimilarProducts(int productId, int limit = 5)
        {
            // Lấy thông tin sản phẩm hiện tại
            var currentProduct = await _context.SanPhams
                .Where(p => p.Id == productId)
                .Select(p => new {
                    p.Id,
                    p.Ten,
                    CategoryId = p.Id_Loai,
                    p.GiaBan,
                    BrandName = p.NhanHieu.Ten,
                    p.MoTa
                })
                .FirstOrDefaultAsync();
                
            if (currentProduct == null)
                return new { error = "Không tìm thấy sản phẩm" };
                
            // Nếu có LLM, sử dụng để tìm sản phẩm tương tự
            if (_exchange.GetClientCapabilities()?.Sampling != null)
            {
                try
                {
                    // Lấy các sản phẩm cùng danh mục
                    var similarProducts = await _context.SanPhams
                        .Where(p => p.Id != productId && p.Id_Loai == currentProduct.CategoryId)
                        .Select(p => new {
                            p.Id,
                            p.Ten,
                            CategoryId = p.Id_Loai,
                            p.GiaBan,
                            BrandName = p.NhanHieu.Ten,
                            p.MoTa,
                            HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName
                        })
                        .Take(20) // Lấy một số lượng sản phẩm để LLM chọn
                        .ToListAsync();
                        
                    if (!similarProducts.Any())
                    {
                        // Nếu không có sản phẩm cùng danh mục, mở rộng tìm kiếm
                        similarProducts = await _context.SanPhams
                            .Where(p => p.Id != productId)
                            .OrderByDescending(p => p.SanPhamBienThes.Count())
                            .Take(20)
                            .Select(p => new {
                                p.Id,
                                p.Ten,
                                CategoryId = p.Id_Loai,
                                p.GiaBan,
                                BrandName = p.NhanHieu.Ten,
                                p.MoTa,
                                HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName
                            })
                            .ToListAsync();
                    }
                    
                    // Chuẩn bị dữ liệu để gửi tới LLM
                    var requestData = new {
                        currentProduct,
                        potentialSimilarProducts = similarProducts,
                        limit
                    };
                    
                    string requestJson = JsonSerializer.Serialize(requestData);
                    
                    var similarProductRequest = new ChatMessage[]
                    {
                        new ChatMessage(ChatRole.User, $"Gợi ý sản phẩm tương tự với sản phẩm tham chiếu:\n\n{requestJson}")
                    };
                    
                    var llmResult = await _exchange.AsSamplingChatClient().GetResponseAsync(
                        similarProductRequest,
                        new ChatOptions
                        {
                            SystemPrompt = "Bạn là một hệ thống gợi ý sản phẩm thông minh. Dựa trên sản phẩm tham chiếu, hãy gợi ý các sản phẩm tương tự từ danh sách danh mục. Trả về JSON có cấu trúc: { \"similarProductIds\": [id1, id2, ...], \"reasoning\": \"lý do gợi ý\" }"
                        });
                    
                    // Phân tích kết quả từ LLM
                    try
                    {
                        var llmRecommendations = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(llmResult);
                        
                        if (llmRecommendations.TryGetValue("similarProductIds", out var productIdsElement) && productIdsElement.ValueKind == JsonValueKind.Array)
                        {
                            var similarIds = new List<int>();
                            foreach (var element in productIdsElement.EnumerateArray())
                            {
                                if (element.TryGetInt32(out var id))
                                {
                                    similarIds.Add(id);
                                }
                            }
                            
                            string reasoning = "";
                            if (llmRecommendations.TryGetValue("reasoning", out var reasoningElement) && reasoningElement.ValueKind == JsonValueKind.String)
                            {
                                reasoning = reasoningElement.GetString();
                            }
                            
                            // Lấy thông tin chi tiết về các sản phẩm tương tự
                            var recommendedSimilar = await _context.SanPhams
                                .Where(p => similarIds.Contains(p.Id))
                                .Select(p => new {
                                    p.Id,
                                    p.Ten,
                                    p.GiaBan,
                                    HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName,
                                    p.MoTa,
                                    ThuongHieu = p.NhanHieu.Ten
                                })
                                .ToListAsync();
                                
                            return new {
                                productId,
                                recommendationType = "ai-similarity",
                                products = recommendedSimilar,
                                reasoning,
                                count = recommendedSimilar.Count()
                            };
                        }
                    }
                    catch
                    {
                        // Fallback nếu không thể phân tích kết quả từ LLM
                    }
                }
                catch (Exception ex)
                {
                    return new {
                        productId, 
                        error = $"Lỗi khi tìm sản phẩm tương tự: {ex.Message}"
                    };
                }
            }
            
            // Fallback: Tìm sản phẩm tương tự dựa trên danh mục và giá
            var similarPriceRange = currentProduct.GiaBan * 0.3m; // ±30% giá
            var lowerPrice = currentProduct.GiaBan - similarPriceRange;
            var upperPrice = currentProduct.GiaBan + similarPriceRange;
            
            var fallbackSimilar = await _context.SanPhams
                .Where(p => p.Id != productId && 
                       p.Id_Loai == currentProduct.CategoryId &&
                       p.GiaBan >= lowerPrice && p.GiaBan <= upperPrice)
                .OrderByDescending(p => p.SanPhamBienThes.Count())
                .Take(limit)
                .Select(p => new {
                    p.Id,
                    p.Ten,
                    p.GiaBan,
                    HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName,
                    p.MoTa,
                    ThuongHieu = p.NhanHieu.Ten
                })
                .ToListAsync();
                
            if (fallbackSimilar.Count() < limit)
            {
                // Nếu không đủ sản phẩm, mở rộng tìm kiếm chỉ dựa trên danh mục
                var additionalCount = limit - fallbackSimilar.Count();
                var additionalSimilar = await _context.SanPhams
                    .Where(p => p.Id != productId && 
                           p.Id_Loai == currentProduct.CategoryId &&
                           !fallbackSimilar.Any(s => s.Id == p.Id))
                    .OrderByDescending(p => p.SanPhamBienThes.Count())
                    .Take(additionalCount)
                    .Select(p => new {
                        p.Id,
                        p.Ten,
                        p.GiaBan,
                        HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName,
                        p.MoTa,
                        ThuongHieu = p.NhanHieu.Ten
                    })
                    .ToListAsync();
                    
                fallbackSimilar = fallbackSimilar.Concat(additionalSimilar).ToList();
            }
            
            return new {
                productId,
                recommendationType = "category-price-based",
                products = fallbackSimilar,
                note = "Gợi ý dựa trên danh mục và mức giá tương tự"
            };
        }
    }
} 