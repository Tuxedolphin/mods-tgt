namespace Backend.Models;

public class Room
{
    public Guid Id { get; set; }

    public Visibility Visibility { get; set; } = Visibility.Restricted;
}

public enum Visibility
{
    PublicView,
    PublicEdit,
    Restricted,
}
