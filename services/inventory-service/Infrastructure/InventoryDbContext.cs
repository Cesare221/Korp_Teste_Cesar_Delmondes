using Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<StockOperation> StockOperations => Set<StockOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(product => product.Id);

            entity.Property(product => product.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(product => product.Code)
                .HasColumnName("code")
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(product => product.Code)
                .IsUnique()
                .HasDatabaseName("ux_products_code");

            entity.Property(product => product.Description)
                .HasColumnName("description")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(product => product.Balance)
                .HasColumnName("balance")
                .IsRequired();

            entity.ToTable(table => table.HasCheckConstraint("ck_products_balance_non_negative", "balance >= 0"));

            entity.Property(product => product.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(product => product.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();
        });

        modelBuilder.Entity<StockOperation>(entity =>
        {
            entity.ToTable("stock_operations");

            entity.HasKey(operation => operation.OperationId);

            entity.Property(operation => operation.OperationId)
                .HasColumnName("operation_id")
                .ValueGeneratedNever();

            entity.Property(operation => operation.ProcessedAt)
                .HasColumnName("processed_at")
                .IsRequired();
        });
    }
}
