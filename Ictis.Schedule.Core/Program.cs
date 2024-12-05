using ictis.schedule.core;
using Ictis.Schedule.Data;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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

    public static async Task<IctisScheduleApiResponse> GetSchedule(string key)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://webictis.sfedu.ru/schedule-api/?query=" + key.Trim());
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var jsondata = await response.Content.ReadAsStringAsync();
        var parsedData = JsonConvert.DeserializeObject<IctisScheduleApiResponse>(jsondata);
        GetWeekSchedule(parsedData.Table);
        return null;
    }

    private static ScheduleWeekModel GetWeekSchedule(ScheduleTable scheduleTable)
    {
        var weekScheduleMeta = new ScheduleWeekModel
        {
            Type = scheduleTable.Type,
            Week = scheduleTable.Week,
            SearchName = scheduleTable.Name,
        };

        var scheduleDayList = new List<ScheduleDayModel>();

        for (int i = 2; i < scheduleTable.Table.GetLength(0); i++)
        {
            var timePeriods = new List<TimePeriodModel>();
            for (int j = 1; j < scheduleTable.Table.GetLength(1); j++)
            {
                if (!string.IsNullOrWhiteSpace(scheduleTable.Table[i, j]))
                {
                    var timePeriod = new TimePeriodModel
                    {
                        Number = scheduleTable.Table[0, j], 
                        Time = scheduleTable.Table[1, j],   
                        Name = scheduleTable.Table[i, j]   
                    };

                    timePeriods.Add(timePeriod);
                }
            }
            scheduleDayList.Add(new ScheduleDayModel
            {
                Date = scheduleTable.Table[i,0],
                TimePeriods = timePeriods
            });


        }
        weekScheduleMeta.DaySchedules = scheduleDayList;
        var test = weekScheduleMeta;

        return weekScheduleMeta;
    }

    private static async Task GetMe(string tgApiKey)
    {
        var smth = await GetSchedule("ктбо3-7");

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