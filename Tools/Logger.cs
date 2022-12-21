namespace khai_schedule_bot.Tools;

public static class Logger
{
    public static void Log(string msg)
    {
        using (StreamWriter w = File.AppendText("logs.txt"))
        {
            string message = $"[{DateTime.Now.ToString("HH:mm:ss, dd/MM/yy")}] {msg}";
            w.WriteLine(message);
        }
    }
}