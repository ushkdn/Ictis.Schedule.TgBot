namespace Ictis.Schedule.Data;

public record ScheduleWeekModel : ScheduleDetails
{
    public List<ScheduleDayModel>? DaySchedules { get; set; } = [];
}