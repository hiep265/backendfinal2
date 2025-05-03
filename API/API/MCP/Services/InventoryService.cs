using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using API.Data;
using System.Linq;
using API.Models;
using Microsoft.Extensions.Logging;

namespace API.MCP.Services
{
    public class InventoryService
    {
        private readonly DPContext _dbContext;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(DPContext dbContext, ILogger<InventoryService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<int> CheckStockAsync(string productName)
        {
            try
            {
                _logger.LogInformation($"Đang tìm kiếm sản phẩm có tên chứa: {productName}");

                // Tìm sản phẩm theo tên
                var product = await _dbContext.SanPhams
                    .AsNoTracking()
                    .Include(p => p.SanPhamBienThes)
                    .Where(p => p.Ten.ToLower().Contains(productName.ToLower()))
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    _logger.LogWarning($"Không tìm thấy sản phẩm có tên chứa: {productName}");
                    throw new Exception($"Không tìm thấy sản phẩm có tên chứa '{productName}'");
                }

                _logger.LogInformation($"Đã tìm thấy sản phẩm: {product.Ten}");

                // Tính tổng số lượng tồn kho từ các biến thể
                var totalStock = product.SanPhamBienThes?
                    .Where(b => b.SoLuongTon > 0)
                    .Sum(b => b.SoLuongTon) ?? 0;

                _logger.LogInformation($"Tổng số lượng tồn kho: {totalStock}");

                return totalStock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi kiểm tra tồn kho cho sản phẩm: {productName}");
                throw new Exception($"Lỗi khi kiểm tra tồn kho: {ex.Message}");
            }
        }
    }
} 