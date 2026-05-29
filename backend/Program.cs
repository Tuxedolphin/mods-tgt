using Backend.Data;
using Backend.Exceptions;
using Backend.Services;
using Backend.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
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
    app.UseCors(b => b.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod());
}
else
{
    app.UseCors(b => b.WithOrigins("http://mods-tgt.com").AllowAnyHeader().AllowAnyMethod());
}

app.UseExceptionHandler();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
