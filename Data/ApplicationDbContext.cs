using TippSendApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<B2BAccount> B2BAccounts => Set<B2BAccount>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Order>()
            .Property(o => o.ServiceFee).HasColumnType("decimal(10,2)");
        builder.Entity<Order>()
            .Property(o => o.EveningSundaySurcharge).HasColumnType("decimal(10,2)");
        builder.Entity<Order>()
            .Property(o => o.ItemMarkupAmount).HasColumnType("decimal(10,2)");
        builder.Entity<Order>()
            .Property(o => o.Total).HasColumnType("decimal(10,2)");
        builder.Entity<Order>()
            .Property(o => o.EstimatedItemPrice).HasColumnType("decimal(10,2)");
        builder.Entity<Order>()
            .Property(o => o.BudgetCap).HasColumnType("decimal(10,2)");
        builder.Entity<Order>()
            .Property(o => o.ActualItemPrice).HasColumnType("decimal(10,2)");
        builder.Entity<Order>()
            .Property(o => o.MarkupPct).HasColumnType("decimal(5,2)");
        builder.Entity<Order>()
            .Property(o => o.KmDriven).HasColumnType("decimal(8,2)");

        builder.Entity<B2BAccount>()
            .Property(b => b.MonthlyRetainer).HasColumnType("decimal(10,2)");
        builder.Entity<B2BAccount>()
            .Property(b => b.PerDeliveryRate).HasColumnType("decimal(10,2)");

        SeedData(builder);
    }

    private static void SeedData(ModelBuilder builder)
    {
        builder.Entity<Shop>().HasData(
            new Shop { Id = 1, Name = "Blooming Thing Floristry", Address = "27 O'Connell St", Town = "Clonmel", OpeningHours = "Mon–Sat 09:00–18:00", ContactPhone = "052 612 0001", PaymentNotes = "Cash on collection" },
            new Shop { Id = 2, Name = "Hickeys Bakery", Address = "14 Mitchel St", Town = "Clonmel", OpeningHours = "Mon–Sat 07:30–17:30", ContactPhone = "052 612 0002", PaymentNotes = "Cash on collection" },
            new Shop { Id = 3, Name = "Lonergan's Off Licence", Address = "8 Gladstone St", Town = "Clonmel", OpeningHours = "Mon–Sun 10:00–22:00", ContactPhone = "052 612 0003", PaymentNotes = "Cash on collection" },
            new Shop { Id = 4, Name = "Clonmel Pharmacy", Address = "51 O'Connell St", Town = "Clonmel", OpeningHours = "Mon–Fri 09:00–18:00, Sat 09:00–17:00", ContactPhone = "052 612 0004", PaymentNotes = "Card or cash" },
            new Shop { Id = 5, Name = "Cahir Crafts & Gifts", Address = "3 Castle St", Town = "Cahir", OpeningHours = "Mon–Sat 10:00–17:30", ContactPhone = "052 744 0001", PaymentNotes = "Cash on collection" },
            new Shop { Id = 6, Name = "Carrick Delicatessen", Address = "18 Main St", Town = "Carrick-on-Suir", OpeningHours = "Mon–Sat 08:00–18:00", ContactPhone = "051 640 0001", PaymentNotes = "Cash or card" },
            new Shop { Id = 7, Name = "Golden Vale Butchers", Address = "5 Parnell St", Town = "Clonmel", OpeningHours = "Mon–Sat 08:00–18:00", ContactPhone = "052 612 0005", PaymentNotes = "Cash on collection" },
            new Shop { Id = 8, Name = "Cashel Heritage Books", Address = "2 Main St", Town = "Cashel", OpeningHours = "Tue–Sat 10:00–17:00", ContactPhone = "062 610 0001", PaymentNotes = "Cash on collection" }
        );

        builder.Entity<B2BAccount>().HasData(
            new B2BAccount { Id = 1, BusinessName = "O'Brien Solicitors", ContactName = "Mary O'Brien", ContactEmail = "mary@obriensolicitors.ie", ContactPhone = "052 612 1001", Address = "12 Wellington St", Town = "Clonmel", MonthlyRetainer = 150m, PerDeliveryRate = 8m, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new B2BAccount { Id = 2, BusinessName = "Clonmel Medical Centre", ContactName = "Dr. Paul Ryan", ContactEmail = "admin@clonmelmc.ie", ContactPhone = "052 612 1002", Address = "45 Irishtown", Town = "Clonmel", MonthlyRetainer = 200m, PerDeliveryRate = 10m, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new B2BAccount { Id = 3, BusinessName = "Slievenamon Hotel", ContactName = "Claire Tobin", ContactEmail = "ops@slievehotel.ie", ContactPhone = "052 612 1003", Address = "Cahir Rd", Town = "Cahir", MonthlyRetainer = 120m, PerDeliveryRate = 9m, IsActive = true, CreatedAt = new DateTime(2026, 2, 1) }
        );
    }
}
