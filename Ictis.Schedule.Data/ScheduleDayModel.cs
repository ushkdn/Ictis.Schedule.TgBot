namespace Ictis.Schedule.Data;

public record ScheduleDayModel : ScheduleDetails
{
    public string? Date { get; set; }
    public List<TimePeriodModel>? TimePeriods { get; set; } = [];
}