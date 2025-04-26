using System.Collections.Generic;

namespace API.MCP.Models
{
    /// <summary>
    /// Represents a request to the MCP server
    /// </summary>
    public class McpRequest
    {
        /// <summary>
        /// Gets or sets the request type
        /// </summary>
        public string RequestType { get; set; }

        /// <summary>
        /// Gets or sets the message content
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets additional parameters for the request
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
} 