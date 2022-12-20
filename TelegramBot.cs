using khai_schedule_bot.Tools;
using Telegram.Bot;
using System.Reflection;
using khai_schedule_bot.Models;
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
    public static TelegramBotClient Bot;

    public static void StartInNewThread()
    {
        _thread.Start();
    }

    public async static void Start()
    {
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

    private static async BotUser GetUser(int chatId)
    {
        
    }
    
    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;
        // Only process text messages
        if (message.Text is not { } messageText)
            return;

        var chat = message.Chat;

        if (messageText == "/start")
        {
            if (GetUser(chat.Id) != null)
            {
                
            }
        }

        var chatId = message.Chat.Id;

        Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

        // Echo received message text
        Message sentMessage = await bot.SendTextMessageAsync(
            chatId: chatId,
            text: "You said:\n" + messageText,
            cancellationToken: cancellationToken);
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