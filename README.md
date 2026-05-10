# ChatBot 聊天机器人

基于 Microsoft.Extensions 和 IChatClient 接口构建的 C# 聊天机器人，支持多种 LLM 提供商。

## 支持的 LLM 提供商

| 提供商 | Provider 值 | 说明 |
|--------|-------------|------|
| 本地模式 | `local` | 使用本地 HTTP API（如 Ollama、LM Studio） |
| OpenAI | `openai` | OpenAI GPT 系列模型 |
| Anthropic | `anthropic` | Claude 系列模型 |
| Gemini | `gemini` | Google Gemini 模型 |
| 本地关键词 | `simple` | 本地关键词匹配（无需 API） |

## 项目结构

```
ChatBot/
├── ChatBot.csproj           # 项目文件
├── Program.cs               # 应用程序入口
├── IChatClient.cs           # 聊天客户端接口
├── ChatBotSettings.cs       # 机器人配置模型
├── ApiProviderSettings.cs   # API 提供商配置模型
├── SimpleChatClient.cs      # 本地关键词匹配客户端
├── HttpChatClient.cs        # 本地 HTTP API 客户端
├── OpenAIChatClient.cs      # OpenAI API 客户端
├── ClaudeChatClient.cs      # Anthropic Claude API 客户端
├── GeminiChatClient.cs      # Google Gemini API 客户端
├── ChatBotService.cs        # 托管服务
├── appsettings.json         # 配置文件
└── README.md                # 说明文档
```

## 功能特性

- **多 LLM 提供商支持** - OpenAI、Anthropic Claude、Google Gemini、本地 API
- **Microsoft.Extensions.Hosting** - 使用 .NET 通用主机构建应用
- **依赖注入 (DI)** - 服务和依赖自动注入
- **配置系统** - 通过 appsettings.json 配置
- **日志记录** - 使用 ILogger 记录运行信息
- **可扩展接口** - 通过 IChatClient 接口轻松扩展

## 配置说明

### 本地模式 (Ollama/LM Studio)

```json
{
  "LlmProvider": {
    "Provider": "local",
    "ApiKey": "",
    "Model": "llama3.2",
    "BaseUrl": "http://localhost:11434",
    "SystemPrompt": "你是一个友好的聊天助手。",
    "MaxTokens": 1000,
    "Temperature": 0.7
  }
}
```

### OpenAI

```json
{
  "LlmProvider": {
    "Provider": "openai",
    "ApiKey": "sk-your-api-key",
    "Model": "gpt-3.5-turbo",
    "SystemPrompt": "你是一个友好的聊天助手，用中文回答问题。",
    "MaxTokens": 1000,
    "Temperature": 0.7
  }
}
```

### Anthropic Claude

```json
{
  "LlmProvider": {
    "Provider": "anthropic",
    "ApiKey": "sk-ant-your-api-key",
    "Model": "claude-3-5-haiku-20241022",
    "SystemPrompt": "你是一个友好的聊天助手，用中文回答问题。",
    "MaxTokens": 1000,
    "Temperature": 0.7
  }
}
```

### Google Gemini

```json
{
  "LlmProvider": {
    "Provider": "gemini",
    "ApiKey": "your-google-api-key",
    "Model": "gemini-1.5-flash",
    "SystemPrompt": "你是一个友好的聊天助手，用中文回答问题。",
    "MaxTokens": 1000,
    "Temperature": 0.7
  }
}
```

### 本地关键词模式

```json
{
  "LlmProvider": {
    "Provider": "simple"
  }
}
```

## 运行方式

```bash
cd ChatBot
dotnet run
```

## 扩展开发

实现 `IChatClient` 接口创建自定义聊天客户端：

```csharp
public class MyCustomChatClient : IChatClient
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("自定义回复");
    }
}
```

然后在 `Program.cs` 中注册即可。
