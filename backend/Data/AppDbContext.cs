using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TimeTable> TimeTables { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Profile>().ToTable("Profiles", t => t.ExcludeFromMigrations());
        modelBuilder
            .Entity<TimeTable>()
            .HasOne<Profile>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TimeTable>().OwnsMany(t => t.MetaData, builder => builder.ToJson());
        modelBuilder.Entity<TimeTable>().Property(t => t.CreatedAt).HasDefaultValueSql("now()");
    }
}
