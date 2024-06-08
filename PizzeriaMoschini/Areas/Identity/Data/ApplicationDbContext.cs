using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PizzeriaMoschini.Models;

namespace PizzeriaMoschini.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<Staff> Staffs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationship between Reservation and Customer
        builder.Entity<Reservation>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reservations)
                .HasForeignKey(r => r.CustomerID)
                .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship between Reservation and Table
        builder.Entity<Reservation>()
            .HasOne(r => r.Table)
            .WithMany(t => t.Reservations)
            .HasForeignKey(r => r.TableID)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship between Reservation and Staff
        builder.Entity<Reservation>()
            .HasOne(r => r.Staff)
            .WithMany(s => s.Reservations)
            .HasForeignKey(r => r.StaffID)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
