using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Timetable> Timetables { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<RoomMember> RoomMembers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==== Profiles ====
        modelBuilder.Entity<Profile>().ToTable("Profiles", t => t.ExcludeFromMigrations());

        // ==== Timetables ====
        modelBuilder.Entity<Timetable>().ToTable("TimeTables");
        modelBuilder
            .Entity<Timetable>()
            .HasOne<Profile>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Timetable>().OwnsMany(t => t.MetaData, builder => builder.ToJson());
        modelBuilder
            .Entity<Timetable>()
            .Property(t => t.CreatedAt)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("now()");

        modelBuilder
            .Entity<Timetable>()
            .HasOne(t => t.OriginalTimetable)
            .WithMany()
            .HasForeignKey(t => t.OriginalTimetableId)
            .OnDelete(DeleteBehavior.SetNull);

        // ==== Rooms ====
        modelBuilder.Entity<Room>().ToTable("Rooms");
        modelBuilder.Entity<Room>().Property(r => r.Visibility).HasConversion<string>();

        // ==== RoomMembers ====
        modelBuilder.Entity<RoomMember>().ToTable("RoomMembers");
        modelBuilder.Entity<RoomMember>().HasKey(m => new { m.RoomId, m.UserId });

        modelBuilder.Entity<RoomMember>().Property(m => m.Role).HasConversion<string>();
        modelBuilder
            .Entity<RoomMember>()
            .HasOne(m => m.Room)
            .WithMany()
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder
            .Entity<RoomMember>()
            .HasOne<Profile>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
