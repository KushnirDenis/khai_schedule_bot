using Telegram.Bot;
using System.Reflection;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using khai_schedule_bot.Tools;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Configuration;
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
        
        foreach (var group in _db.Groups)
        {
            _db.Classes.AddRange(Parser.ParseClasses(group.EngCode));
        }
        _db.SaveChanges();
    }
}

IConfiguration configuration = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
    .Build();

var token = configuration.GetValue<string>("BOT_TOKEN");

if (token is null)
{
    Console.WriteLine("*** ТОКЕН НЕ НАЙДЕН ***");
    return;
}
var botClient = new TelegramBotClient(token);

using CancellationTokenSource cts = new ();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new ()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    // Echo received message text
    Message sentMessage = await bot.SendTextMessageAsync(
        chatId: chatId,
        text: "You said:\n" + messageText,
        cancellationToken: cancellationToken);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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