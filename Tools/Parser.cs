using System.Globalization;
using System.Net;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using khai_schedule_bot.Models;

namespace khai_schedule_bot.Tools;

public static class Parser
{
    private static AppDbContext _db = new AppDbContext();
    /// <summary>
    /// Парсит группы с сайта кафедры.
    /// </summary>
    /// <param name="department">Код кафедры</param>
    /// <returns>Список групп для кафедры</returns>
    public static Group[] ParseGroups(int department)
    {
        var html = ParsePage($"https://education.khai.edu/department/{department}");

        if (html is null)
            return new Group[0];

        var groupsSection = html.QuerySelector(".py-2");
        var groups = groupsSection.QuerySelectorAll("a");

        Group[] result = new Group[groups.Count];
        int faculty = department / 100;
        
        for (int i = 0; i < groups.Count; i++)
        {
            string uaCode = WebUtility.HtmlDecode(groups[i].InnerText.Replace(", ", ""));
            string engCode = groups[i].GetAttributeValue("href", "").Split("/")[^1];

            result[i] = new Group(faculty, department, uaCode, engCode);
        }

        return result;
    }

    private static HtmlDocument? ParsePage(string url)
    {
        HtmlWeb web = new HtmlWeb();

        HtmlDocument html;
        
        try
        {
            html = web.Load(url);
        }
        catch
        {
            string msg = "Неправильная ссылка или сайт недоступен";
            Console.WriteLine(msg);
            Logger.Log($"Parser.ParsePage(), url: {url}, msg: {msg}");
            return null;
        }

        return html;
    }

    public static List<Class> ParseSchedule(string engGroupCode)
    {
        List<Class> result = new List<Class>();
        Group group = _db.Groups.First(g => g.EngCode == engGroupCode);
        
        var html = ParsePage($"https://education.khai.edu/union/schedule/group/{engGroupCode}");
        if (html is null)
            return new List<Class>();

        var tableRows = html.QuerySelectorAll("tr");

        DayOfWeek dayOfWeek = DayOfWeek.Monday;
        // Счетчик номера пары в списке
        int counter = 1;
        
        foreach (var row in tableRows)
        {
            if (WebUtility.HtmlDecode(row.InnerHtml.Trim()).Contains("fa-calendar-minus"))
            {
                continue;
            }
            var head = row.QuerySelector("th");
            
            if (head != null)
            {
                switch (WebUtility.HtmlDecode(head.InnerText.Trim()))
                {
                    case "Понеділок":
                        dayOfWeek = DayOfWeek.Monday;
                        counter = 1;
                        continue;
                    case "Вівторок":
                        dayOfWeek = DayOfWeek.Tuesday;
                        counter = 1;
                        continue;
                    case "Середа":
                        dayOfWeek = DayOfWeek.Wednesday;
                        counter = 1;
                        continue;
                    case "Четвер":
                        dayOfWeek = DayOfWeek.Thursday;
                        counter = 1;
                        continue;
                    case "П'ятниця":
                        dayOfWeek = DayOfWeek.Friday;
                        counter = 1;
                        continue;
                }
            }

            if (row.QuerySelectorAll("th").Count == 2)
            {
                var data = row.QuerySelector("th");
                
                string[] classInfo = WebUtility.HtmlDecode(data.InnerText.Trim()).Split(",");
                
                
                if (classInfo.Length == 1)
                {
                    counter++;
                    continue;
                }

                var prev = result.Last();
                if (classInfo.Length == 2)
                {
                    result.Add(new Class(group.Id, counter, WeekType.Denominator, classInfo[0], ClassType.Practice, null, null, 
                        dayOfWeek,  prev.StartTime,
                        prev.EndTime));
                }
                else
                {
                    result.Add(new Class(group.Id, counter, WeekType.Denominator, classInfo[1], ClassType.Practice,
                        classInfo[3], classInfo[0], 
                        dayOfWeek,  prev.StartTime,
                        prev.EndTime));
                }
                counter++;
                continue;
            }

            var rowData = row.QuerySelectorAll("td");

            // ex: 11:55 - 13:30
            string time = WebUtility.HtmlDecode(rowData[0].InnerText.Trim());
            
            // ex: 123р, Архітектура комп'ютерів, лаб. практикум, доцент Дужий Вячеслав Ігорович
            string info = WebUtility.HtmlDecode(rowData[1].InnerText.Trim());

            // [0] 11:55; [1] 13:30;
            string[] timeArr = time.Split(" - ");

            //[0] 123p; [1] Архітектура комп'ютерів, [2] лаб. практикум, [3] доцент Дужий Вячеслав Ігорович;
            // or
            //[0] Фізичне виховання; [1] практика;
            string[] infoArr = info.Split(",");


            if (infoArr.Length == 1)
            {
                counter++;
                continue;
            }
            
            if (infoArr.Length == 2)
            {
                result.Add(new Class(group.Id, counter, WeekType.Numerator, infoArr[0], ClassType.Practice, null, null, 
                    dayOfWeek,  DateTime.ParseExact(timeArr[0], "HH:mm", CultureInfo.InvariantCulture),
                    DateTime.ParseExact(timeArr[1], "HH:mm", CultureInfo.InvariantCulture)));
            }
            else
            {
                result.Add(new Class(group.Id, counter, WeekType.Numerator, infoArr[1], ClassType.Practice, infoArr[3], infoArr[0], 
                    dayOfWeek,  DateTime.ParseExact(timeArr[0], "HH:mm", CultureInfo.InvariantCulture),
                    DateTime.ParseExact(timeArr[1], "HH:mm", CultureInfo.InvariantCulture)));
            }
            counter++;
        }
        
        foreach (var r in result)
            Console.WriteLine(r);

        return result;
    }
}