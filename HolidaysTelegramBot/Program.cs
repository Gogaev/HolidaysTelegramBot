using OpenAI_API;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var botClient = new TelegramBotClient("5914114434:AAGJ9mh8Iy92K4IBtoq6awzsZDrIscSOd5g");


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

    switch (message.Text)
    {
        case "Gpt":
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Write persone description",
                replyMarkup: GetButtons(),
                cancellationToken: cancellationToken);
            break;
        case "What this bot can do?":
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Bot will write a list of ideas for presents for birthday to someone. \nYou only have to write a description of this persone.",
                replyMarkup: GetButtons(),
                cancellationToken: cancellationToken);
            break;
        default:
            string response = await AskChatGPT(message.Text);
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: response,
                replyMarkup: GetButtons(),
                cancellationToken: cancellationToken);
            break;
    }   
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

IReplyMarkup? GetButtons()
{
    return new ReplyKeyboardMarkup
    (
         new List<KeyboardButton>
         (
            new List<KeyboardButton> { new KeyboardButton("Gpt"), new KeyboardButton("What this bot can do?") }
         )
    );
}

async Task<string> AskChatGPT(string query)
{
    var openAI = new OpenAIAPI("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");
    var chat = openAI.Chat.CreateConversation();
    chat.AppendSystemMessage("You get a description of a person and have to write some ideas for present to this persone. Write only propositions for present to this persone.");
    chat.AppendUserInput(query);
    string response = await chat.GetResponseFromChatbotAsync();
    Console.WriteLine(response);
    return response;
}