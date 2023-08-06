namespace HolidaysTelegramBot.Domain;

public enum States
{
    Initial = 1,
    WaitingForNameInput,
    WaitingForAgeInput,
    WaitingForGenderInput,
    WaitingForJobHobieInput,
    WaitingForDescriptionInput,
}
