using Amazon.DynamoDBv2;
using HabitTracker.DynamoDb;
using HabitTracker.Habits;
using HabitTracker.Http;
using HabitTracker.UserInfo;
using HabitTracker.Utilities;

const string myAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            // TODO: limit to one stage?
            policy.WithOrigins("http://localhost:5173", "https://dev.habit-tracker.cloudchaotic.com", "https://habit-tracker.cloudchaotic.com")
                // allows cors preflight requests
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
        });
});

builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(8080);
});

// Add services to the container.
builder.Services.AddControllers();

// Habit Tracker controller dependencies
builder.Services.AddTransient<HabitRepository>();
builder.Services.AddScoped<DynamoDbContextWrapper>();
builder.Services.AddTransient<EnvironmentVariableGetter>();
builder.Services.AddTransient<UserInfoGetter>();
builder.Services.AddTransient<HttpClientWrapper>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseCors(myAllowSpecificOrigins);
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Welcome to running ASP.NET Core Minimal API on Amazon ECS");

app.Run();
