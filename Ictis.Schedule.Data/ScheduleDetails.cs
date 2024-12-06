namespace Ictis.Schedule.Data;

public abstract record ScheduleDetails
{
    public string? Type { get; set; }

    public string? SearchName { get; set; }

    public int Week { get; set; }
}