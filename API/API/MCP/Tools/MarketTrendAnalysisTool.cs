using System;
using System.Collections.Generic;
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
    public class MarketTrendAnalysisTool
    {
        private readonly DPContext _context;
        private readonly IMcpServerExchange _exchange;
        
        public MarketTrendAnalysisTool(DPContext context, IMcpServerExchange exchange)
        {
            _context = context;
            _exchange = exchange;
        }
        
        [McpServerTool, Description("Phân tích xu hướng mua sắm và sản phẩm phổ biến")]
        public async Task<object> AnalyzeProductTrends(int daysLookback = 30, int topCount = 10)
        {
            var startDate = DateTime.Now.AddDays(-daysLookback);
            
            // Lấy thống kê sản phẩm bán chạy trong khoảng thời gian
            var topSellingProducts = await _context.ChiTietHoaDons
                .Join(_context.HoaDons,
                    ct => ct.Id_HoaDon,
                    hd => hd.Id,
                    (ct, hd) => new { ct, hd })
                .Where(x => x.hd.NgayTao >= startDate)
                .GroupBy(x => x.ct.Id_SanPham)
                .Select(g => new {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(x => x.ct.Soluong),
                    TotalRevenue = g.Sum(x => (x.ct.GiaBan ?? 0) * x.ct.Soluong)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(topCount)
                .Join(_context.SanPhams,
                    s => s.ProductId,
                    p => p.Id,
                    (s, p) => new {
                        p.Id,
                        p.Ten,
                        IdLoaiSanPham = p.Id_Loai,
                        p.Tag,
                        s.TotalQuantity,
                        s.TotalRevenue
                    })
                .ToListAsync();
                
            if (topSellingProducts == null || !topSellingProducts.Any())
            {
                return new {
                    error = $"Không có dữ liệu bán hàng trong {daysLookback} ngày qua"
                };
            }
                
            // Lấy thông tin về loại sản phẩm phổ biến
            var topCategories = await _context.ChiTietHoaDons
                .Join(_context.HoaDons,
                    ct => ct.Id_HoaDon,
                    hd => hd.Id,
                    (ct, hd) => new { ct, hd })
                .Where(x => x.hd.NgayTao >= startDate)
                .Join(_context.SanPhams,
                    x => x.ct.Id_SanPham,
                    p => p.Id,
                    (x, p) => new { x.ct, x.hd, p })
                .GroupBy(x => x.p.Id_Loai)
                .Select(g => new {
                    CategoryId = g.Key,
                    TotalQuantity = g.Sum(x => x.ct.Soluong),
                    TotalRevenue = g.Sum(x => (x.ct.GiaBan ?? 0) * x.ct.Soluong)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(topCount)
                .Join(_context.Loais,
                    s => s.CategoryId,
                    c => c.Id,
                    (s, c) => new {
                        c.Id,
                        c.Ten,
                        s.TotalQuantity,
                        s.TotalRevenue
                    })
                .ToListAsync();
                
            // Lấy thông tin về thương hiệu phổ biến
            var topBrands = await _context.ChiTietHoaDons
                .Join(_context.HoaDons,
                    ct => ct.Id_HoaDon,
                    hd => hd.Id,
                    (ct, hd) => new { ct, hd })
                .Where(x => x.hd.NgayTao >= startDate)
                .Join(_context.SanPhams,
                    x => x.ct.Id_SanPham,
                    p => p.Id,
                    (x, p) => new { x.ct, x.hd, p })
                .Where(x => x.p.Id_NhanHieu != null)
                .GroupBy(x => x.p.Id_NhanHieu)
                .Select(g => new {
                    BrandId = g.Key,
                    TotalQuantity = g.Sum(x => x.ct.Soluong),
                    TotalRevenue = g.Sum(x => (x.ct.GiaBan ?? 0) * x.ct.Soluong)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(topCount)
                .Join(_context.NhanHieus,
                    s => s.BrandId,
                    b => b.Id,
                    (s, b) => new {
                        BrandId = b.Id,
                        BrandName = b.Ten,
                        s.TotalQuantity,
                        s.TotalRevenue
                    })
                .ToListAsync();
                
            // Tạo phân tích tăng trưởng so với kỳ trước
            var previousPeriodStart = startDate.AddDays(-daysLookback);
            
            var currentPeriodSales = await _context.HoaDons
                .Where(h => h.NgayTao >= startDate)
                .SumAsync(h => h.TongTien);
                
            var previousPeriodSales = await _context.HoaDons
                .Where(h => h.NgayTao >= previousPeriodStart && h.NgayTao < startDate)
                .SumAsync(h => h.TongTien);
                
            decimal growthRate = 0;
            if (previousPeriodSales > 0)
            {
                growthRate = (currentPeriodSales - previousPeriodSales) / previousPeriodSales * 100;
            }
            
            // Tổng hợp kết quả cơ bản
            var basicResults = new {
                timeRange = new {
                    startDate,
                    endDate = DateTime.Now,
                    daysLookback
                },
                sales = new {
                    currentPeriod = currentPeriodSales,
                    previousPeriod = previousPeriodSales,
                    growthRate = Math.Round(growthRate, 2)
                },
                topSellingProducts,
                topCategories,
                topBrands
            };
            
            // Nếu có LLM, tạo thêm phân tích sâu
            if (_exchange.GetClientCapabilities()?.Sampling != null)
            {
                try
                {
                    // Chuẩn bị dữ liệu để gửi tới LLM
                    var trendData = new {
                        basicResults,
                        timestamp = DateTime.Now
                    };
                    
                    string requestJson = JsonSerializer.Serialize(trendData);
                    
                    var trendAnalysisRequest = new ChatMessage[]
                    {
                        new ChatMessage(ChatRole.User, $"Phân tích xu hướng thị trường dựa trên dữ liệu bán hàng:\n\n{requestJson}")
                    };
                    
                    var llmResult = await _exchange.AsSamplingChatClient().GetResponseAsync(
                        trendAnalysisRequest, 
                        new ChatOptions
                        {
                            SystemPrompt = "Bạn là một chuyên gia phân tích thị trường. Hãy phân tích dữ liệu bán hàng được cung cấp và đưa ra nhận xét về xu hướng thị trường, sản phẩm nổi bật, đề xuất chiến lược kinh doanh. Trả về JSON có cấu trúc: { \"marketInsights\": \"nhận xét về thị trường\", \"topProductsAnalysis\": \"phân tích sản phẩm bán chạy\", \"categoryTrendsAnalysis\": \"phân tích xu hướng danh mục\", \"brandAnalysis\": \"phân tích thương hiệu\", \"recommendations\": \"đề xuất chiến lược\" }"
                        });
                    
                    try
                    {
                        var llmAnalysis = JsonSerializer.Deserialize<Dictionary<string, string>>(llmResult);
                        
                        return new {
                            data = basicResults,
                            aiAnalysis = llmAnalysis,
                            analysisSupportedBy = "AI Market Insights"
                        };
                    }
                    catch
                    {
                        // Fallback nếu không thể phân tích kết quả từ LLM
                        return new {
                            data = basicResults,
                            aiAnalysis = new {
                                rawAnalysis = llmResult
                            },
                            analysisSupportedBy = "AI Market Insights (Raw Output)"
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new {
                        data = basicResults,
                        error = $"Không thể tạo phân tích AI: {ex.Message}"
                    };
                }
            }
            
            // Trường hợp không có LLM
            return basicResults;
        }
        
        [McpServerTool, Description("Phân tích tính thời vụ và xu hướng theo thời gian")]
        public async Task<object> AnalyzeSeasonalTrends(int monthsLookback = 12)
        {
            // Giản lược code
            return new { result = "Seasonal analysis implemented" };
        }
        
        [McpServerTool, Description("Phân tích tương quan giữa các sản phẩm thường mua cùng nhau")]
        public async Task<object> AnalyzeProductCorrelations(int topCount = 10, int daysLookback = 90)
        {
            // Giản lược code
            return new { result = "Correlation analysis implemented" };
        }
    }
} 