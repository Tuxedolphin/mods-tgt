using System.Text.Json.Serialization;

namespace Backend.Models;

public class Profile
{
    [JsonIgnore]
    public Guid Id { get; set; }
    public string? Username { get; set; }
}
