using Microsoft.EntityFrameworkCore;
using WorshipConsole.Components;
using WorshipConsole.Database;
using WorshipConsole.Models;
using WorshipConsole.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string defaultDbPath = Path.Combine(builder.Environment.ContentRootPath, "pageant.db");
string connectionString = builder.Configuration.GetConnectionString("PageantDb") ?? $"Data Source={defaultDbPath}";
builder.Services.AddDbContextFactory<PageantDb>(options => options.UseSqlite(connectionString));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<UniFiService>();
builder.Services.AddSingleton<ViscaService>();
builder.Services.AddScoped<ObsWebSocketService>();
builder.Services.AddHttpClient<PcoApiService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    IDbContextFactory<PageantDb> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PageantDb>>();
    await using PageantDb db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();

    // Seed Cameras if none exist
    if (!await db.Cameras.AnyAsync())
    {
        var env = app.Environment;
        if (env.IsDevelopment())
        {
            db.Cameras.AddRange(new[]
            {
                new CameraInfo { Name = "Camera 1 (Wide)", IpAddress = "192.168.1.101", ViscaPort = 5678, UniFiPortNumber = 1, PanSpeed = 10, TiltSpeed = 10, ZoomSpeed = 4, NumberOfPresets = 9 },
                new CameraInfo { Name = "Camera 2 (Pulpit)", IpAddress = "192.168.1.102", ViscaPort = 5678, UniFiPortNumber = 2, PanSpeed = 8, TiltSpeed = 8, ZoomSpeed = 3, NumberOfPresets = 9 },
                new CameraInfo { Name = "Camera 3", IpAddress = "192.168.1.103", ViscaPort = 5678, UniFiPortNumber = 3, PanSpeed = 10, TiltSpeed = 10, ZoomSpeed = 4, NumberOfPresets = 9 },
                new CameraInfo { Name = "Camera 4", IpAddress = "192.168.1.104", ViscaPort = 5678, UniFiPortNumber = 4, PanSpeed = 10, TiltSpeed = 10, ZoomSpeed = 4, NumberOfPresets = 9 }
            });
        }
        else
        {
            db.Cameras.AddRange(new[]
            {
                new CameraInfo { Name = "Camera 1", IpAddress = "", ViscaPort = 5678, UniFiPortNumber = null, PanSpeed = 10, TiltSpeed = 10, ZoomSpeed = 4, NumberOfPresets = 9 },
                new CameraInfo { Name = "Camera 2", IpAddress = "", ViscaPort = 5678, UniFiPortNumber = null, PanSpeed = 10, TiltSpeed = 10, ZoomSpeed = 4, NumberOfPresets = 9 },
                new CameraInfo { Name = "Camera 3", IpAddress = "", ViscaPort = 5678, UniFiPortNumber = null, PanSpeed = 10, TiltSpeed = 10, ZoomSpeed = 4, NumberOfPresets = 9 },
                new CameraInfo { Name = "Camera 4", IpAddress = "", ViscaPort = 5678, UniFiPortNumber = null, PanSpeed = 10, TiltSpeed = 10, ZoomSpeed = 4, NumberOfPresets = 9 }
            });
        }
        await db.SaveChangesAsync();
    }
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
