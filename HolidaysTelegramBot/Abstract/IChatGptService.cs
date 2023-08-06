using HolidaysTelegramBot.Domain;

namespace HolidaysTelegramBot.Abstract;

public interface IChatGptService
{
    Task<string> Ask(UserContext context);
}
