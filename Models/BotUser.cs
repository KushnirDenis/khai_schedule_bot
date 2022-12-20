namespace khai_schedule_bot.Models;

public class BotUser
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    public string? TgFirstName { get; set; }
    public string? TgLastName { get; set; }
    public string? TgUsername { get; set; }
}
