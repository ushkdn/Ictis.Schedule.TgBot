namespace Ictis.Schedule.Data;

public record PeriodModel
{
    public string? Date { get; set; }
    public List<string>? PeriodNames { get; set; }
}