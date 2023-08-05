namespace HolidaysTelegramBot.Abstract
{
    public interface IChatGPTService
    {
        Task<string> AskChatGPT(string query);
    }
}
