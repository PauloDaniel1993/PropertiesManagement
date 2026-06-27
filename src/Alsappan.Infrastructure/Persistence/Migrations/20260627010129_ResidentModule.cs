using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1062, CA1861, IDE0161

namespace Alsappan.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class ResidentModule : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "residents",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            full_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
            preferred_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
            email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
            normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
            phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            normalized_phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            secondary_phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            normalized_secondary_phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            document_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            document_identifier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
            normalized_document_identifier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
            birth_date = table.Column<DateOnly>(type: "date", nullable: true),
            emergency_contact_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
            emergency_contact_relationship = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
            emergency_contact_phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            normalized_emergency_contact_phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            portal_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            privacy_flags = table.Column<int>(type: "integer", nullable: false),
            notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            linked_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            search_text = table.Column<string>(type: "character varying(1800)", maxLength: 1800, nullable: false),
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
            table.PrimaryKey("pk_residents", x => x.id);
          });

      migrationBuilder.CreateIndex(
          name: "ix_residents_organization_id_deleted_at",
          schema: "app",
          table: "residents",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_residents_organization_id_full_name",
          schema: "app",
          table: "residents",
          columns: new[] { "organization_id", "full_name" });

      migrationBuilder.CreateIndex(
          name: "ix_residents_organization_id_linked_user_id",
          schema: "app",
          table: "residents",
          columns: new[] { "organization_id", "linked_user_id" });

      migrationBuilder.CreateIndex(
          name: "ix_residents_organization_id_normalized_document_identifier",
          schema: "app",
          table: "residents",
          columns: new[] { "organization_id", "normalized_document_identifier" });

      migrationBuilder.CreateIndex(
          name: "ix_residents_organization_id_normalized_email",
          schema: "app",
          table: "residents",
          columns: new[] { "organization_id", "normalized_email" });

      migrationBuilder.CreateIndex(
          name: "ix_residents_organization_id_normalized_phone",
          schema: "app",
          table: "residents",
          columns: new[] { "organization_id", "normalized_phone" });

      migrationBuilder.CreateIndex(
          name: "ix_residents_organization_id_portal_status",
          schema: "app",
          table: "residents",
          columns: new[] { "organization_id", "portal_status" });

      migrationBuilder.CreateIndex(
          name: "ix_residents_organization_id_search_text",
          schema: "app",
          table: "residents",
          columns: new[] { "organization_id", "search_text" });

      migrationBuilder.CreateIndex(
          name: "ix_residents_organization_id_status",
          schema: "app",
          table: "residents",
          columns: new[] { "organization_id", "status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "residents",
          schema: "app");
    }
  }
}
