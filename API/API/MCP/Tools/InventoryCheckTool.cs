using System.Threading.Tasks;
using ModelContextProtocol.Server;
using System.ComponentModel;
using API.MCP.Services;

namespace API.MCP.Tools
{
    [McpServerToolType]
    public class InventoryCheckTool
    {
        private readonly InventoryService _inventoryService;

        public InventoryCheckTool(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [McpServerTool, Description("Kiểm tra tồn kho sản phẩm theo tên.")]
        public async Task<string> CheckInventory(string productName)
        {
            int quantity = await _inventoryService.CheckStockAsync(productName);
            return quantity > 0
                ? $"Còn {quantity} sản phẩm {productName}"
                : $"Hết hàng {productName}";
        }
    }
} 