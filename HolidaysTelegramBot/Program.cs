using HolidaysTelegramBot;
using HolidaysTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using OpenAIClient;
using OpenAIClient.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


#region COMMANDS
const string description = "What this bot can do?";
const string gpt = "Generate ideas with ChatGPT";
#endregion

var botClient = new TelegramBotClient("5914114434:AAGJ9mh8Iy92K4IBtoq6awzsZDrIscSOd5g");

var buttons = new ReplyKeyboardMarkup
    (
         new List<KeyboardButton>
         (
            new List<KeyboardButton> { new KeyboardButton(gpt), new KeyboardButton(description) }
         )
    );

using CancellationTokenSource cts = new();



ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() 
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();


cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{

    if (update.Message is not { } message)
        return;

    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;
    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
    var currentState = await GetUserCondition(chatId);

    if (currentState == null)
        CreateUserCondition(chatId, "", message.Text);

    if (currentState is null || string.IsNullOrEmpty(currentState.LastQuery))
    {
        switch (message.Text)
        {
            case gpt:
                await botClient.SendTextMessageAsync(chatId: chatId,
                    text: "Write person Name",
                    replyMarkup: buttons,
                    cancellationToken: cancellationToken);
                UpdateUserCondition(chatId, "Write person Name", message.Text);
                break;
            case description:
                await botClient.SendTextMessageAsync(chatId: chatId,
                    text: "Bot will write a list of ideas for presents for birthday to someone. \nYou only have to write a description of this person.",
                    replyMarkup: buttons,
                    cancellationToken: cancellationToken);
                break;
            default:
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Type smth else",
                    replyMarkup: buttons,
                    cancellationToken: cancellationToken);
                break;
        }
    }

    else
    {
        GetPersonInfo(botClient, message);
    }
}

async void GetPersonInfo(ITelegramBotClient client, Message message)
{
    var chatId = message.Chat.Id;
    var currentState = await GetUserCondition(chatId);
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
                UpdateUserCondition(chatId, "Write person Age", result);
                break;
            case "Write person Age":
                result += "\nAge - " + message.Text;
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Write person Gender",
                replyMarkup: buttons);
                UpdateUserCondition(chatId, "Write person Gender", result);
                break;
            case "Write person Gender":
                result += "\nJob/Hobie - " + message.Text;
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Write person Job/Hobie",
                replyMarkup: buttons);
                UpdateUserCondition(chatId, "Write person Job/Hobie", result);
                break;
            case "Write person Job/Hobie":
                result += "\nJob/Hobie - " + message.Text;
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Write person description",
                replyMarkup: buttons);
                UpdateUserCondition(chatId, "Write person description", result);
                break;
            case "Write person description":
                result += "Description " + message.Text;
                var response = await chatGpt.AskChatGPT(result);
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: response,
                replyMarkup: buttons);
                UpdateUserCondition(chatId, null, null);
                break;
            default:
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Type smth else",
                    replyMarkup: buttons);
                UpdateUserCondition(chatId, null, null);
                break;
        }
    //}
}

async Task<UserCondition?> GetUserCondition(long chatId)
{
    await using var db = new ApplicationDbContext();
    var userCondition = await db.Conditions.FirstOrDefaultAsync(x => x.ChatId == chatId);
    return userCondition ?? null;
}

async void CreateUserCondition(long chatId, string? lastQuery, string? message)
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

async void UpdateUserCondition(long chatId, string? lastQuery, string? message)
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

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}