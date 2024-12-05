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
    private static Dictionary<long, string> UserDates = new Dictionary<long, string>();
    private static Dictionary<long, string> UserChoices = new Dictionary<long, string>();
    private static Dictionary<long, string> UserGroups = new Dictionary<long, string>();

    private static async Task Main(string[] args)
    {
        var config = DotEnvLoad.Load();

        try
        {
            var tgBotApiKey = config["tgBotApiKey"]
                ?? throw new ArgumentNullException("Unable to configure tg-bot with specified key.");
            await GetMe();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static async Task<ScheduleWeekModel> GetSchedule(string key)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://webictis.sfedu.ru/schedule-api/?query=" + key.Trim());
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var jsondata = await response.Content.ReadAsStringAsync();
        var parsedData = JsonConvert.DeserializeObject<IctisScheduleApiResponse>(jsondata);
        return GetWeekSchedule(parsedData.Table);
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
                Date = scheduleTable.Table[i, 0],
                TimePeriods = timePeriods
            });
        }

        weekScheduleMeta.DaySchedules = scheduleDayList;

        return weekScheduleMeta;
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

        async Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Text == "/start")
            {
                await bot.SendMessage(msg.Chat, "Привет! Выбери опцию для получения расписания",
                    replyMarkup: new InlineKeyboardMarkup().AddButtons("День", "Неделя"));
            }
            else if (UserChoices.ContainsKey(msg.Chat.Id))
            {
                // Обработка введенной группы или преподавателя
                UserGroups[msg.Chat.Id] = msg.Text; // Сохраняем номер группы или фамилию преподавателя

                if (UserChoices[msg.Chat.Id] == "День")
                {
                    // Запрашиваем дату
                    await bot.SendMessage(msg.Chat, "Введите дату (например, 01 или 20):");
                }
                else if (UserChoices[msg.Chat.Id] == "Неделя")
                {
                    // Получаем расписание на неделю сразу
                    var weekSchedule = await GetSchedule(UserGroups[msg.Chat.Id]);
                    var responseString =
                        $"{weekSchedule.Type}  {weekSchedule.SearchName}: \n" +
                        $"Неделя: {weekSchedule.Week}\n";

                    foreach (var day in weekSchedule.DaySchedules)
                    {
                        responseString += $"{day.Date}\n\n";
                        foreach (var timePeriod in day.TimePeriods)
                        {
                            responseString +=
                            $"{timePeriod.Time} : {timePeriod.Name}\n\n";
                        }
                    }
                    await bot.SendMessage(msg.Chat, $"{weekSchedule.SearchName} + {weekSchedule.Type}: \n" + responseString);

                    // Очистка данных после обработки
                    UserChoices.Remove(msg.Chat.Id);
                    UserGroups.Remove(msg.Chat.Id);
                }
            }
            else if (UserDates.ContainsKey(msg.Chat.Id))
            {
                // Обработка введенной даты
                UserDates[msg.Chat.Id] = msg.Text; // Сохраняем введенную дату
                string groupNumber = UserGroups[msg.Chat.Id]; // Получаем номер группы

                // Получаем расписание на день
                var daySchedule = await GetDaySchedule(UserDates[msg.Chat.Id], groupNumber);
                await bot.SendMessage(msg.Chat, $"Расписание на {UserDates[msg.Chat.Id]} для группы {groupNumber}: {daySchedule}");

                // Очистка данных после обработки
                UserDates.Remove(msg.Chat.Id);
                UserChoices.Remove(msg.Chat.Id);
                UserGroups.Remove(msg.Chat.Id);
            }
            else if (msg.Text.StartsWith("Группа: "))
            {
                var groupNumber = msg.Text.Substring(8); // Извлечение номера группы
                UserGroups[msg.Chat.Id] = groupNumber; // Сохраняем номер группы

                // Запрашиваем дату или фамилию преподавателя в зависимости от выбора
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
                    await bot.SendMessage(query.Message.Chat, "Введите номер вашей группы:");
                    UserChoices[query.Message.Chat.Id] = query.Data; // Сохраняем выбор пользователя
                    UserDates[query.Message.Chat.Id] = ""; // Инициализация для даты
                    UserGroups[query.Message.Chat.Id] = ""; // Инициализация для группы
                }
            }
        }
    }

    public static async Task<ScheduleDayModel> GetDaySchedule(string date, string groupNumber)
    {
        // Здесь должна быть ваша логика для получения расписания на день по дате и номеру группы.

        return new ScheduleDayModel(); // Верните полученное расписание.
    }
}