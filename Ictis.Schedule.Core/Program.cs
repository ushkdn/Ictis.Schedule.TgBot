using ictis.schedule.core;
using Ictis.Schedule.Services.ScheduleService;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ictis.schedule.tgbot;

public static class TelegramB
{
    public static Dictionary<long, string> UserDates = new Dictionary<long, string>();
    public static Dictionary<long, string> UserChoices = new Dictionary<long, string>();
    public static Dictionary<long, string> UserGroups = new Dictionary<long, string>();

    public static async Task GetMe(string tgApiKey)
    {
        IScheduleService scheduleService = new ScheduleService();
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

        async Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Text == "/start")
            {
                await bot.SendMessage(msg.Chat, "Привет! Выбери опцию для получения расписания",
                    replyMarkup: new InlineKeyboardMarkup().AddButtons("День", "Неделя"));
            }
            else if (UserChoices.ContainsKey(msg.Chat.Id))
            {
                UserGroups[msg.Chat.Id] = msg.Text;

                if (UserChoices[msg.Chat.Id] == "День")
                {
                    await bot.SendMessage(msg.Chat, "Введите дату (например, 01 или 20):");
                }
                else if (UserChoices[msg.Chat.Id] == "Неделя")
                {
                    var apiResponseSchedule = await scheduleService.GetSchedule("https://webictis.sfedu.ru/schedule-api", UserGroups[msg.Chat.Id]);
                    var weekSchedule = scheduleService.GetWeekSchedule(apiResponseSchedule.Table);

                    var weekScheduleHTMLString = scheduleService.WeekScheduleToHtml(weekSchedule);
                    await bot.SendMessage(msg.Chat, $"{weekScheduleHTMLString}",
                        ParseMode.Html,
                        protectContent: false);

                    UserChoices.Remove(msg.Chat.Id);
                    UserGroups.Remove(msg.Chat.Id);
                }
            }
            else if (UserDates.ContainsKey(msg.Chat.Id))
            {
                UserDates[msg.Chat.Id] = msg.Text;
                string groupNumber = UserGroups[msg.Chat.Id];

                var apiResponseSchedule = await scheduleService.GetSchedule("https://webictis.sfedu.ru/schedule-api", UserGroups[msg.Chat.Id]);
                var weekSchedule = scheduleService.GetWeekSchedule(apiResponseSchedule.Table);
                var daySchedule = scheduleService.GetDayScheduleByDate(weekSchedule, UserDates[msg.Chat.Id]);
                await bot.SendMessage(msg.Chat, $"Расписание на {UserDates[msg.Chat.Id]} для группы {groupNumber}: {daySchedule}");

                UserDates.Remove(msg.Chat.Id);
                UserChoices.Remove(msg.Chat.Id);
                UserGroups.Remove(msg.Chat.Id);
            }
            else if (msg.Text.StartsWith("Группа: "))
            {
                var groupNumber = msg.Text.Substring(8);
                UserGroups[msg.Chat.Id] = groupNumber;

                await bot.SendMessage(msg.Chat, "Введите номер вашей группы или фамилию преподавателя:");
            }
        }

        async Task OnUpdate(Update update)
        {
            if (update.CallbackQuery is not null)
            {
                var query = update.CallbackQuery;

                if (query.Data == "День" || query.Data == "Неделя")
                {
                    await bot.SendMessage(query.Message.Chat, "Введите номер вашей группы или фамилию преподавателя:");
                    UserChoices[query.Message.Chat.Id] = query.Data;
                    UserDates[query.Message.Chat.Id] = "";
                    UserGroups[query.Message.Chat.Id] = "";
                }
            }
        }
    }
}

internal class Program
{
    private static async Task Main(string[] args)
    {
        var config = DotEnvLoad.Load();

        try
        {
            var tgBotApiKey = config["tgBotApiKey"]
                ?? throw new ArgumentException("Unable to configure tg-bot with specified key.");
            var scheduleApiRoute = config["ictisScheduleApiRoute"]
                ?? throw new ArgumentException("Unable to configure ictis schedule api route with specified key.");

            await TelegramB.GetMe("");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}