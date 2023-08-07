using System.Text;
using HolidaysTelegramBot.Abstract;
using HolidaysTelegramBot.Domain;
using OpenAI_API;

namespace HolidaysTelegramBot.Services;

public class ChatGptService : IChatGptService
{
    // const string APIKey = "";

    public async Task<string> Ask(UserContext context)
    {
        var key = Environment.GetEnvironmentVariable("OpenAiKey");
        var openAi = new OpenAIAPI(key);
        var chat = openAi.Chat.CreateConversation();
        var queryBuilder = new StringBuilder();
        queryBuilder.Append("Name - " + context.Name);
        queryBuilder.Append(" Age - " + context.Age);
        queryBuilder.Append(" Gender - " + context.Gender);
        queryBuilder.Append(" Job/Hobie - " + context.Hobie);
        queryBuilder.Append(" Description - " + context.Description);
        chat.AppendSystemMessage("You get a description of a person and have to write some ideas for present" +
                                 " to this person. Write only propositions for present to this person.");
        chat.AppendUserInput(queryBuilder.ToString());
        return await chat.GetResponseFromChatbotAsync();
    }
}
