# API for E-Fashion Shop

This API provides backend services for the E-Fashion e-commerce platform, including AI-powered features via the Model Context Protocol (MCP).

## AI Features Configuration

The application supports multiple LLM providers including OpenAI, Google's Gemini, and Anthropic's Claude. You can configure which provider to use in the `appsettings.json` file.

### LLM Configuration

In `appsettings.json`, you can configure the LLM provider under the `LLMConfig` section:

```json
"LLMConfig": {
    "Provider": "OpenAI",  // Options: "OpenAI", "Gemini", "Claude"
    "ApiKey": "your-api-key-here",
    "BaseUrl": "https://api.openai.com",
    "ModelName": "gpt-3.5-turbo",
    "Temperature": 0.7,
    "MaxTokens": 1000,
    "TimeoutSeconds": 30
}
```

### Provider-Specific Configuration Examples

#### OpenAI

```json
"LLMConfig": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-openai-api-key",
    "BaseUrl": "https://api.openai.com",
    "ModelName": "gpt-4o",
    "Temperature": 0.7,
    "MaxTokens": 1000,
    "TimeoutSeconds": 30
}
```

#### Google Gemini

```json
"LLMConfig": {
    "Provider": "Gemini",
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "BaseUrl": "https://generativelanguage.googleapis.com",
    "ModelName": "gemini-2.0-flash",
    "Temperature": 0.7,
    "MaxTokens": 1000,
    "TimeoutSeconds": 30
}
```

#### Anthropic Claude

```json
"LLMConfig": {
    "Provider": "Claude",
    "ApiKey": "your-claude-api-key",
    "BaseUrl": "https://api.anthropic.com",
    "ModelName": "claude-3-opus-20240229",
    "Temperature": 0.7,
    "MaxTokens": 1000,
    "TimeoutSeconds": 30
}
```

### Testing LLM Configuration

You can test your LLM configuration using the `/api/LLMConfig/test` endpoint:

```bash
curl -X POST "https://localhost:44302/api/LLMConfig/test" \
     -H "Content-Type: application/json" \
     -d '{"prompt": "Explain AI in one sentence"}'
```

## API Endpoints

### AI Features

- `POST /api/ai/query` - Process an AI query
- `GET /api/ai/help` - Get help information about AI capabilities
- `GET /api/LLMConfig` - Get current LLM configuration
- `GET /api/LLMConfig/examples` - Get examples of LLM configurations
- `POST /api/LLMConfig/test` - Test the current LLM configuration

## Environment Setup

1. Configure your LLM provider in `appsettings.json`
2. Ensure you have a valid API key for your chosen provider
3. Start the API with `dotnet run` 