using System.Text;
using khai_schedule_bot;
using khai_schedule_bot.Tools;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

AppDbContext _db = new AppDbContext();

var defaultDepartments = new int[]
{
    101,
    102,
    103,
    104,
    105,
    106,
    107,
    201,
    202,
    203,
    204,
    205,
    301,
    302,
    303,
    304,
    305,
    401,
    402,
    403,
    405,
    406,
    407,
    501,
    502,
    503,
    504,
    505,
    601,
    602,
    603,
    604,
    605,
    701,
    702,
    703,
    704,
    705,
    801,
    802
};

for (int i = 0; i < args.Length; i++)
{
    // Ex: ./khai_schedule_bot --fill-db
    if (args[i] == "--fill-db")
    {
        foreach (var department in defaultDepartments)
        {
            _db.Groups.AddRange(Parser.ParseGroups(department));
        }
        _db.SaveChanges();

        Console.WriteLine("Groups added successfully");
        
        foreach (var group in _db.Groups)
        {
            _db.Classes.AddRange(Parser.ParseClasses(group.EngCode));
        }
        _db.SaveChanges();
        
        Console.WriteLine("Classes added successfully");
    }
}


TelegramBot.StartInNewThread();
Console.ReadLine();

// while (true)
// {
//     string message = Console.ReadLine();
//     await TelegramBot.Bot.SendTextMessageAsync(326623471, message);
//     
//     var st2 = _db.Groups.First(group => group.EngCode == "515st2");
//     var classesInMonay = _db.Classes.Where(c => 
//         c.DayOfWeek == DayOfWeek.Monday && c.Group.Id == st2.Id).OrderBy(c => c.Number).ToList();
//
     
// }