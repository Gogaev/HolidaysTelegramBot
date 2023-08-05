using HolidaysTelegramBot.Models;
using HolidaysTelegramBot.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace HolidaysTelegramBot.Repository
{
    public class UserConditionRepository : IUserConditionRepository
    {
        public async Task<UserCondition?> GetUserCondition(long chatId)
        {
            await using var db = new ApplicationDbContext();
            var userCondition = await db.Conditions.FirstOrDefaultAsync(x => x.ChatId == chatId);
            return userCondition ?? null;
        }

        public async void CreateUserCondition(long chatId, string? lastQuery, string? message)
        {
            await using var db = new ApplicationDbContext();
            var userCondition = new UserCondition()
            {
                ChatId = chatId,
                LastQuery = lastQuery,
                Response = message
            };
            await db.Conditions.AddAsync(userCondition);
            await db.SaveChangesAsync();
        }

        public async void UpdateUserCondition(long chatId, string? lastQuery, string? message)
        {
            await using var db = new ApplicationDbContext();
            var userCond = await db.Conditions.FirstOrDefaultAsync(x => x.ChatId == chatId);
            if (userCond != null)
            {
                userCond.LastQuery = lastQuery;
                userCond.Response = message;
            }

            await db.SaveChangesAsync();
        }
    }
}
