namespace ChatBot;

public class ChatBotSettings
{
    public string BotName { get; set; } = "ChatBot";
    public string WelcomeMessage { get; set; } = "你好！我是聊天机器人，有什么可以帮助你的吗？";
    public string ExitCommand { get; set; } = "exit";
    public bool UseOpenAI { get; set; } = false;
}
