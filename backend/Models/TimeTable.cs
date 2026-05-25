namespace Backend.Models;

public class TimeTableMetaData
{
    // TODO: placeholder, get exact details from Jordan
    public string Name { get; set; } = string.Empty;
}

public class TimeTable
{
    public Guid Id { get; set; }
    public TimeTableMetaData MetaData { get; set; } = new();
}
