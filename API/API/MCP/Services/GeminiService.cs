using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.MCP.Configuration;
using API.MCP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace API.MCP.Services
{
    /// <summary>
    /// Implementation cho Google's Gemini API
    /// </summary>
    public class GeminiService : ILLMService
    {
        private readonly LLMConfig _config;
        private readonly ILogger<GeminiService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _hardcodedUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=AIzaSyCBmGo3mAG5hJQs-KYWL0qQC8YO6yBd5Pg";

        public GeminiService(IOptions<LLMConfig> config, ILogger<GeminiService> logger, HttpClient httpClient)
        {
            _config = config.Value;
            _logger = logger;
            _httpClient = httpClient;

            // Không cần thiết lập BaseAddress vì chúng ta sẽ sử dụng URL đầy đủ
            _logger.LogInformation($"Initialized GeminiService với URL hardcoded: {_hardcodedUrl}");
        }

        /// <inheritdoc />
        public async Task<string> GenerateCompletionAsync(string prompt, string systemPrompt = null)
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage("system", systemPrompt ?? "You are a helpful assistant."),
                new ChatMessage("user", prompt)
            };

            var options = new API.MCP.Models.ChatOptions
            {
                Temperature = _config.Temperature,
                MaxTokens = _config.MaxTokens,
                ModelName = _config.ModelName ?? "gemini-pro"
            };

            return await GenerateCompletionAsync(messages.ToArray(), options);
        }

        /// <inheritdoc />
        public async Task<string> GenerateCompletionAsync(ChatMessage[] messages, API.MCP.Models.ChatOptions options)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu gọi Gemini API với URL và nội dung cố định");

                // Sử dụng cấu trúc request chính xác theo yêu cầu
                var json = @"{
  ""contents"": [{
    ""parts"":[{""text"": ""Explain how AI works""}]
  }]
}";

                _logger.LogDebug($"Request JSON: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _logger.LogInformation($"Gọi Gemini API với URL cố định: {_hardcodedUrl}");
                
                var response = await _httpClient.PostAsync(_hardcodedUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API error: {response.StatusCode}, Details: {errorContent}");
                    throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {errorContent}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Gemini API response: {responseString}");
                
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseString);

                if (responseObject.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var text))
                {
                    _logger.LogInformation("Đã phân tích thành công phản hồi từ Gemini API");
                    return text.GetString();
                }

                _logger.LogError($"Không thể phân tích phản hồi từ Gemini: {responseString}");
                throw new Exception("Không thể phân tích phản hồi từ Gemini");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Gemini API");
                throw;
            }
        }

        /// <summary>
        /// Lấy nội dung tin nhắn người dùng từ mảng tin nhắn
        /// </summary>
        private string GetUserMessageText(ChatMessage[] messages)
        {
            // Ưu tiên lấy tin nhắn của user, nếu không có thì lấy tin nhắn đầu tiên
            foreach (var message in messages)
            {
                if (message.Role.ToLower() == "user")
                {
                    return message.Content;
                }
            }
            
            // Nếu không có tin nhắn user, lấy tin nhắn cuối cùng
            return messages.Length > 0 ? messages[messages.Length - 1].Content : "Explain how AI works";
        }

        /// <inheritdoc />
        public async Task<string> ProcessAsync(LLMRequest request)
        {
            try
            {
                _logger.LogInformation($"Xử lý yêu cầu LLM");
                
                if (request.Messages != null && request.Messages.Count > 0)
                {
                    var options = new API.MCP.Models.ChatOptions
                    {
                        Temperature = _config.Temperature,
                        MaxTokens = _config.MaxTokens,
                        ModelName = _config.ModelName
                    };
                    
                    _logger.LogInformation($"Xử lý yêu cầu với {request.Messages.Count} tin nhắn");
                    return await GenerateCompletionAsync(request.Messages.ToArray(), options);
                }
                else
                {
                    _logger.LogInformation($"Xử lý yêu cầu với prompt: {request.Prompt?.Substring(0, Math.Min(50, request.Prompt?.Length ?? 0))}...");
                    return await GenerateCompletionAsync(request.Prompt, request.SystemPrompt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý yêu cầu Gemini");
                throw;
            }
        }

        private string MapRole(string role)
        {
            return role.ToLower() switch
            {
                "system" => "user", // Gemini không có system role, map thành user
                "user" => "user",
                "assistant" => "model",
                _ => "user"
            };
        }
    }
} 