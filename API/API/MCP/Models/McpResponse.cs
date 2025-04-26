using System.Collections.Generic;

namespace API.MCP.Models
{
    /// <summary>
    /// Represents a response from the MCP server
    /// </summary>
    public class McpResponse
    {
        /// <summary>
        /// Gets or sets whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the response message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the response data
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets additional information about the response
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
} 