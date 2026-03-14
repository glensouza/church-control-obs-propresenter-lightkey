using Microsoft.EntityFrameworkCore;
using WorshipConsole.Models;

namespace WorshipConsole.Database;

public class PageantDb : DbContext
{
    public PageantDb(DbContextOptions<PageantDb> options) : base(options) { }

    public DbSet<Script> Scripts => this.Set<Script>();
    public DbSet<Settings> Settings => this.Set<Settings>();
}
