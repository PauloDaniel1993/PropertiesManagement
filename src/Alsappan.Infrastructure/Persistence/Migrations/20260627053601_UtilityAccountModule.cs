using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161, CA1062, CA1861

namespace Alsappan.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class UtilityAccountModule : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "utility_accounts",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            property_id = table.Column<Guid>(type: "uuid", nullable: true),
            contract_id = table.Column<Guid>(type: "uuid", nullable: true),
            resident_id = table.Column<Guid>(type: "uuid", nullable: true),
            type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            responsibility = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            billing_period_start = table.Column<DateOnly>(type: "date", nullable: false),
            billing_period_end = table.Column<DateOnly>(type: "date", nullable: false),
            due_date = table.Column<DateOnly>(type: "date", nullable: false),
            amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            paid_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            paid_on = table.Column<DateOnly>(type: "date", nullable: true),
            payment_method = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
            bank_reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
            status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
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
            table.PrimaryKey("pk_utility_accounts", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "utility_document_links",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            utility_account_id = table.Column<Guid>(type: "uuid", nullable: false),
            document_id = table.Column<Guid>(type: "uuid", nullable: false),
            kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
            table.PrimaryKey("pk_utility_document_links", x => x.id);
            table.ForeignKey(
                      name: "fk_utility_document_links_utility_accounts_utility_account_id",
                      column: x => x.utility_account_id,
                      principalSchema: "app",
                      principalTable: "utility_accounts",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_billing_period_start_billi~",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "billing_period_start", "billing_period_end" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_contract_id",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "contract_id" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_deleted_at",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_due_date",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "due_date" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_property_id",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "property_id" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_resident_id",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "resident_id" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_responsibility",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "responsibility" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_search_text",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "search_text" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_status",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "status" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_accounts_organization_id_type",
          schema: "app",
          table: "utility_accounts",
          columns: new[] { "organization_id", "type" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_document_links_organization_id_deleted_at",
          schema: "app",
          table: "utility_document_links",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_document_links_organization_id_document_id",
          schema: "app",
          table: "utility_document_links",
          columns: new[] { "organization_id", "document_id" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_document_links_organization_id_utility_account_id",
          schema: "app",
          table: "utility_document_links",
          columns: new[] { "organization_id", "utility_account_id" });

      migrationBuilder.CreateIndex(
          name: "ix_utility_document_links_organization_id_utility_account_id_d~",
          schema: "app",
          table: "utility_document_links",
          columns: new[] { "organization_id", "utility_account_id", "document_id", "kind" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_utility_document_links_utility_account_id",
          schema: "app",
          table: "utility_document_links",
          column: "utility_account_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "utility_document_links",
          schema: "app");

      migrationBuilder.DropTable(
          name: "utility_accounts",
          schema: "app");
    }
  }
}
