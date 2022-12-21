namespace khai_schedule_bot.Models;

public class BotUser
{
    public int Id { get; set; }
    public long ChatId { get; set; }
    public UserState State { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? Faculty { get; set; }
    public int? Department { get; set; }
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    public string? TgFirstName { get; set; }
    public string? TgLastName { get; set; }
    public string? TgUsername { get; set; }

    public BotUser()
    {
        
    }
    public BotUser(long chatId, UserState state, string? firstName, 
        string? lastName, int? faculty, int? department,
        int? groupId, Group? group, string? tgFirstName, 
        string? tgLastName, string? tgUsername)
    {
        ChatId = chatId;
        State = state;
        FirstName = firstName;
        LastName = lastName;
        Faculty = faculty;
        Department = department;
        GroupId = groupId;
        Group = group;
        TgFirstName = tgFirstName;
        TgLastName = tgLastName;
        TgUsername = tgUsername;
    }
}
