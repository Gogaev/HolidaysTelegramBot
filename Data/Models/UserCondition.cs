
namespace Data.Models
{
    public class UserCondition
    {
        public int Id { get; set; }

        public string? LastQuery { get; set; }

        public long ChatId { get; set; }

        public string? Response { get; set; }
    }
}
