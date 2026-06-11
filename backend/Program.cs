using Backend.Data;
using Backend.Exceptions;
using Backend.Hubs;
using Backend.Services.Auth;
using Backend.Services.Profiles;
using Backend.Services.Room;
using Backend.Services.Timetables;
using Backend.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder.Services.AddSignalR();

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
builder.Services.AddSingleton<IRoomTracker, RoomTracker>();

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

app.MapControllers();
app.MapHub<RoomHub>("/hubs/room");

app.Run();
