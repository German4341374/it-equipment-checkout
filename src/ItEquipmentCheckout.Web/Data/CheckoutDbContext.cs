using ItEquipmentCheckout.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Data;

public sealed class CheckoutDbContext(DbContextOptions<CheckoutDbContext> options) : DbContext(options)
{
    public DbSet<Equipment> Equipment => Set<Equipment>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Checkout> Checkouts => Set<Checkout>();

    public DbSet<EquipmentEvent> EquipmentEvents => Set<EquipmentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.Property(item => item.InventoryNumber)
                .HasMaxLength(40)
                .UseCollation("NOCASE");
            entity.Property(item => item.Category)
                .HasConversion<string>()
                .HasMaxLength(32);
            entity.Property(item => item.Manufacturer).HasMaxLength(80);
            entity.Property(item => item.Model).HasMaxLength(100);
            entity.Property(item => item.SerialNumber)
                .HasMaxLength(100)
                .UseCollation("NOCASE");
            entity.Property(item => item.Status)
                .HasConversion<string>()
                .HasMaxLength(32);
            entity.HasIndex(item => item.InventoryNumber).IsUnique();
            entity.HasIndex(item => item.SerialNumber).IsUnique();
            entity.HasIndex(item => new { item.Status, item.Category });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Equipment_ExpectedReturnDays",
                "\"ExpectedReturnDays\" BETWEEN 1 AND 365"));
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(item => item.EmployeeNumber)
                .HasMaxLength(30)
                .UseCollation("NOCASE");
            entity.Property(item => item.FullName).HasMaxLength(120);
            entity.Property(item => item.Email)
                .HasMaxLength(254)
                .UseCollation("NOCASE");
            entity.Property(item => item.Department).HasMaxLength(100);
            entity.HasIndex(item => item.EmployeeNumber).IsUnique();
            entity.HasIndex(item => item.Email).IsUnique();
            entity.HasIndex(item => item.FullName);
        });

        modelBuilder.Entity<Checkout>(entity =>
        {
            entity.Property(item => item.Notes).HasMaxLength(500);
            entity.HasOne(item => item.Equipment)
                .WithMany(item => item.Checkouts)
                .HasForeignKey(item => item.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Employee)
                .WithMany(item => item.Checkouts)
                .HasForeignKey(item => item.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.EquipmentId, item.ActualReturnDate });
            entity.HasIndex(item => item.PlannedReturnDate);
            entity.HasIndex(item => item.EquipmentId)
                .IsUnique()
                .HasFilter("\"ActualReturnDate\" IS NULL");
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Checkout_PlannedReturnDate",
                    "\"PlannedReturnDate\" >= \"CheckoutDate\"");
                table.HasCheckConstraint(
                    "CK_Checkout_ActualReturnDate",
                    "\"ActualReturnDate\" IS NULL OR \"ActualReturnDate\" >= \"CheckoutDate\"");
            });
        });

        modelBuilder.Entity<EquipmentEvent>(entity =>
        {
            entity.Property(item => item.Type)
                .HasConversion<string>()
                .HasMaxLength(32);
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.HasIndex(item => new { item.EquipmentId, item.OccurredAt });
            entity.HasOne(item => item.Equipment)
                .WithMany(item => item.Events)
                .HasForeignKey(item => item.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Employee)
                .WithMany()
                .HasForeignKey(item => item.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(item => item.Checkout)
                .WithMany()
                .HasForeignKey(item => item.CheckoutId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Equipment>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.InventoryNumber = entry.Entity.InventoryNumber.Trim();
                entry.Entity.Manufacturer = entry.Entity.Manufacturer.Trim();
                entry.Entity.Model = entry.Entity.Model.Trim();
                entry.Entity.SerialNumber = entry.Entity.SerialNumber.Trim();
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Employee>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.EmployeeNumber = entry.Entity.EmployeeNumber.Trim();
                entry.Entity.FullName = entry.Entity.FullName.Trim();
                entry.Entity.Email = entry.Entity.Email.Trim().ToLowerInvariant();
                entry.Entity.Department = entry.Entity.Department.Trim();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
