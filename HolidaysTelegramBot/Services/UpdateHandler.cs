using HolidaysTelegramBot.Abstract;
using HolidaysTelegramBot.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HolidaysTelegramBot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IChatGptService _chatGptService;
    private readonly ApplicationDbContext _dbContext;

    public UpdateHandler(
        ITelegramBotClient botClient,
        ILogger<UpdateHandler> logger,
        IChatGptService chatGptService,
        ApplicationDbContext dbContext)
    {
        _botClient = botClient;
        _logger = logger;
        _chatGptService = chatGptService;
        _dbContext = dbContext;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { } message }                       => BotOnMessageReceived(message, cancellationToken),
            _                                              => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        var buttons = new ReplyKeyboardMarkup
        (
             new List<KeyboardButton>
             (
                new List<KeyboardButton> { new KeyboardButton("ChatGPT"), new KeyboardButton("Description") }
             )
        );

        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        var currentUserContext = await _dbContext.Contexts
            .FirstOrDefaultAsync(x => x.UserId == chatId, cancellationToken: cancellationToken);

        if (currentUserContext == null)
        {
            var userContext = new UserContext
            {
                UserId = chatId,
                State = States.Initial,
                Name = null,
                Age = null,
                Gender = null,
                Hobie = null,
                Description = null,
            };
            _dbContext.Contexts.Add(userContext);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _botClient.SendTextMessageAsync(chatId: chatId,
                text: "ChatGPT - Will ask you information about person and generate ideas for present\nDescription - will write what this bot can do?",
                replyMarkup: buttons,
                cancellationToken: cancellationToken);
        }
        else
        {
            if (currentUserContext.State is States.Initial)
            {
                switch (message.Text)
                {
                    case "ChatGPT":
                        await _botClient.SendTextMessageAsync(chatId: chatId,
                            text: "Write person Name",
                            replyMarkup: buttons,
                            cancellationToken: cancellationToken);
                        currentUserContext.State = States.WaitingForNameInput;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        break;
                    case "Description":
                        await _botClient.SendTextMessageAsync(chatId: chatId,
                            text: "Bot will write a list of ideas for presents for birthday to someone. \nYou only have to write a description of this person.",
                            replyMarkup: buttons,
                            cancellationToken: cancellationToken);
                        break;
                    default:
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Choose command",
                            replyMarkup: buttons,
                            cancellationToken: cancellationToken);
                        break;
                }
            }
            else
            {
                switch (currentUserContext.State)
                {
                    case States.WaitingForNameInput:
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Write person Age",
                            cancellationToken: cancellationToken);
                        currentUserContext.Name = message.Text;
                        currentUserContext.State = States.WaitingForAgeInput;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        break;
                    case States.WaitingForAgeInput:
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Write person Gender(Male/Female)",
                            cancellationToken: cancellationToken);
                        currentUserContext.Age = int.Parse(message.Text);
                        currentUserContext.State = States.WaitingForGenderInput;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        break;
                    case States.WaitingForGenderInput:
                        if (!Enum.TryParse(message.Text, true, out Gender gender))
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Write person Gender correctly(Male/Female)",
                                cancellationToken: cancellationToken);
                            break;
                        }
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Write person Job/Hobie",
                            cancellationToken: cancellationToken);
                        currentUserContext.Gender = gender;
                        currentUserContext.State = States.WaitingForJobHobieInput;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        break;
                    case States.WaitingForJobHobieInput:
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Write person description",
                            cancellationToken: cancellationToken);
                        currentUserContext.Hobie = message.Text;
                        currentUserContext.State = States.WaitingForDescriptionInput;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        break;
                    case States.WaitingForDescriptionInput:
                        currentUserContext.Description = message.Text;
                        currentUserContext.State = States.Initial;
                        var response = await _chatGptService.Ask(currentUserContext);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: response,
                            cancellationToken: cancellationToken);
                        break;
                    default:
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Type smth else",
                            cancellationToken: cancellationToken);
                        break;
                }
            }
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken _)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);

        if (exception is RequestException)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}
