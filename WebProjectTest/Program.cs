using Microsoft.EntityFrameworkCore;
using WebProjectTest.Data;
using Microsoft.Extensions.Configuration;
using System.IO;
using DotNetEnv;

// Load .env file
DotNetEnv.Env.Load();

Console.WriteLine($"DB_SERVER: {Env.GetString("DB_SERVER")}");
Console.WriteLine($"DB_NAME: {Env.GetString("DB_NAME")}");

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Replace the variables with actual values
connectionString = connectionString
    .Replace("${DB_SERVER}", Env.GetString("DB_SERVER"))
    .Replace("${DB_NAME}", Env.GetString("DB_NAME"))
    .Replace("${DB_USER}", Env.GetString("DB_USER"))
    .Replace("${DB_PASSWORD}", Env.GetString("DB_PASSWORD"));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Rest of your code remains the same...

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();