using HolidaysTelegramBot.Abstract;
using HolidaysTelegramBot.Repository.IRepository;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace HolidaysTelegramBot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IChatGPTService _chatGPTService;
    private readonly IUserConditionRepository _userCondition;

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger, IChatGPTService chatGPTService, IUserConditionRepository userCondition)
    {
        _botClient = botClient;
        _logger = logger;
        _chatGPTService = chatGPTService;
        _userCondition = userCondition;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message }                       => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message }                 => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery }           => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery }               => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
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
            new List<KeyboardButton> { new KeyboardButton("Generate ideas with ChatGPT"), new KeyboardButton("What this bot can do?") }
         )
    );

        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        var currentState = await _userCondition.GetUserCondition(chatId);
        if (currentState == null)
            _userCondition.CreateUserCondition(chatId, "", message.Text);

        if (currentState is null || string.IsNullOrEmpty(currentState.LastQuery))
        {
            switch (message.Text)
            {
                case "Generate ideas with ChatGPT":
                    await _botClient.SendTextMessageAsync(chatId: chatId,
                        text: "Write person Name",
                        replyMarkup: buttons,
                        cancellationToken: cancellationToken);
                    _userCondition.UpdateUserCondition(chatId, "Write person Name", message.Text);
                    break;
                case "What this bot can do?":
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
            GetPersonInfo(_botClient, message);
        }
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);

        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    async void GetPersonInfo(ITelegramBotClient client, Message message)
    {
        var buttons = new ReplyKeyboardMarkup
        (
         new List<KeyboardButton>
         (
            new List<KeyboardButton> { new KeyboardButton("Generate ideas with ChatGPT"), new KeyboardButton("What this bot can do?") }
         )
        );
        var chatId = message.Chat.Id;
        var currentState = await _userCondition.GetUserCondition(chatId);
        IChatGPTService chatGpt = new ChatGPTService();
        var result = currentState.Response;
        switch (currentState.LastQuery)
        {
            case "Write person Name":
                result += "Name - " + message.Text;
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Write person Age",
                replyMarkup: buttons);
                _userCondition.UpdateUserCondition(chatId, "Write person Age", result);
                break;
            case "Write person Age":
                result += "\nAge - " + message.Text;
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Write person Gender",
                replyMarkup: buttons);
                _userCondition.UpdateUserCondition(chatId, "Write person Gender", result);
                break;
            case "Write person Gender":
                result += "\nJob/Hobie - " + message.Text;
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Write person Job/Hobie",
                replyMarkup: buttons);
                _userCondition.UpdateUserCondition(chatId, "Write person Job/Hobie", result);
                break;
            case "Write person Job/Hobie":
                result += "\nJob/Hobie - " + message.Text;
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Write person description",
                replyMarkup: buttons);
                _userCondition.UpdateUserCondition(chatId, "Write person description", result);
                break;
            case "Write person description":
                result += "Description " + message.Text;
                var response = await chatGpt.AskChatGPT(result);
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: response,
                replyMarkup: buttons);
                _userCondition.UpdateUserCondition(chatId, null, null);
                break;
            default:
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Type smth else",
                    replyMarkup: buttons);
                _userCondition.UpdateUserCondition(chatId, null, null);
                break;
        }
    }
}
