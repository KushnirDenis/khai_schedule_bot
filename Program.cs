using System.Text;
using khai_schedule_bot;
using khai_schedule_bot.Models;
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

var startTimes = (from c in _db.Classes select c.StartTime).Distinct().OrderBy(c => c.Hour).ToList();


while (true)
{
    // var now = new DateTime(1001, 1, 21, 9, 40, 0);
    var now = DateTime.Now;

    // Если день не >= Понедельник и не <= Пятнице
    if (!(now.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday))
    {
        var addDays = (7 - (int)now.DayOfWeek) == 7 ? 1 : 7 - (int)now.DayOfWeek;
        var tmp = now.AddDays(addDays);

        var ms = tmp.Subtract(now).TotalMilliseconds;

        var msg = $"{ms} ms sleep (weekend)";
        
        Logger.Log(msg);
        Console.WriteLine(msg);
        
        Thread.Sleep((int)ms);
        TelegramBot.UpdateWeekType();
    }

    if (now.Hour > startTimes[^1].Hour - 1)
    {
        var tmp = new DateTime(now.Year, now.Month, now.Day + 1, startTimes[0].Hour - 1, startTimes[0].Minute + 30, 0);
        var ms = (int)tmp.Subtract(now).TotalMilliseconds;
        Console.WriteLine($"{ms} ms sleep (кончилась последняя пара)");
        Thread.Sleep(ms);
        TelegramBot.UpdateWeekType();
    }

    for (int i = 0; i < startTimes.Count; i++)
    {
        if (now.Hour == startTimes[i].Hour &&
            now.Minute == startTimes[i].Minute - 10)
        {
            List<Class> classes = new List<Class>();
            var groups = _db.Users.Select(u => u.Group).ToList();

            foreach (var group in groups)
            {
                var tmp = _db.Classes.Where(c => c.GroupId == group.Id &&
                                                 c.StartTime.Hour == startTimes[i].Hour &&
                                                 c.StartTime.Minute == startTimes[i].Minute &&
                                                 (c.WeekType == TelegramBot.WeekType ||
                                                 c.WeekType == WeekType.NotAlternate) &&
                                                 c.DayOfWeek == now.DayOfWeek).ToList();
                classes.AddRange(tmp);
            }
            
            foreach (var c in classes)
            {
                var users = _db.Users.Where(u => u.GroupId == c.GroupId);
                
                foreach (var user in users)
                {
                    await TelegramBot.Bot.SendTextMessageAsync(user.ChatId, c.ToString(), ParseMode.Html);
                }
            }

            break;
        }
    }
    
    Thread.Sleep(120000);
    
}


Console.ReadLine();