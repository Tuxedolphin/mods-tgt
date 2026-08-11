using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Timetable> Timetables { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<RoomMember> RoomMembers { get; set; } = null!;
    public DbSet<OptimiserPreference> OptimiserPreferences { get; set; } = null!;
    public DbSet<OptimiserResult> OptimiserResults { get; set; } = null!;

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

        // ==== OptimiserPreferences ====
        modelBuilder.Entity<OptimiserPreference>().ToTable("OptimiserPreferences");

        // Nulls not distinct so a user can only have one global row
        modelBuilder
            .Entity<OptimiserPreference>()
            .HasIndex(p => new { p.UserId, p.RoomId })
            .IsUnique()
            .AreNullsDistinct(false);

        modelBuilder
            .Entity<OptimiserPreference>()
            .HasOne<Profile>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder
            .Entity<OptimiserPreference>()
            .HasOne(p => p.Room)
            .WithMany()
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<OptimiserPreference>()
            .OwnsOne(
                p => p.Payload,
                builder =>
                {
                    builder.ToJson();
                    builder.OwnsOne(x => x.PreferredWindow);
                    builder.OwnsMany(x => x.LockedLessons);

                    builder.Property(x => x.LunchBreak).HasConversion<string>();
                    builder.Property(x => x.CompactDays).HasConversion<string>();
                    builder.Property(x => x.FewerCampusDays).HasConversion<string>();
                    builder.Property(x => x.FreeDay).HasConversion<string>();
                    builder.PrimitiveCollection(x => x.BlockedDays).ElementType().HasConversion<string>();
                }
            );

        // ==== OptimiserResults ====
        modelBuilder.Entity<OptimiserResult>().ToTable("OptimiserResults");
        modelBuilder.Entity<OptimiserResult>().HasKey(r => r.Id);

        // Nulls not distinct so a room has at most one group result and one solo
        // result per user
        modelBuilder
            .Entity<OptimiserResult>()
            .HasIndex(r => new { r.RoomId, r.UserId })
            .IsUnique()
            .AreNullsDistinct(false);
        modelBuilder
            .Entity<OptimiserResult>()
            .HasOne(r => r.Room)
            .WithMany()
            .HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OptimiserResult>().Property(r => r.PayloadJson).HasColumnType("jsonb");
        modelBuilder
            .Entity<OptimiserResult>()
            .Property(r => r.CreatedAt)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("now()");
    }
}
