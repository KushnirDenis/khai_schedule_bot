using khai_schedule_bot.Tools;
using Telegram.Bot;
using System.Reflection;
using System.Text;
using khai_schedule_bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types.ReplyMarkups;

namespace khai_schedule_bot;

public static class TelegramBot
{
    private static AppDbContext _db = new AppDbContext();
    private static Thread _thread = new Thread(Start);

    private static ReplyKeyboardMarkup _buttons = new(new[]
    {
        new KeyboardButton[] { "Пари на сьогодні" },
        new KeyboardButton[] { "Наступна пара" },
        new KeyboardButton[] { "Зареєструватись заново" },
    })
    {
        ResizeKeyboard = true
    };

    public static TelegramBotClient Bot;
    public static WeekType WeekType;

    public static void StartInNewThread()
    {
        _thread.Start();
    }

    public static void UpdateWeekType()
    {
        WeekType = Parser.GetWeekType();
    }

    public async static void Start()
    {
        UpdateWeekType();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            .Build();

        var token = configuration.GetValue<string>("BOT_TOKEN");

        if (token is null)
        {
            Console.WriteLine("*** ТОКЕН НЕ НАЙДЕН ***");
            return;
        }

        Bot = new TelegramBotClient(token);

        using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        Bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await Bot.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        // Send cancellation request to stop bot
        cts.Cancel();
    }

