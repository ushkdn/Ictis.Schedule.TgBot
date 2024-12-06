using Ictis.Schedule.Data;
using Newtonsoft.Json;

namespace Ictis.Schedule.Services.ScheduleService;

public class ScheduleService : IScheduleService
{
    public ScheduleDayModel GetDayScheduleByDate(ScheduleWeekModel scheduleWeek, string date)
    {
        var scheduleDay = scheduleWeek.DaySchedules.FirstOrDefault(x => x.Date.Contains(date));
        scheduleDay.Week = scheduleWeek.Week;
        scheduleDay.SearchName = scheduleWeek.SearchName;
        scheduleDay.Type = scheduleWeek.Type;
        return scheduleDay;
    }

    public async Task<IctisScheduleApiResponse> GetSchedule(string apiRoute, string query)
    {
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, apiRoute + $"/?query={query}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var jsondata = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<IctisScheduleApiResponse>(jsondata)
                ?? throw new InvalidOperationException($"Unable to parse json to model: {nameof(IctisScheduleApiResponse)}");
            return parsedData;
        }
    }

    //todo: refactor and optimize
    public ScheduleWeekModel GetWeekSchedule(ScheduleTable scheduleTable)
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
            if (timePeriods.Any())
            {
                scheduleDayList.Add(new ScheduleDayModel
                {
                    Date = scheduleTable.Table[i, 0],
                    TimePeriods = timePeriods
                });
            }
        }

        weekScheduleMeta.DaySchedules = scheduleDayList;

        return weekScheduleMeta;
    }

    public string WeekScheduleToHtml(ScheduleWeekModel weekSchedule)
    {
        var responseString =
            $"<b>{weekSchedule.Type} {weekSchedule.SearchName}</b>\n" +
            $"Неделя: {weekSchedule.Week}\n\n";

        foreach (var day in weekSchedule.DaySchedules)
        {
            responseString += $"<b>{day.Date}</b>\n";
            foreach (var timePeriod in day.TimePeriods)
            {
                responseString +=
                    $"{timePeriod.Time} : {timePeriod.Name}\n";
            }
            responseString += "\n";
        }

        return responseString;
    }
}