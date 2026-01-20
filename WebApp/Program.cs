using BLL;
using DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
homeDirectory += Path.DirectorySeparatorChar;

connectionString = connectionString.Replace("<db_file>", $"{homeDirectory}app.db");


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// custom dependencies
// AddScoped - instance is created once for web request
// AddSingleton - instance is created once
// AddTransient - new instance is created every time
// choose one: either EF based or file system based repo

builder.Services.AddScoped<IConfigRepository<GameConfiguration>, ConfigRepositoryEf>();
builder.Services.AddScoped<IGameRepository<GameBrain>, GameRepositoryEf>();

//builder.Services.AddScoped<IConfigRepository<GameConfiguration>, ConfigRepositoryJson>();
//builder.Services.AddScoped<IGameRepository<GameBrain>, GameRepositoryJson>();



builder.Services.AddScoped<IAiService, AiService>();

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.Run();

// Make Program class public for testing
public partial class Program { }