using Backend.Data;
using Backend.Services;
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

/// === Settings ===
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
    });
builder.Services.AddAuthorization();

// Supabase Postgres Connection -- EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// === Application Services ===

builder.Services.AddScoped<ITimeTableService, TimeTableService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// === Defauly setup ===

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
