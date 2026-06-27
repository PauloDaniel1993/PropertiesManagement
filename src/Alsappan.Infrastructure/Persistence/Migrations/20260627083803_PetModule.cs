using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161, CA1062, CA1861

namespace Alsappan.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class PetModule : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "pets",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            resident_id = table.Column<Guid>(type: "uuid", nullable: false),
            property_id = table.Column<Guid>(type: "uuid", nullable: true),
            contract_id = table.Column<Guid>(type: "uuid", nullable: true),
            name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            species = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            breed = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
            authorization_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            authorization_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
            table.PrimaryKey("pk_pets", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "pet_document_links",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            pet_id = table.Column<Guid>(type: "uuid", nullable: false),
            document_id = table.Column<Guid>(type: "uuid", nullable: false),
            kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
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
            table.PrimaryKey("pk_pet_document_links", x => x.id);
            table.ForeignKey(
                      name: "fk_pet_document_links_pets_pet_id",
                      column: x => x.pet_id,
                      principalSchema: "app",
                      principalTable: "pets",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "ix_pet_document_links_organization_id_deleted_at",
          schema: "app",
          table: "pet_document_links",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_pet_document_links_organization_id_document_id",
          schema: "app",
          table: "pet_document_links",
          columns: new[] { "organization_id", "document_id" });

      migrationBuilder.CreateIndex(
          name: "ix_pet_document_links_organization_id_pet_id",
          schema: "app",
          table: "pet_document_links",
          columns: new[] { "organization_id", "pet_id" });

      migrationBuilder.CreateIndex(
          name: "ix_pet_document_links_organization_id_pet_id_document_id_kind",
          schema: "app",
          table: "pet_document_links",
          columns: new[] { "organization_id", "pet_id", "document_id", "kind" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_pet_document_links_pet_id",
          schema: "app",
          table: "pet_document_links",
          column: "pet_id");

      migrationBuilder.CreateIndex(
          name: "ix_pets_organization_id_authorization_status",
          schema: "app",
          table: "pets",
          columns: new[] { "organization_id", "authorization_status" });

      migrationBuilder.CreateIndex(
          name: "ix_pets_organization_id_contract_id",
          schema: "app",
          table: "pets",
          columns: new[] { "organization_id", "contract_id" });

      migrationBuilder.CreateIndex(
          name: "ix_pets_organization_id_deleted_at",
          schema: "app",
          table: "pets",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_pets_organization_id_property_id",
          schema: "app",
          table: "pets",
          columns: new[] { "organization_id", "property_id" });

      migrationBuilder.CreateIndex(
          name: "ix_pets_organization_id_resident_id",
          schema: "app",
          table: "pets",
          columns: new[] { "organization_id", "resident_id" });

      migrationBuilder.CreateIndex(
          name: "ix_pets_organization_id_search_text",
          schema: "app",
          table: "pets",
          columns: new[] { "organization_id", "search_text" });

      migrationBuilder.CreateIndex(
          name: "ix_pets_organization_id_species",
          schema: "app",
          table: "pets",
          columns: new[] { "organization_id", "species" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "pet_document_links",
          schema: "app");

      migrationBuilder.DropTable(
          name: "pets",
          schema: "app");
    }
  }
}
