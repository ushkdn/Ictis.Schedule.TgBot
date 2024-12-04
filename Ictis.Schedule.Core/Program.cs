using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using ictis.schedule.core;

namespace ictis.schedule.tgbot;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var config = DotEnvLoad.Load();

        try
        {
            var tgBotApiKey = config["tgBotApiKey"]
                ?? throw new ArgumentNullException("Unable to configure tg-bot with specified key.");
            await GetMe(config["tgBotApiKey"]);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static async Task GetMe(string tgApiKey)
    {
        using var cts = new CancellationTokenSource();
        var bot = new TelegramBotClient(tgApiKey, cancellationToken: cts.Token);
        var me = await bot.GetMe();
        bot.OnMessage += OnMessage;
        bot.OnError += OnError;
        bot.OnUpdate += OnUpdate;

        Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
        Console.ReadLine();
        cts.Cancel(); // stop the bot

        async Task OnError(Exception ex, HandleErrorSource source)
        {
            Console.WriteLine(ex);
        }

        // method that handle messages received by the bot:
        async Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Text == "/start")
            {
                await bot.SendMessage(msg.Chat, "Привет! Выбери опцию для получения расписания",
                    replyMarkup: new InlineKeyboardMarkup().AddButtons("Группа", "Преподаватель"));
                //await bot.SendMessage(msg.Chat, "Привет! Выбери временной период для получения расписания",
                //    replyMarkup: new InlineKeyboardMarkup().AddButtons("День", "Неделя", "Максимальное на данный момент"));
            }
        }
        async Task OnUpdate(Update update)
        {
            if (update is { CallbackQuery: { } query })
            {
                await bot.AnswerCallbackQuery(query.Id, $"You piсked {query.Data}");
                await bot.SendMessage(query.Message!.Chat, $"User {query.From} clicked on {query.Data}");
            }
        }
    }
}