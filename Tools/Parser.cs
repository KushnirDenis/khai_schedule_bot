using System.Net;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using khai_schedule_bot.Models;

namespace khai_schedule_bot;

public static class Parser
{
    /// <summary>
    /// Парсит группы с сайта кафедры.
    /// </summary>
    /// <param name="department">Код кафедры</param>
    /// <returns>Список групп для кафедры</returns>
    public static Group[] ParseGroups(int department)
    {
        var url = $"https://education.khai.edu/department/{department}";

        var html = ParsePage(url);

        if (html is null)
            return new Group[0];

        var groupsSection = html.QuerySelector(".py-2");
        var groups = groupsSection.QuerySelectorAll("a");

        Group[] result = new Group[groups.Count];
        int faculty = department / 100;
        
        for (int i = 0; i < groups.Count; i++)
        {
            string uaCode = WebUtility.HtmlDecode(groups[i].InnerText.Replace(", ", ""));
            string engCode = groups[i].GetAttributeValue("href", "");

            result[i] = new Group(faculty, uaCode, engCode);
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
            Console.WriteLine("Неправильная ссылка или сайт недоступен");
            return null;
        }

        return html;
    }

    public static void ParseSchedule(string engGroupCode)
    {
        
    }
}