using ChatBot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<ChatBotSettings>(context.Configuration.GetSection("ChatBot"));
        services.Configure<ApiProviderSettings>(context.Configuration.GetSection("LlmProvider"));

        services.AddHttpClient("Claude");
        services.AddHttpClient("Gemini");
        services.AddHttpClient("OpenAI");

        var provider = context.Configuration.GetValue<string>("LlmProvider:Provider") ?? "local";

        switch (provider.ToLower())
        {
            case "openai":
                services.AddSingleton<IChatClient, OpenAIChatClient>();
                break;
            case "anthropic":
                services.AddSingleton<IChatClient, ClaudeChatClient>();
                break;
            case "gemini":
                services.AddSingleton<IChatClient, GeminiChatClient>();
                break;
            case "http":
            case "local":
                services.AddHttpClient<HttpChatClient>();
                services.AddSingleton<IChatClient, HttpChatClient>();
                break;
            default:
                services.AddSingleton<IChatClient, SimpleChatClient>();
                break;
        }

        services.AddHostedService<ChatBotService>();
    })
    .Build();

await host.RunAsync();
