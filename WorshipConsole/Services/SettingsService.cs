using Microsoft.EntityFrameworkCore;
using WorshipConsole.Database;
using WorshipConsole.Models;

namespace WorshipConsole.Services;

public class SettingsService
{
    private readonly IDbContextFactory<PageantDb> dbFactory;
    private readonly IConfiguration configuration;

    public SettingsService(IDbContextFactory<PageantDb> dbFactory, IConfiguration configuration)
    {
        this.dbFactory = dbFactory;
        this.configuration = configuration;
    }

    public async Task<string> GetSettingAsync(string category, string key, string defaultValue = "")
    {
        await using PageantDb db = await this.dbFactory.CreateDbContextAsync();
        Settings? setting = await db.Settings.FirstOrDefaultAsync(s => s.Category == category && s.Key == key);
        if (setting != null) return setting.Value;

        // Fallback to configuration
        string configKey = string.IsNullOrEmpty(category) ? key : $"{category}:{key}";
        return this.configuration[configKey] ?? defaultValue;
    }

    public async Task<int> GetSettingIntAsync(string category, string key, int defaultValue = 0)
    {
        string value = await this.GetSettingAsync(category, key, defaultValue.ToString());
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    public async Task SaveSettingAsync(string category, string key, string value)
    {
        await using PageantDb db = await this.dbFactory.CreateDbContextAsync();
        Settings? setting = await db.Settings.FirstOrDefaultAsync(s => s.Category == category && s.Key == key);

        if (setting == null)
        {
            setting = new Settings { Category = category, Key = key, Value = value };
            db.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
            db.Settings.Update(setting);
        }

        await db.SaveChangesAsync();
    }

    public async Task InitializeFromConfigAsync()
    {
        await using PageantDb db = await this.dbFactory.CreateDbContextAsync();
        
        // OBS
        await this.EnsureSetting(db, "OBS", "Host", "127.0.0.1");
        await this.EnsureSetting(db, "OBS", "Port", "4455");

        // ProPresenter
        await this.EnsureSetting(db, "ProPresenter", "Host", "127.0.0.1");
        await this.EnsureSetting(db, "ProPresenter", "Port", "20000");
        await this.EnsureSetting(db, "ProPresenter", "MediaRootPath", "C:\\Media");
        await this.EnsureSetting(db, "ProPresenter", "FfmpegPath", "");
        await this.EnsureSetting(db, "ProPresenter", "WelcomeVideoFolder", "Welcome");
        await this.EnsureSetting(db, "ProPresenter", "WelcomeVideoFileName", "Welcome.mp4");
        await this.EnsureSetting(db, "ProPresenter", "BackgroundVideosFolder", "Backgrounds");
        await this.EnsureSetting(db, "ProPresenter", "YouTubeDownloadsFolder", "YouTube");

        // PCO
        await this.EnsureSetting(db, "Pco", "ProPresenterPosition", "ProPresenter");
        await this.EnsureSetting(db, "Pco", "LivestreamPosition", "Livestream");
        await this.EnsureSetting(db, "Pco", "WorshipCoordinatorPosition", "Worship Coordinator");

        await db.SaveChangesAsync();
    }

    private async Task EnsureSetting(PageantDb db, string category, string key, string defaultValue)
    {
        bool exists = await db.Settings.AnyAsync(s => s.Category == category && s.Key == key);
        if (!exists)
        {
            string configKey = $"{category}:{key}";
            string value = this.configuration[configKey] ?? defaultValue;
            db.Settings.Add(new Settings { Category = category, Key = key, Value = value });
        }
    }
}