    private static async Task<BotUser?> GetUser(long chatId)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.ChatId == chatId);
    }

    private static async void RegisterUser(Chat chat)
    {
        var newUser = new BotUser()
        {
            ChatId = chat.Id,
            State = UserState.EntersName,
            TgFirstName = chat.FirstName,
            TgLastName = chat.LastName,
            TgUsername = chat.Username
        };

        _db.Users.Add(newUser);

        await _db.SaveChangesAsync();

        await Bot.SendTextMessageAsync(chat.Id, "Введіть ім'я та прізвище <b>через " +
                                                "пробіл</b> (напр. Іван Іванов)",
            ParseMode.Html);
    }

    private static async void RegisterUser(BotUser user, string message)
    {
        var names = message.Split(" ");

        foreach (var line in _buttons.Keyboard)
        {
            foreach (var button in line)
            {
                if (message == button.Text)
                {
                    await Bot.SendTextMessageAsync(user.ChatId, "Введіть ім'я та прізвище <b>через " +
                                                                "пробіл</b> (напр. Іван Іванов)",
                        ParseMode.Html);
                    return;
                }
            }
        }

        if (names.Length != 2)
        {
            await Bot.SendTextMessageAsync(user.ChatId, "Введіть ім'я та прізвище <b>через " +
                                                        "пробіл</b> (напр. Іван Іванов)",
                ParseMode.Html);
            return;
        }

        user.FirstName = names[0];
        user.LastName = names[1];
        user.State = UserState.ChoosesFaculty;

        _db.Users.Update(user);
        _db.SaveChanges();
    }

    private static async void ChooseFaculty(BotUser user, string message)
    {
        var dataSplit = message.Split(":");
        long chatId = long.MinValue;
        int choosedFaculty = Int32.MinValue;

        if (dataSplit.Length == 3 &&
            dataSplit[0] == "faculty" &&
            int.TryParse(dataSplit[2], out choosedFaculty) &&
            long.TryParse(dataSplit[1], out chatId))
        {
            user.Faculty = choosedFaculty;
            user.State = UserState.ChoosesDepartment;
            _db.Users.Update(user);
            _db.SaveChanges();
            ChooseDepartment(user, "");
            return;
        }

        var faculties = (from g in _db.Groups select g.Faculty).Distinct().ToList();
        faculties.Sort();

        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var faculty in faculties)
        {
            buttons.Add(
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: faculty.ToString(),
                        callbackData: $"faculty:{user.ChatId}:{faculty.ToString()}")
                });
        }

        InlineKeyboardMarkup inlineKeyboard = new(buttons);

        await Bot.SendTextMessageAsync(
            chatId: user.ChatId,
            text: "Оберіть факультет",
            replyMarkup: inlineKeyboard);
    }

    private static async void ChooseDepartment(BotUser user, string message)
    {
        var dataSplit = message.Split(":");
        long chatId = long.MinValue;
        int choosedDepartment = Int32.MinValue;

        if (dataSplit.Length == 3 &&
            dataSplit[0] == "department" &&
            int.TryParse(dataSplit[2], out choosedDepartment) &&
            long.TryParse(dataSplit[1], out chatId))
        {
            user.Department = choosedDepartment;
            user.State = UserState.ChoosesGroup;
            _db.Users.Update(user);
            _db.SaveChanges();
            ChooseGroup(user, "");
            return;
        }

        var departments = (from g in _db.Groups.Where(g => g.Faculty == user.Faculty)
            select g.Department).Distinct().ToList();
        ;

        departments.Sort();

        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var department in departments)
        {
            buttons.Add(
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: department.ToString(),
                        callbackData: $"department:{user.ChatId}:{department.ToString()}")
                });
        }

        InlineKeyboardMarkup inlineKeyboard = new(buttons);

        await Bot.SendTextMessageAsync(
            chatId: user.ChatId,
            text: "Оберіть кафедру",
            replyMarkup: inlineKeyboard);
    }

    private static async void ChooseGroup(BotUser user, string message)
    {
        var dataSplit = message.Split(":");
        long chatId = long.MinValue;

        if (dataSplit.Length == 3 &&
            dataSplit[0] == "group" &&
            long.TryParse(dataSplit[1], out chatId))
        {
            string choosedGroup = dataSplit[2];
            user.Group = _db.Groups.First(g => g.UaCode == choosedGroup);
            user.State = UserState.Registered;
            _db.Users.Update(user);
            _db.SaveChanges();

            await Bot.SendTextMessageAsync(user.ChatId, "Тепер ви можете отримувати розклад за допомогою кнопок 👇",
                replyMarkup: _buttons);
            return;
        }

        var groups = (from g in _db.Groups.Where(g =>
                g.Faculty == user.Faculty && g.Department == user.Department)
            select g.UaCode);
        ;

        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var group in groups)
        {
            buttons.Add(
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: group,
                        callbackData: $"group:{user.ChatId}:{group}")
                });
        }

        InlineKeyboardMarkup inlineKeyboard = new(buttons);

        await Bot.SendTextMessageAsync(
            chatId: user.ChatId,
            text: "Оберіть групу",
            replyMarkup: inlineKeyboard);
    }

    private static async void CheckState(long chatId, string message, Chat chat = default)
    {
        if (chatId == Int64.MinValue)
            chatId = chat.Id;

        var user = await GetUser(chatId);

        // Пользователя нет в базе
        if (user == null)
        {
            RegisterUser(chat);
            return;
        }
        // Пользователь есть в базе

        if (message == "/change" || message == "Зареєструватись заново")
        {
            user.State = UserState.EntersName;
            _db.SaveChanges();
        }

        if (user.State == UserState.EntersName)
        {
            RegisterUser(user, message);
        }

        if (user.State == UserState.ChoosesFaculty)
        {
            ChooseFaculty(user, message);
        }

        if (user.State == UserState.ChoosesDepartment)
        {
            ChooseDepartment(user, message);
        }

        if (user.State == UserState.ChoosesGroup)
        {
            ChooseGroup(user, message);
        }

        if (user.State == UserState.Registered)
        {
            if (message == "Наступна пара")
            {
                var now = DateTime.Now;
                var classes = _db.Classes
                    .FirstOrDefault(c => c.GroupId == user.GroupId &&
                                         c.DayOfWeek == now.DayOfWeek &&
                                         (c.WeekType == WeekType || c.WeekType == 0) &&
                                         (
                                             ((c.StartTime.Hour - now.Hour) > 0) ||
                                             (
                                                 ((c.StartTime.Hour - now.Hour) == 0) &&
                                                 ((c.StartTime.Minute - now.Minute) > 0)
                                             )
                                         ));


                if (classes is null)
                {
                    await Bot.SendTextMessageAsync(chatId, "Сьогодні пар більше немає 🎉",
                        ParseMode.Html);
                    return;
                }

                await Bot.SendTextMessageAsync(chatId, classes.ToString(), ParseMode.Html);
            }
            else if (message == "Пари на сьогодні")
            {
                var now = DateTime.Now;
                var classes = _db.Classes
                    .Where(c => c.GroupId == user.GroupId &&
                                c.DayOfWeek == now.DayOfWeek &&
                                (c.WeekType == WeekType || c.WeekType == 0))
                    .OrderBy(c => c.Number);

                if (classes.Count() == 0)
                {
                    await Bot.SendTextMessageAsync(chatId, "Пар сьогодні немає 🎉",
                        ParseMode.Html);
                    return;
                }

                StringBuilder sb = new StringBuilder();
                foreach (var c in classes)
                {
                    sb.Append(c + "\n");
                }

                await Bot.SendTextMessageAsync(chatId, sb.ToString(), ParseMode.Html);
            }
            else
            {
                await Bot.SendTextMessageAsync(user.ChatId, "Ви можете отримати розклад за допомогою кнопок 👇",
                    replyMarkup: _buttons);
            }
        }
    }

    private static async void CheckCallbackQuery(CallbackQuery callback)
    {
        var dataSplit = callback.Data.Split(":");
        if (dataSplit.Length != 3)
            return;

        long chatId = long.MinValue;

        if (!long.TryParse(dataSplit[1], out chatId))
            return;


        var user = _db.Users.FirstOrDefault(u => u.ChatId == chatId);

        if (user == null)
            RegisterUser(callback.Message.Chat);

        if (dataSplit[0] == "faculty")
            ChooseFaculty(user, callback.Data);
        if (dataSplit[0] == "department")
            ChooseDepartment(user, callback.Data);
        if (dataSplit[0] == "group")
            ChooseGroup(user, callback.Data);
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            CheckCallbackQuery(update.CallbackQuery);
            return;
        }

        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        var chat = message.Chat;

        CheckState(chat.Id, messageText, chat);
    }

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}