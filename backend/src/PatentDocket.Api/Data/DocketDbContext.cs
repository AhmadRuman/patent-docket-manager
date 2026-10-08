using Microsoft.EntityFrameworkCore;
using PatentDocket.Api.Domain;

namespace PatentDocket.Api.Data;

public class DocketDbContext(DbContextOptions<DocketDbContext> options) : DbContext(options)
{
    public DbSet<Attorney> Attorneys => Set<Attorney>();
    public DbSet<Matter> Matters => Set<Matter>();
    public DbSet<Deadline> Deadlines => Set<Deadline>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Attorney>(e =>
        {
            e.Property(a => a.Name).HasMaxLength(120);
            e.Property(a => a.Initials).HasMaxLength(5);
            e.Property(a => a.Email).HasMaxLength(200);
            e.Property(a => a.Role).HasMaxLength(40);
            e.HasIndex(a => a.Email).IsUnique();
        });

        b.Entity<Matter>(e =>
        {
            e.Property(m => m.DocketNumber).HasMaxLength(40);
            e.HasIndex(m => m.DocketNumber).IsUnique();
            e.Property(m => m.Title).HasMaxLength(300);
            e.Property(m => m.ClientName).HasMaxLength(200);
            e.Property(m => m.Jurisdiction).HasMaxLength(10);
            e.Property(m => m.ApplicationNumber).HasMaxLength(40);
            e.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(m => m.ResponsibleAttorney)
                .WithMany(a => a.Matters)
                .HasForeignKey(m => m.ResponsibleAttorneyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Deadline>(e =>
        {
            e.Property(d => d.Description).HasMaxLength(300);
            e.Property(d => d.Notes).HasMaxLength(2000);
            e.Property(d => d.Type).HasConversion<string>().HasMaxLength(50);
            e.HasOne(d => d.Matter)
                .WithMany(m => m.Deadlines)
                .HasForeignKey(d => d.MatterId)
                .OnDelete(DeleteBehavior.Cascade);
            // The docket's hot query: open deadlines in a date window.
            e.HasIndex(d => new { d.IsCompleted, d.DueDate });
        });
    }
}
