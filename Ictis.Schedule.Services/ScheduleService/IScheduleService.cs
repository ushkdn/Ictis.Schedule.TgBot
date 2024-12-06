using Ictis.Schedule.Data;

namespace Ictis.Schedule.Services.ScheduleService
{
    public interface IScheduleService
    {
        Task<IctisScheduleApiResponse> GetSchedule(string apiRoute, string query);

        ScheduleWeekModel GetWeekSchedule(ScheduleTable scheduleTable);

        ScheduleDayModel GetDayScheduleByDate(ScheduleWeekModel scheduleWeek, string date);

        string WeekScheduleToHtml(ScheduleWeekModel weekSchedule);
    }
}