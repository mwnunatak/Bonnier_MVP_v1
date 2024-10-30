using Microsoft.EntityFrameworkCore;
using BonPortalBackend.Data;
using BonPortalBackend.Middleware;
using BonPortalBackend.Services;
using Microsoft.Extensions.Configuration;
using System.IO;
using DotNetEnv;



// Load .env file at the start
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Get and configure database connection string
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
connectionString = connectionString
    .Replace("${DB_SERVER}", Env.GetString("DB_SERVER"))
    .Replace("${DB_NAME}", Env.GetString("DB_NAME"))
    .Replace("${DB_USER}", Env.GetString("DB_USER"))
    .Replace("${DB_PASSWORD}", Env.GetString("DB_PASSWORD"));

// Add configuration sources
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();  // This will include the .env variables

// Add services to the container
builder.Services.AddControllersWithViews();

// Register EmailService with configuration
builder.Services.AddScoped<IEmailService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<EmailService>>();
    var configuration = new ConfigurationBuilder()
        .AddConfiguration(builder.Configuration)
        .AddEnvironmentVariables()
        .Build();
    return new EmailService(configuration, logger);
});

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//Configure Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Add this line
    options.Cookie.SameSite = SameSiteMode.Lax; // Add this line
});


var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseDatabaseExceptionHandler();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<DatabaseExceptionMiddleware>();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add a test endpoint to verify configuration (remove in production)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/test-config", (IConfiguration configuration) =>
    {
        var emailConfig = new
        {
            ConnectionString = configuration["AZURE_COMMUNICATION_CONNECTION_STRING"]?.Substring(0, 20) + "...", // Only show beginning for security
            SenderEmail = configuration["AZURE_COMMUNICATION_SENDER_EMAIL"]
        };
        return Results.Ok(emailConfig);
    });
}

app.Run();