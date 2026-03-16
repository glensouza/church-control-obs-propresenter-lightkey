using Microsoft.EntityFrameworkCore;
using WorshipConsole.Components;
using WorshipConsole.Database;
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
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
