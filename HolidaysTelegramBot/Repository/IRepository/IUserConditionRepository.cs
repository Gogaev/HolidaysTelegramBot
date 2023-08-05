using HolidaysTelegramBot.Models;

namespace HolidaysTelegramBot.Repository.IRepository
{
    public interface IUserConditionRepository
    {
        Task<UserCondition?> GetUserCondition(long chatId);
        void CreateUserCondition(long chatId, string? lastQuery, string? message);
        void UpdateUserCondition(long chatId, string? lastQuery, string? message);
    }
}
