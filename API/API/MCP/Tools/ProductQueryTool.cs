using System;
using System.Linq;
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
    public class ProductQueryTool
    {
        private readonly DPContext _context;
        
        public ProductQueryTool(DPContext context)
        {
            _context = context;
        }
        
        [McpServerTool, Description("Tìm kiếm sản phẩm theo tên, loại hoặc thương hiệu")]
        public async Task<object> SearchProducts(string query)
        {
            var products = await _context.SanPhams
                .Where(p => p.Ten.Contains(query) || 
                            p.Tag.Contains(query) ||
                            p.Loai.Ten.Contains(query) ||
                            p.NhanHieu.Ten.Contains(query))
                .Select(p => new {
                    p.Id,
                    p.Ten,
                    p.GiaBan,
                    p.MoTa,
                    LoaiSanPham = p.Loai.Ten,
                    ThuongHieu = p.NhanHieu.Ten,
                    HinhAnh = p.ImageSanPhams.FirstOrDefault().ImageName
                })
                .Take(5)
                .ToListAsync();
                
            return products;
        }
        
        [McpServerTool, Description("Kiểm tra trạng thái đơn hàng")]
        public async Task<object> CheckOrderStatus(int orderId)
        {
            var order = await _context.HoaDons
                .Where(h => h.Id == orderId)
                .Select(h => new {
                    h.Id,
                    h.NgayTao,
                    h.TrangThai,
                    h.TongTien
                })
                .FirstOrDefaultAsync();
                
            return order;
        }
    }
} 