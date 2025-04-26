using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using API.MCP.Configuration;
using API.MCP.Services;
using Microsoft.Extensions.Options;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LLMConfigController : ControllerBase
    {
        private readonly LLMConfig _llmConfig;
        private readonly IConfiguration _configuration;
        private readonly ILLMService _llmService;
        private readonly ILogger<LLMConfigController> _logger;

        public LLMConfigController(
            LLMConfig llmConfig, 
            IConfiguration configuration,
            ILLMService llmService,
            ILogger<LLMConfigController> logger)
        {
            _llmConfig = llmConfig;
            _configuration = configuration;
            _llmService = llmService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetConfig()
        {
            // Return the current LLM configuration, but hide API key for security
            var configToReturn = new
            {
                _llmConfig.Provider,
                ApiKey = "***" + (_llmConfig.ApiKey?.Substring(Math.Max(0, _llmConfig.ApiKey.Length - 4)) ?? ""),
                _llmConfig.BaseUrl,
                _llmConfig.ModelName,
                _llmConfig.Temperature,
                _llmConfig.MaxTokens,
                _llmConfig.TimeoutSeconds
            };
            
            return Ok(configToReturn);
        }

        [HttpGet("gemini")]
        public IActionResult GetGeminiConfig()
        {
            // Return Gemini configuration
            var geminiConfig = _configuration.GetSection("LLM").Get<object>();
            return Ok(geminiConfig);
        }

        [HttpPost("test")]
        [Consumes("application/json")]
        public async Task<IActionResult> TestConfig([FromBody] TestPromptRequest request)
        {
            try
            {
                _logger.LogInformation($"Testing LLM with prompt: {request?.Prompt}");
                
                // Nếu request null, tạo một mặc định
                if (request == null)
                {
                    request = new TestPromptRequest();
                }
                
                // Test the current LLM configuration with a simple prompt
                var response = await _llmService.GenerateCompletionAsync(
                    request.Prompt, 
                    request.SystemPrompt);
                
                return Ok(new { success = true, response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing LLM configuration");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        
        [HttpGet("quicktest")]
        public async Task<IActionResult> QuickTest()
        {
            try
            {
                _logger.LogInformation("Performing quick test of LLM service");
                
                // Test with a default prompt
                var response = await _llmService.GenerateCompletionAsync(
                    "Tell me about yourself in one short sentence.", 
                    "You are a helpful AI assistant.");
                
                return Ok(new { success = true, response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick test of LLM service");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    public class TestPromptRequest
    {
        public string Prompt { get; set; } = "Tell me about yourself in one short sentence.";
        public string SystemPrompt { get; set; }
    }
} 