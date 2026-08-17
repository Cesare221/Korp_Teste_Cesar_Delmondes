using System;
using Billing.Api.Domain;
using Billing.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Billing.Api.Infrastructure.Migrations;

[DbContext(typeof(BillingDbContext))]
partial class BillingDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.HasSequence<long>("invoice_number_seq", "billing");

        modelBuilder.Entity("Billing.Api.Domain.Invoice", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<DateTime?>("ClosedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("closed_at");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<long>("Number")
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint")
                .HasColumnName("number")
                .HasDefaultValueSql("nextval('billing.invoice_number_seq')");

            b.Property<InvoiceStatus>("Status")
                .HasMaxLength(20)
                .HasColumnType("character varying(20)")
                .HasColumnName("status");

            b.HasKey("Id");

            b.HasIndex("Number")
                .IsUnique()
                .HasDatabaseName("ux_invoices_number");

            b.ToTable("invoices");
        });

        modelBuilder.Entity("Billing.Api.Domain.InvoiceItem", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<Guid>("InvoiceId")
                .HasColumnType("uuid")
                .HasColumnName("invoice_id");

            b.Property<Guid>("ProductId")
                .HasColumnType("uuid")
                .HasColumnName("product_id");

            b.Property<string>("ProductCode")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying(50)")
                .HasColumnName("product_code");

            b.Property<string>("ProductDescription")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("product_description");

            b.Property<int>("Quantity")
                .HasColumnType("integer")
                .HasColumnName("quantity");

            b.HasKey("Id");

            b.HasIndex("InvoiceId");

            b.ToTable("invoice_items", t =>
            {
                t.HasCheckConstraint("ck_invoice_items_quantity_positive", "quantity > 0");
            });
        });

        modelBuilder.Entity("Billing.Api.Domain.InvoiceItem", b =>
        {
            b.HasOne("Billing.Api.Domain.Invoice", "Invoice")
                .WithMany("Items")
                .HasForeignKey("InvoiceId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Invoice");
        });

        modelBuilder.Entity("Billing.Api.Domain.Invoice", b =>
        {
            b.Navigation("Items");
        });
    }
}
