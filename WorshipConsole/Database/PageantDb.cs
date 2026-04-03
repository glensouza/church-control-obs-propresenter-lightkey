using Microsoft.EntityFrameworkCore;
using WorshipConsole.Models;

namespace WorshipConsole.Database;

public class PageantDb(DbContextOptions<PageantDb> options) : DbContext(options)
{
    public DbSet<Script> Scripts => this.Set<Script>();
    public DbSet<Settings> Settings => this.Set<Settings>();
    public DbSet<CameraInfo> Cameras => this.Set<CameraInfo>();
    public DbSet<PageantCameraPreset> PageantCameraPresets => this.Set<PageantCameraPreset>();
    public DbSet<PageantObsScene> PageantObsScenes => this.Set<PageantObsScene>();
}
