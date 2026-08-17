using Billing.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Billing.Api.Infrastructure;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>("invoice_number_seq", "billing")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");

            entity.HasKey(invoice => invoice.Id);

            entity.Property(invoice => invoice.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(invoice => invoice.Number)
                .HasColumnName("number")
                .HasDefaultValueSql("nextval('billing.invoice_number_seq')")
                .IsRequired();

            entity.HasIndex(invoice => invoice.Number)
                .IsUnique()
                .HasDatabaseName("ux_invoices_number");

            entity.Property(invoice => invoice.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(invoice => invoice.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(invoice => invoice.ClosedAt)
                .HasColumnName("closed_at");

            entity.HasMany(invoice => invoice.Items)
                .WithOne(item => item.Invoice)
                .HasForeignKey(item => item.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("invoice_items");

            entity.HasKey(item => item.Id);

            entity.Property(item => item.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(item => item.InvoiceId)
                .HasColumnName("invoice_id")
                .IsRequired();

            entity.Property(item => item.ProductId)
                .HasColumnName("product_id")
                .IsRequired();

            entity.Property(item => item.ProductCode)
                .HasColumnName("product_code")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(item => item.ProductDescription)
                .HasColumnName("product_description")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(item => item.Quantity)
                .HasColumnName("quantity")
                .IsRequired();

            entity.ToTable(table => table.HasCheckConstraint("ck_invoice_items_quantity_positive", "quantity > 0"));
        });
    }
}
