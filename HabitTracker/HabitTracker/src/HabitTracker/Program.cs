using HabitTracker.DynamoDb;
using HabitTracker.Habits;
using HabitTracker.Http;
using HabitTracker.LettuceEncrypt;
using HabitTracker.UserInfo;
using HabitTracker.Utilities;
using LettuceEncrypt;
using LettuceEncrypt.Accounts;

const string myAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            var corsOrigins = EnvironmentVariableGetter.Get("CORS_ORIGINS").Split(",");
            policy.WithOrigins(corsOrigins)
              // allow cors preflight requests
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
        });
});

builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(8080);
});

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseKestrel(options =>
    {
        var appServices = options.ApplicationServices;
        options.ListenAnyIP(8081, listenOptions => listenOptions.UseHttps(o => o.UseLettuceEncrypt(appServices)));
    });
}
else
{
    builder.WebHost.UseKestrel(options =>
    {
        options.ListenAnyIP(8081, listenOptions => listenOptions.UseHttps());
    });
}

// Add services to the container.
builder.Services.AddControllers();

// Habit Tracker controller dependencies
builder.Services.AddTransient<HabitRepository>();
builder.Services.AddScoped<DynamoDbContextWrapper>();
builder.Services.AddTransient<UserInfoGetter>();
builder.Services.AddTransient<HttpClientWrapper>();
builder.Services.AddLettuceEncrypt(options =>
{
    options.AcceptTermsOfService = true;
    options.DomainNames = new string[] { EnvironmentVariableGetter.Get("DOMAIN_NAME") };
    options.EmailAddress = "erikandresall@gmail.com";
});
builder.Services.AddTransient<ICertificateRepository, CertificateRepository>();
builder.Services.AddTransient<ICertificateSource, CertificateSource>();
builder.Services.AddTransient<IAccountStore, AccountStore>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(myAllowSpecificOrigins);
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Welcome to running ASP.NET Core Minimal API on Amazon ECS");

app.Run();
