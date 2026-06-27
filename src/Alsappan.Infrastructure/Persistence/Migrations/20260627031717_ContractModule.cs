using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1062, CA1861

namespace Alsappan.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ContractModule : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.CreateTable(
        name: "contracts",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          property_id = table.Column<Guid>(type: "uuid", nullable: false),
          primary_resident_id = table.Column<Guid>(type: "uuid", nullable: false),
          status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          start_date = table.Column<DateOnly>(type: "date", nullable: false),
          end_date = table.Column<DateOnly>(type: "date", nullable: true),
          monthly_rent_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
          monthly_rent_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
          due_day = table.Column<int>(type: "integer", nullable: false),
          deposit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
          deposit_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
          adjustment_index = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          adjustment_interval_months = table.Column<int>(type: "integer", nullable: false),
          next_adjustment_date = table.Column<DateOnly>(type: "date", nullable: true),
          penalty_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
          discount_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
          generate_payments_automatically = table.Column<bool>(type: "boolean", nullable: false),
          notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
          search_text = table.Column<string>(type: "character varying(2200)", maxLength: 2200, nullable: false),
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
          table.PrimaryKey("pk_contracts", x => x.id);
        });

    migrationBuilder.CreateTable(
        name: "contract_residents",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          contract_id = table.Column<Guid>(type: "uuid", nullable: false),
          resident_id = table.Column<Guid>(type: "uuid", nullable: false),
          is_primary = table.Column<bool>(type: "boolean", nullable: false),
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
          table.PrimaryKey("pk_contract_residents", x => x.id);
          table.ForeignKey(
                      name: "fk_contract_residents_contracts_contract_id",
                      column: x => x.contract_id,
                      principalSchema: "app",
                      principalTable: "contracts",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateIndex(
        name: "ix_contract_residents_contract_id",
        schema: "app",
        table: "contract_residents",
        column: "contract_id");

    migrationBuilder.CreateIndex(
        name: "ix_contract_residents_organization_id_contract_id",
        schema: "app",
        table: "contract_residents",
        columns: new[] { "organization_id", "contract_id" });

    migrationBuilder.CreateIndex(
        name: "ix_contract_residents_organization_id_deleted_at",
        schema: "app",
        table: "contract_residents",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_contract_residents_organization_id_is_primary",
        schema: "app",
        table: "contract_residents",
        columns: new[] { "organization_id", "is_primary" });

    migrationBuilder.CreateIndex(
        name: "ix_contract_residents_organization_id_resident_id",
        schema: "app",
        table: "contract_residents",
        columns: new[] { "organization_id", "resident_id" });

    migrationBuilder.CreateIndex(
        name: "ix_contract_residents_organization_id_resident_id_contract_id",
        schema: "app",
        table: "contract_residents",
        columns: new[] { "organization_id", "resident_id", "contract_id" },
        unique: true);

    migrationBuilder.CreateIndex(
        name: "ix_contracts_organization_id_deleted_at",
        schema: "app",
        table: "contracts",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_contracts_organization_id_end_date",
        schema: "app",
        table: "contracts",
        columns: new[] { "organization_id", "end_date" });

    migrationBuilder.CreateIndex(
        name: "ix_contracts_organization_id_primary_resident_id",
        schema: "app",
        table: "contracts",
        columns: new[] { "organization_id", "primary_resident_id" });

    migrationBuilder.CreateIndex(
        name: "ix_contracts_organization_id_property_id",
        schema: "app",
        table: "contracts",
        columns: new[] { "organization_id", "property_id" });

    migrationBuilder.CreateIndex(
        name: "ix_contracts_organization_id_search_text",
        schema: "app",
        table: "contracts",
        columns: new[] { "organization_id", "search_text" });

    migrationBuilder.CreateIndex(
        name: "ix_contracts_organization_id_start_date",
        schema: "app",
        table: "contracts",
        columns: new[] { "organization_id", "start_date" });

    migrationBuilder.CreateIndex(
        name: "ix_contracts_organization_id_status",
        schema: "app",
        table: "contracts",
        columns: new[] { "organization_id", "status" });
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.DropTable(
        name: "contract_residents",
        schema: "app");

    migrationBuilder.DropTable(
        name: "contracts",
        schema: "app");
  }
}
