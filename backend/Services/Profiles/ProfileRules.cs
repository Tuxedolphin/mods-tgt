namespace Backend.Services.Profiles;

public static class ProfileRules
{
    public const int MinLength = 4;
    public const int MaxLength = 16;

    public const string BasicUsernamePattern = "^[a-zA-Z0-9 _-]+$";

    public const string DetailedHandlePattern = "^[a-z](?:[a-z0-9]|[_-](?![_-]))*[a-z0-9]$";
}
