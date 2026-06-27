using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF Core migrations intentionally use generated array literals.

namespace Alsappan.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class VehicleModule : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    ArgumentNullException.ThrowIfNull(migrationBuilder);

    migrationBuilder.CreateTable(
        name: "vehicles",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          resident_id = table.Column<Guid>(type: "uuid", nullable: false),
          property_id = table.Column<Guid>(type: "uuid", nullable: true),
          contract_id = table.Column<Guid>(type: "uuid", nullable: true),
          plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
          normalized_plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
          type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          color = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
          brand = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
          model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
          year = table.Column<int>(type: "integer", nullable: true),
          authorization_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          parking_space_identifier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
          normalized_parking_space_identifier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
          parking_allocation_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
          notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
          table.PrimaryKey("pk_vehicles", x => x.id);
        });

    migrationBuilder.CreateIndex(
        name: "ix_vehicles_organization_id_authorization_status",
        schema: "app",
        table: "vehicles",
        columns: new[] { "organization_id", "authorization_status" });

    migrationBuilder.CreateIndex(
        name: "ix_vehicles_organization_id_contract_id",
        schema: "app",
        table: "vehicles",
        columns: new[] { "organization_id", "contract_id" });

    migrationBuilder.CreateIndex(
        name: "ix_vehicles_organization_id_deleted_at",
        schema: "app",
        table: "vehicles",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_vehicles_organization_id_normalized_plate",
        schema: "app",
        table: "vehicles",
        columns: new[] { "organization_id", "normalized_plate" });

    migrationBuilder.CreateIndex(
        name: "ix_vehicles_organization_id_property_id",
        schema: "app",
        table: "vehicles",
        columns: new[] { "organization_id", "property_id" });

    migrationBuilder.CreateIndex(
        name: "ix_vehicles_organization_id_property_id_normalized_parking_spa~",
        schema: "app",
        table: "vehicles",
        columns: new[] { "organization_id", "property_id", "normalized_parking_space_identifier" });

    migrationBuilder.CreateIndex(
        name: "ix_vehicles_organization_id_resident_id",
        schema: "app",
        table: "vehicles",
        columns: new[] { "organization_id", "resident_id" });

    migrationBuilder.CreateIndex(
        name: "ix_vehicles_organization_id_search_text",
        schema: "app",
        table: "vehicles",
        columns: new[] { "organization_id", "search_text" });

    migrationBuilder.CreateIndex(
        name: "ix_vehicles_organization_id_type",
        schema: "app",
        table: "vehicles",
        columns: new[] { "organization_id", "type" });
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    ArgumentNullException.ThrowIfNull(migrationBuilder);

    migrationBuilder.DropTable(
        name: "vehicles",
        schema: "app");
  }
}

#pragma warning restore CA1861
