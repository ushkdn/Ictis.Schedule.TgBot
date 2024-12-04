namespace Ictis.Schedule.Data;

public record ScheduleModel
{
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Week { get; set; }
    public List<string>? Periods { get; set; }
    public List<string>? Time { get; set; }
    public List<PeriodModel>? PeriodNames { get; set; }
}