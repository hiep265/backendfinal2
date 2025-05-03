using System;
using System.Threading.Tasks;
using System.Net.Http;
using API.MCP.Services;

namespace API.MCP
{
    public class McpClient : IInventoryTool
    {
        private readonly HttpClient _httpClient;
        private readonly InventoryService _inventoryService;

        public McpClient(HttpClient httpClient, InventoryService inventoryService)
        {
            _httpClient = httpClient;
            _inventoryService = inventoryService;
        }

        public async Task<string> CheckInventoryAsync(string productName)
        {
            try
            {
                var stock = await _inventoryService.CheckStockAsync(productName);
                return $"Sản phẩm {productName} còn {stock} cái trong kho.";
            }
            catch (Exception ex)
            {
                return $"Xin lỗi, có lỗi xảy ra khi kiểm tra tồn kho: {ex.Message}";
            }
        }
    }
} 