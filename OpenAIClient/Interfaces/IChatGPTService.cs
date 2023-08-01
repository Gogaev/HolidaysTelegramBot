namespace OpenAIClient.Interfaces
{
    public interface IChatGPTService
    {
        Task<string> AskChatGPT(string query);
    }
}
