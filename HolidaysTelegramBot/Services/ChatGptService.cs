using HolidaysTelegramBot.Abstract;
using OpenAI_API;

namespace HolidaysTelegramBot.Services
{
    public class ChatGPTService : IChatGPTService
    {
        const string APIKey = "";

        public async Task<string> AskChatGPT(string query)
        {
            var openAI = new OpenAIAPI(APIKey);
            var chat = openAI.Chat.CreateConversation();
            chat.AppendSystemMessage("You get a description of a person and have to write some ideas for present to this person. Write only propositions for present to this person.");
            chat.AppendUserInput(query);
            var response = await chat.GetResponseFromChatbotAsync();
            Console.WriteLine(response);
            return response;
        }
    }
}
