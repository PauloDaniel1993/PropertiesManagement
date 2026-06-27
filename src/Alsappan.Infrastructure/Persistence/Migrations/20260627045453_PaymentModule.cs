using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161, CA1062, CA1861

namespace Alsappan.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class PaymentModule : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "payment_charges",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            contract_id = table.Column<Guid>(type: "uuid", nullable: true),
            property_id = table.Column<Guid>(type: "uuid", nullable: true),
            resident_id = table.Column<Guid>(type: "uuid", nullable: true),
            utility_account_id = table.Column<Guid>(type: "uuid", nullable: true),
            title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            due_date = table.Column<DateOnly>(type: "date", nullable: false),
            amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            discount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            penalty_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            penalty_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            preferred_method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            reconciliation_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            provider_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
            provider_reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
            provider_metadata_json = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
            notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            search_text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
            concurrency_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            organization_id = table.Column<Guid>(type: "uuid", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_payment_charges", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "payment_receipt_links",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            charge_id = table.Column<Guid>(type: "uuid", nullable: false),
            document_id = table.Column<Guid>(type: "uuid", nullable: false),
            label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
            concurrency_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            organization_id = table.Column<Guid>(type: "uuid", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_payment_receipt_links", x => x.id);
            table.ForeignKey(
                      name: "fk_payment_receipt_links_payment_charges_charge_id",
                      column: x => x.charge_id,
                      principalSchema: "app",
                      principalTable: "payment_charges",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "payment_transactions",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            charge_id = table.Column<Guid>(type: "uuid", nullable: false),
            amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            settled_on = table.Column<DateOnly>(type: "date", nullable: false),
            bank_reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
            provider_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
            provider_reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
            receipt_document_id = table.Column<Guid>(type: "uuid", nullable: true),
            notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            concurrency_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            organization_id = table.Column<Guid>(type: "uuid", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_payment_transactions", x => x.id);
            table.ForeignKey(
                      name: "fk_payment_transactions_payment_charges_charge_id",
                      column: x => x.charge_id,
                      principalSchema: "app",
                      principalTable: "payment_charges",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "ix_payment_charges_organization_id_contract_id",
          schema: "app",
          table: "payment_charges",
          columns: new[] { "organization_id", "contract_id" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_charges_organization_id_deleted_at",
          schema: "app",
          table: "payment_charges",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_charges_organization_id_due_date",
          schema: "app",
          table: "payment_charges",
          columns: new[] { "organization_id", "due_date" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_charges_organization_id_property_id",
          schema: "app",
          table: "payment_charges",
          columns: new[] { "organization_id", "property_id" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_charges_organization_id_provider_code_provider_refe~",
          schema: "app",
          table: "payment_charges",
          columns: new[] { "organization_id", "provider_code", "provider_reference" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_charges_organization_id_resident_id",
          schema: "app",
          table: "payment_charges",
          columns: new[] { "organization_id", "resident_id" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_charges_organization_id_search_text",
          schema: "app",
          table: "payment_charges",
          columns: new[] { "organization_id", "search_text" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_charges_organization_id_status",
          schema: "app",
          table: "payment_charges",
          columns: new[] { "organization_id", "status" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_charges_organization_id_utility_account_id",
          schema: "app",
          table: "payment_charges",
          columns: new[] { "organization_id", "utility_account_id" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_receipt_links_charge_id",
          schema: "app",
          table: "payment_receipt_links",
          column: "charge_id");

      migrationBuilder.CreateIndex(
          name: "ix_payment_receipt_links_organization_id_charge_id",
          schema: "app",
          table: "payment_receipt_links",
          columns: new[] { "organization_id", "charge_id" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_receipt_links_organization_id_charge_id_document_id",
          schema: "app",
          table: "payment_receipt_links",
          columns: new[] { "organization_id", "charge_id", "document_id" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_payment_receipt_links_organization_id_deleted_at",
          schema: "app",
          table: "payment_receipt_links",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_receipt_links_organization_id_document_id",
          schema: "app",
          table: "payment_receipt_links",
          columns: new[] { "organization_id", "document_id" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_transactions_charge_id",
          schema: "app",
          table: "payment_transactions",
          column: "charge_id");

      migrationBuilder.CreateIndex(
          name: "ix_payment_transactions_organization_id_charge_id",
          schema: "app",
          table: "payment_transactions",
          columns: new[] { "organization_id", "charge_id" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_transactions_organization_id_deleted_at",
          schema: "app",
          table: "payment_transactions",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_transactions_organization_id_provider_code_provider~",
          schema: "app",
          table: "payment_transactions",
          columns: new[] { "organization_id", "provider_code", "provider_reference" });

      migrationBuilder.CreateIndex(
          name: "ix_payment_transactions_organization_id_settled_on",
          schema: "app",
          table: "payment_transactions",
          columns: new[] { "organization_id", "settled_on" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "payment_receipt_links",
          schema: "app");

      migrationBuilder.DropTable(
          name: "payment_transactions",
          schema: "app");

      migrationBuilder.DropTable(
          name: "payment_charges",
          schema: "app");
    }
  }
}
