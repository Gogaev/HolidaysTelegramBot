
namespace HolidaysTelegramBot.Models;

public class UserContext
{
    public long UserId { get; set; }
    public States State { get; set; }


    //public string? LastQuery { get; set; }
    // public long ChatId { get; set; }
    // public string? Response { get; set; }
}

public enum States
{
    Initial = 1,
    WaitingForNameInput,
    WaitingForAgeInput,
    WaitingForGenderInput,
    WaitingForJobHobieInput,
    WaitingForDescriptionInput,
}
