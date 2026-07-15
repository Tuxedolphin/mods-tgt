using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Backend.Data;
using Backend.DTOs.Mappings;
using Backend.Exceptions;
using Backend.Hubs;
using Backend.Services.Auth;
using Backend.Services.Profiles;
using Backend.Services.Rooms;
using Backend.Services.Storage;
using Backend.Services.Timetables;
using Backend.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );

        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddOpenApi();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder
    .Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );
    });

// === Rate Limiter ===

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(
        "handle-check",
        httpContext =>
        {
            var key =
                httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                }
            );
        }
    );

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// === Exception Handling ===
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// === Settings ===
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("Supabase"));

var supabaseSettings = builder.Configuration.GetSection("Supabase").Get<SupabaseSettings>()!;

// === Supabase Connection ===

// Supabase Auth Configuration
var supabase = new Supabase.Client(
    supabaseSettings.Url,
    supabaseSettings.PublishableKey,
    new Supabase.SupabaseOptions { AutoRefreshToken = true, AutoConnectRealtime = false }
);
await supabase.InitializeAsync();

builder.Services.AddSingleton(supabase);

// Needed to prevent name collision, this uses the secretKey while the other uses
// PublishableKey
var storageSupabase = new Supabase.Client(
    supabaseSettings.Url,
    supabaseSettings.SecretKey,
    new Supabase.SupabaseOptions { AutoConnectRealtime = false }
);
await storageSupabase.InitializeAsync();

// Needed for avatar uploads, uses admin key
builder.Services.AddSingleton(new SupabaseStorageClient(storageSupabase));

// Needed for deleting users, uses admin key
builder.Services.AddSingleton(storageSupabase.AdminAuth(supabaseSettings.SecretKey));

builder.Services.AddHttpClient(
    "Gotrue",
    client =>
    {
        client.BaseAddress = new Uri($"{supabaseSettings.Url}/auth/v1/");
        client.DefaultRequestHeaders.Add("apikey", supabaseSettings.PublishableKey);
    }
);

// Supabase JWT Configuration
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{supabaseSettings.Url}/auth/v1";
        options.Audience = "authenticated";
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/room"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// Supabase Postgres Connection -- EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// === Application Services ===

builder.Services.AddScoped<ITimetableService, TimetableService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddSingleton<IAvatarUrlProvider, AvatarUrlProvider>();
builder.Services.AddSingleton<IProfileResponseMapper, ProfileResponseMapper>();
builder.Services.AddSingleton<IRoomTracker, RoomTracker>();
builder.Services.AddSingleton<IProfileTracker, ProfileTracker>();

// === Default setup ===

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseCors(b =>
        b.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()
    );
}
else
{
    app.UseCors(b =>
        b.WithOrigins("https://mods-tgt.com").AllowAnyHeader().AllowAnyMethod().AllowCredentials()
    );
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<RoomHub>("/hubs/room");

app.Run();
