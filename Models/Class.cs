namespace khai_schedule_bot.Models;

public class Class
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; }
    /// <summary>
    /// Номер по расписанию
    /// </summary>
    public int Number { get; set; }

    public WeekType WeekType { get; set; }
    public string Name { get; set; }
    public ClassType ClassType { get; set; }
    public string? TeacherName { get; set; }
    public string? AudienceNumber { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public Class()
    {
        
    }
    
    public Class(int groupId, int number, WeekType weekType, string name, 
        ClassType classType, string? teacherName, string? audienceNumber, 
        DayOfWeek dayOfWeek, DateTime startTime, DateTime endTime)
    {
        GroupId = groupId;
        Number = number;
        WeekType = weekType;
        Name = name;
        ClassType = classType;
        TeacherName = teacherName;
        AudienceNumber = audienceNumber;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }

    public override string ToString()
    {
        string type = String.Empty;
        
        if (ClassType == ClassType.Lecture)
            type = "Лекція";
        else
            type = "Практика";
        
        string number = String.Empty;

        switch (Number)
        {
            case 1:
                number = "1⃣";
                break;
            case 2:
                number = "2⃣";
                break;
            case 3:
                number = "3⃣";
                break;
            case 4:
                number = "4⃣";
                break;
        }

        return $"\n======= <b>{number} пара</b> =======\n" +
               $"\n🕛 {StartTime.ToShortTimeString()} - {EndTime.ToShortTimeString()}\n" +
               $"<b>{Name}</b>. {type}\n\n" +
               $"Викладач: {TeacherName}\n" +
               $"Аудиторія: {AudienceNumber}" +
               $"\n======================";
        // return $"{GroupId}, Number: {Number}, WeekType: {WeekType}, {Name}, {ClassType}, {TeacherName}, {AudienceNumber}, " +
        //        $"{DayOfWeek}, {StartTime.ToShortTimeString()}, {EndTime.ToShortTimeString()}";
    }
}