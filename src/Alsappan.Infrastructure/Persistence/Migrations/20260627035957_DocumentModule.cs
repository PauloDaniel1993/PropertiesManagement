using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161, CA1062, CA1861

namespace Alsappan.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class DocumentModule : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "documents",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
            status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
            description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            current_file_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
            current_content_type = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
            current_size_bytes = table.Column<long>(type: "bigint", nullable: false),
            current_storage_key = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
            current_version_number = table.Column<int>(type: "integer", nullable: false),
            current_uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            current_uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
            table.PrimaryKey("pk_documents", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "document_links",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            document_id = table.Column<Guid>(type: "uuid", nullable: false),
            entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
            entity_id = table.Column<Guid>(type: "uuid", nullable: false),
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
            table.PrimaryKey("pk_document_links", x => x.id);
            table.ForeignKey(
                      name: "fk_document_links_documents_document_id",
                      column: x => x.document_id,
                      principalSchema: "app",
                      principalTable: "documents",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "document_versions",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            document_id = table.Column<Guid>(type: "uuid", nullable: false),
            version_number = table.Column<int>(type: "integer", nullable: false),
            file_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
            content_type = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
            size_bytes = table.Column<long>(type: "bigint", nullable: false),
            storage_key = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
            notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
            table.PrimaryKey("pk_document_versions", x => x.id);
            table.ForeignKey(
                      name: "fk_document_versions_documents_document_id",
                      column: x => x.document_id,
                      principalSchema: "app",
                      principalTable: "documents",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "ix_document_links_document_id",
          schema: "app",
          table: "document_links",
          column: "document_id");

      migrationBuilder.CreateIndex(
          name: "ix_document_links_organization_id_deleted_at",
          schema: "app",
          table: "document_links",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_document_links_organization_id_document_id",
          schema: "app",
          table: "document_links",
          columns: new[] { "organization_id", "document_id" });

      migrationBuilder.CreateIndex(
          name: "ix_document_links_organization_id_document_id_entity_type_enti~",
          schema: "app",
          table: "document_links",
          columns: new[] { "organization_id", "document_id", "entity_type", "entity_id" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_document_links_organization_id_entity_type_entity_id",
          schema: "app",
          table: "document_links",
          columns: new[] { "organization_id", "entity_type", "entity_id" });

      migrationBuilder.CreateIndex(
          name: "ix_document_versions_document_id",
          schema: "app",
          table: "document_versions",
          column: "document_id");

      migrationBuilder.CreateIndex(
          name: "ix_document_versions_organization_id_deleted_at",
          schema: "app",
          table: "document_versions",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_document_versions_organization_id_document_id_version_number",
          schema: "app",
          table: "document_versions",
          columns: new[] { "organization_id", "document_id", "version_number" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_document_versions_organization_id_storage_key",
          schema: "app",
          table: "document_versions",
          columns: new[] { "organization_id", "storage_key" });

      migrationBuilder.CreateIndex(
          name: "ix_documents_organization_id_category",
          schema: "app",
          table: "documents",
          columns: new[] { "organization_id", "category" });

      migrationBuilder.CreateIndex(
          name: "ix_documents_organization_id_current_uploaded_at",
          schema: "app",
          table: "documents",
          columns: new[] { "organization_id", "current_uploaded_at" });

      migrationBuilder.CreateIndex(
          name: "ix_documents_organization_id_search_text",
          schema: "app",
          table: "documents",
          columns: new[] { "organization_id", "search_text" });

      migrationBuilder.CreateIndex(
          name: "ix_documents_organization_id_status",
          schema: "app",
          table: "documents",
          columns: new[] { "organization_id", "status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "document_links",
          schema: "app");

      migrationBuilder.DropTable(
          name: "document_versions",
          schema: "app");

      migrationBuilder.DropTable(
          name: "documents",
          schema: "app");
    }
  }
}
