using System.Net;
using System.Web;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using khai_schedule_bot.Models;

namespace khai_schedule_bot;

public static class Parser
{
    /// <summary>
    /// Парсит группы с сайта кафедры.
    /// </summary>
    /// <param name="department"></param>
    /// <returns>Хеш страницы</returns>
    public static Group[] ParseGroups(int department)
    {
        var url = $"https://education.khai.edu/department/{department}";

        HtmlWeb web = new HtmlWeb();

        var html = web.Load(url);


        var groupsSection = html.QuerySelector(".py-2");
        var groups = groupsSection.QuerySelectorAll("a");

        Group[] result = new Group[groups.Count];
        int faculty = department / 100;
        
        for (int i = 0; i < groups.Count; i++)
        {
            string uaCode = WebUtility.HtmlDecode(groups[i].InnerText.Replace(", ", ""));
            string engCode = groups[i].GetAttributeValue("href", "");

            result[i] = new Group(faculty, uaCode, engCode);
            Console.WriteLine(result[i]);
        }

        return result;
    }
}