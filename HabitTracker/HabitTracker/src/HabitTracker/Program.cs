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
            policy.WithOrigins("http://localhost:5173", "https://dev.habit-tracker.cloudchaotic.com", "https://habit-tracker.cloudchaotic.com");
        });
});

// Add services to the container.
builder.Services.AddControllers();

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

// Habit Tracker controller dependencies
builder.Services.AddTransient<HabitRepository>();
builder.Services.AddScoped<DynamoDbContextWrapper>();
builder.Services.AddScoped<AmazonDynamoDBClient>();
builder.Services.AddTransient<EnvironmentVariableGetter>();
builder.Services.AddTransient<UserInfoGetter>();
builder.Services.AddTransient<HttpClientWrapper>();
builder.Services.AddHttpClient();

var app = builder.Build();


app.UseHttpsRedirection();
app.UseCors(myAllowSpecificOrigins);
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Welcome to running ASP.NET Core Minimal API on AWS Lambda");

app.Run();
