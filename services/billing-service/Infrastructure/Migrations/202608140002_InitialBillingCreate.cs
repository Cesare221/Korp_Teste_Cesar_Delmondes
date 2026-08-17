using System;
using Billing.Api.Domain;
using Billing.Api.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Api.Infrastructure.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("202608140002_InitialBillingCreate")]
public partial class InitialBillingCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "billing");

        migrationBuilder.CreateSequence<long>(
            name: "invoice_number_seq",
            schema: "billing");

        migrationBuilder.CreateTable(
            name: "invoices",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                number = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('billing.invoice_number_seq')"),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_invoices", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "invoice_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                product_description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_invoice_items", x => x.id);
                table.CheckConstraint("ck_invoice_items_quantity_positive", "quantity > 0");
                table.ForeignKey(
                    name: "fk_invoice_items_invoices_invoice_id",
                    column: x => x.invoice_id,
                    principalTable: "invoices",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_invoice_items_invoice_id",
            table: "invoice_items",
            column: "invoice_id");

        migrationBuilder.CreateIndex(
            name: "ux_invoices_number",
            table: "invoices",
            column: "number",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "invoice_items");
        migrationBuilder.DropTable(name: "invoices");
        migrationBuilder.DropSequence(name: "invoice_number_seq", schema: "billing");
    }
}
