namespace ChatBot;

public class ChatBotSettings
{
    public string BotName { get; set; } = "ChatBot";
    public string WelcomeMessage { get; set; } = "你好！我是聊天机器人，有什么可以帮助你的吗？";
    public bool UseOpenAI { get; set; } = false;
    public int TypingDelayMs { get; set; } = 30;
    public List<string> ExitCommands { get; set; } = new() { "exit", "退出", "quit", "bye", "q" };
    public int RateLimitPerMinute { get; set; } = 20;
    public bool ShowStatistics { get; set; } = true;
}
