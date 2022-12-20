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
        
        string[] tmpTime = new string[2];

        for (int i = 0; i < tableRows.Count; i++)
        {
            var row = tableRows[i];
            
            var head = row.QuerySelectorAll("th");
            // Проверка на день недели. Например "Понеділок", "Вівторок" і т.д
            if (head.Count == 1)
            {
                switch (WebUtility.HtmlDecode(head[0].InnerText.Trim()))
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
                    default:
                        continue;
                }
            } else if (head.Count == 2) // Проверка на пару по знаменателю
            {
                var info = WebUtility.HtmlDecode(head[0].InnerText.Trim()).Split(",");
                if (info.Length == 2)
                {
                    result.Add(new Class(group.Id, counter, WeekType.Denominator, info[0], ClassType.Practice,
                        null, null, 
                        dayOfWeek,  DateTime.ParseExact(tmpTime[0], "HH:mm", CultureInfo.InvariantCulture),
                        DateTime.ParseExact(tmpTime[1], "HH:mm", CultureInfo.InvariantCulture)));
                }
                else
                {
                    result.Add(new Class(group.Id, counter, WeekType.Denominator, info[1], ClassType.Practice,
                        info[3], info[0], 
                        dayOfWeek,  DateTime.ParseExact(tmpTime[0], "HH:mm", CultureInfo.InvariantCulture),
                        DateTime.ParseExact(tmpTime[1], "HH:mm", CultureInfo.InvariantCulture)));
                }

                counter++;
                continue;
            }
            // Проверка есть ли чередование предметов в эту пару
            if (row.QuerySelector("td").Attributes.Contains("rowspan") &&
                row.QuerySelector("td").GetAttributeValue("rowspan", 0) == 2)
            {
                var data = row.QuerySelectorAll("td");
                
                // Проверка есть ли пара по числителю, или это прочерк (Выполняется, если пары нет)
                if (data[1].Attributes.Contains("colspan") &&
                    data[1].GetAttributeValue("colspan", 0) == 2)
                {
                    // Если пары нет, записываем время
                    var timeArr = data[0].InnerText.Split(" - ");
                    
                    // получаем пару по знаменателю
                    var nextRow = tableRows[++i];
                
                    var classInfo = nextRow.QuerySelector("th");
                    var info = WebUtility.HtmlDecode(classInfo.InnerText.Trim()).Split(",");
                    
                    if (info.Length == 2)
                    {
                        result.Add(new Class(group.Id, counter, WeekType.Denominator, info[0], ClassType.Practice,
                            null, null, 
                            dayOfWeek,  DateTime.ParseExact(timeArr[0], "HH:mm", CultureInfo.InvariantCulture),
                            DateTime.ParseExact(timeArr[1], "HH:mm", CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        result.Add(new Class(group.Id, counter, WeekType.Denominator, info[1], ClassType.Practice,
                            info[3], info[0], 
                            dayOfWeek,  DateTime.ParseExact(timeArr[0], "HH:mm", CultureInfo.InvariantCulture),
                            DateTime.ParseExact(timeArr[1], "HH:mm", CultureInfo.InvariantCulture)));
                    }
                    counter++;
                    continue;
                }
                else 
                {   // Если пара по числителю сущетсвует, записываем данные
                    var rowData = row.QuerySelectorAll("td");
                    var timeArr = rowData[0].InnerText.Split(" - ");
                    var infoArr = WebUtility.HtmlDecode(rowData[1].InnerText.Trim()).Split(",");
                    
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
                    
                    var nextRow = tableRows[i + 1];
                    // Если пары по знаменателю нет, инкрементим счётчик пар
                    if (nextRow.QuerySelectorAll("th").Count == 1)
                    {
                        counter++;
                        continue;
                    }
                    else
                    {
                        // Если пара по знаменателю есть, записываем
                        // время пары во временную переменную
                        tmpTime = timeArr;
                    }
                }
            }
            
            
            
        }
        // foreach (var row in tableRows)
        // {
        //     
        //     
        //
        //     if (row.QuerySelectorAll("th").Count == 2)
        //     {
        //         var data = row.QuerySelector("th");
        //         
        //         string[] classInfo = WebUtility.HtmlDecode(data.InnerText.Trim()).Split(",");
        //         
        //         
        //         if (classInfo.Length == 1)
        //         {
        //             counter++;
        //             continue;
        //         }
        //
        //         var prev = result.Last();
        //         if (classInfo.Length == 2)
        //         {
        //             result.Add(new Class(group.Id, counter, WeekType.Denominator, classInfo[0], ClassType.Practice, null, null, 
        //                 dayOfWeek,  prev.StartTime,
        //                 prev.EndTime));
        //         }
        //         else
        //         {
        //             result.Add(new Class(group.Id, counter, WeekType.Denominator, classInfo[1], ClassType.Practice,
        //                 classInfo[3], classInfo[0], 
        //                 dayOfWeek,  prev.StartTime,
        //                 prev.EndTime));
        //         }
        //         counter++;
        //         continue;
        //     }
        //
        //     var rowData = row.QuerySelectorAll("td");
        //
        //     // ex: 11:55 - 13:30
        //     string time = WebUtility.HtmlDecode(rowData[0].InnerText.Trim());
        //     
        //     // ex: 123р, Архітектура комп'ютерів, лаб. практикум, доцент Дужий Вячеслав Ігорович
        //     string info = WebUtility.HtmlDecode(rowData[1].InnerText.Trim());
        //
        //     // [0] 11:55; [1] 13:30;
        //     string[] timeArr = time.Split(" - ");
        //
        //     //[0] 123p; [1] Архітектура комп'ютерів, [2] лаб. практикум, [3] доцент Дужий Вячеслав Ігорович;
        //     // or
        //     //[0] Фізичне виховання; [1] практика;
        //     string[] infoArr = info.Split(",");
        //
        //
        //     if (infoArr.Length == 1)
        //     {
        //         counter++;
        //         continue;
        //     }
        //     
        // if (infoArr.Length == 2)
        // {
        //     result.Add(new Class(group.Id, counter, WeekType.Numerator, infoArr[0], ClassType.Practice, null, null, 
        //         dayOfWeek,  DateTime.ParseExact(timeArr[0], "HH:mm", CultureInfo.InvariantCulture),
        //         DateTime.ParseExact(timeArr[1], "HH:mm", CultureInfo.InvariantCulture)));
        // }
        // else
        // {
        //     result.Add(new Class(group.Id, counter, WeekType.Numerator, infoArr[1], ClassType.Practice, infoArr[3], infoArr[0], 
        //         dayOfWeek,  DateTime.ParseExact(timeArr[0], "HH:mm", CultureInfo.InvariantCulture),
        //         DateTime.ParseExact(timeArr[1], "HH:mm", CultureInfo.InvariantCulture)));
        // }
        // counter++;
        // }
        //
        foreach (var r in result) Console.WriteLine(r);

        return result;
    }
}