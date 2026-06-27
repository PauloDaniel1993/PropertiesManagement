using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1062, CA1861, IDE0161

namespace Alsappan.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class PropertyModule : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "properties",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            street_line = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            street_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            address_complement = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
            neighborhood = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
            postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
            country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
            status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            suggested_rent_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            suggested_rent_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            garage_space_count = table.Column<int>(type: "integer", nullable: false),
            garage_space_identifiers = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
            notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            search_text = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
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
            table.PrimaryKey("pk_properties", x => x.id);
          });

      migrationBuilder.CreateIndex(
          name: "ix_properties_organization_id_deleted_at",
          schema: "app",
          table: "properties",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_properties_organization_id_garage_space_count",
          schema: "app",
          table: "properties",
          columns: new[] { "organization_id", "garage_space_count" });

      migrationBuilder.CreateIndex(
          name: "ix_properties_organization_id_name",
          schema: "app",
          table: "properties",
          columns: new[] { "organization_id", "name" });

      migrationBuilder.CreateIndex(
          name: "ix_properties_organization_id_search_text",
          schema: "app",
          table: "properties",
          columns: new[] { "organization_id", "search_text" });

      migrationBuilder.CreateIndex(
          name: "ix_properties_organization_id_status",
          schema: "app",
          table: "properties",
          columns: new[] { "organization_id", "status" });

      migrationBuilder.CreateIndex(
          name: "ix_properties_organization_id_type",
          schema: "app",
          table: "properties",
          columns: new[] { "organization_id", "type" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "properties",
          schema: "app");
    }
  }
}

#pragma warning restore CA1062, CA1861, IDE0161
