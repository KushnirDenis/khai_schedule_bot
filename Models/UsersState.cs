namespace khai_schedule_bot.Models;

public class UsersState
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public BotUser User { get; set; }
    public UserState UserState { get; set; }
}