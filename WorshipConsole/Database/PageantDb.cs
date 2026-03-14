using Microsoft.EntityFrameworkCore;
using WorshipConsole.Models;

namespace WorshipConsole.Database;

public class PageantDb : DbContext
{
    public PageantDb(DbContextOptions options) : base(options) { }

    public DbSet<Scripts> Scripts => this.Set<Scripts>();
    public DbSet<Settings> Settings => this.Set<Settings>();
}
