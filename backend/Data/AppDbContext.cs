using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Timetable> Timetables { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Timetable>().ToTable("TimeTables");
        modelBuilder.Entity<Profile>().ToTable("Profiles", t => t.ExcludeFromMigrations());
        modelBuilder
            .Entity<Timetable>()
            .HasOne<Profile>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Timetable>().OwnsMany(t => t.MetaData, builder => builder.ToJson());
        modelBuilder
            .Entity<Timetable>()
            .Property(t => t.CreatedAt)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("now()");
    }
}
