using khai_schedule_bot.Tools;
using khai_schedule_bot.Models;

// AppDbContext _db = new AppDbContext();

// var defaultGroups = new Group[]
// {
//     new Group(5, 503, "515ст2", "515st2")
// };
//
// for (int i = 0; i < args.Length; i++)
// {
//     // Ex: ./khai_schedule_bot --parse-groups 503
//     if (args[i] == "--parse-groups")
//     {
//         if (args[i + 1] == "all")
//         {
//             foreach (var group in defaultGroups)
//             {
//                 _db.RecreateGroups(Parser.ParseGroups(group.Department));
//             }
//         } else 
//             _db.RecreateGroups(Parser.ParseGroups(int.Parse(args[i + 1])));
//     }
// }


Parser.ParseSchedule("515st2");


// using System.Reflection;
// using Telegram.Bot;
// using Telegram.Bot.Types;
// using Telegram.Bot.Polling;
// using Telegram.Bot.Exceptions;
// using Telegram.Bot.Types.Enums;
// using Microsoft.Extensions.Configuration;
//
// IConfiguration configuration = new ConfigurationBuilder()
//     .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
//     .Build();
//
// var token = configuration.GetValue<string>("BOT_TOKEN");
//
// if (token is null)
// {
//     Console.WriteLine("*** ТОКЕН НЕ НАЙДЕН ***");
//     return;
// }
// var botClient = new TelegramBotClient(token);
//
// using CancellationTokenSource cts = new ();
//
// // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
// ReceiverOptions receiverOptions = new ()
// {
//     AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
// };
//
// botClient.StartReceiving(
//     updateHandler: HandleUpdateAsync,
//     pollingErrorHandler: HandlePollingErrorAsync,
//     receiverOptions: receiverOptions,
//     cancellationToken: cts.Token
// );
//
// var me = await botClient.GetMeAsync();
//
// Console.WriteLine($"Start listening for @{me.Username}");
// Console.ReadLine();
//
// // Send cancellation request to stop bot
// cts.Cancel();
//
// async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
// {
//     // Only process Message updates: https://core.telegram.org/bots/api#message
//     if (update.Message is not { } message)
//         return;
//     // Only process text messages
//     if (message.Text is not { } messageText)
//         return;
//
//     var chatId = message.Chat.Id;
//
//     Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
//
//     // Echo received message text
//     Message sentMessage = await bot.SendTextMessageAsync(
//         chatId: chatId,
//         text: "You said:\n" + messageText,
//         cancellationToken: cancellationToken);
// }
//
// Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
// {
//     var ErrorMessage = exception switch
//     {
//         ApiRequestException apiRequestException
//             => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
//         _ => exception.ToString()
//     };
//
//     Console.WriteLine(ErrorMessage);
//     return Task.CompletedTask;
// }