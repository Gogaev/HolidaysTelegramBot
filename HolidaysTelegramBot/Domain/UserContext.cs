
using System.ComponentModel.DataAnnotations;

namespace HolidaysTelegramBot.Domain;

public class UserContext
{
    [Key]
    public long UserId { get; set; }
    public States? State { get; set; }
    public string? Name { get; set; }
    public int? Age { get; set; }
    public Gender? Gender { get; set; }
    public string? Hobie { get; set; }
    public string? Description { get; set; }
}
