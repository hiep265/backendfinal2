using System.Threading.Tasks;

namespace API.MCP
{
    public interface IInventoryTool
    {
        Task<string> CheckInventoryAsync(string productName);
    }
} 