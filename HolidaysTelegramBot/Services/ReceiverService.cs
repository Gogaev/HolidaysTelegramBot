using HolidaysTelegramBot.Abstract;
using HolidaysTelegramBot.Services;
using Microsoft.Extensions.Logging;

namespace Telegram.Bot.Services;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService : ReceiverServiceBase<UpdateHandler>
{
    public ReceiverService(
        ITelegramBotClient botClient,
        UpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<UpdateHandler>> logger)
        : base(botClient, updateHandler, logger)
    {
    }
}
