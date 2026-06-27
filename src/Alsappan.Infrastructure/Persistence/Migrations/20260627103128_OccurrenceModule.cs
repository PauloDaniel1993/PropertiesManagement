using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF Core migrations intentionally use generated array literals.

namespace Alsappan.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class OccurrenceModule : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    ArgumentNullException.ThrowIfNull(migrationBuilder);

    migrationBuilder.CreateTable(
        name: "occurrences",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
          description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
          type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          property_id = table.Column<Guid>(type: "uuid", nullable: true),
          resident_id = table.Column<Guid>(type: "uuid", nullable: true),
          contract_id = table.Column<Guid>(type: "uuid", nullable: true),
          assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
          due_date = table.Column<DateOnly>(type: "date", nullable: true),
          resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
          resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
          resolution_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
          cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
          cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
          cancellation_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
          table.PrimaryKey("pk_occurrences", x => x.id);
        });

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
        name: "occurrence_assignment_history",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          occurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
          previous_assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
          new_assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
          notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
          table.PrimaryKey("pk_occurrence_assignment_history", x => x.id);
          table.ForeignKey(
                    name: "fk_occurrence_assignment_history_occurrences_occurrence_id",
                    column: x => x.occurrence_id,
                    principalSchema: "app",
                    principalTable: "occurrences",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateTable(
        name: "occurrence_attachments",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          occurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
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
          table.PrimaryKey("pk_occurrence_attachments", x => x.id);
          table.ForeignKey(
                    name: "fk_occurrence_attachments_occurrences_occurrence_id",
                    column: x => x.occurrence_id,
                    principalSchema: "app",
                    principalTable: "occurrences",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateTable(
        name: "occurrence_comments",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          occurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
          body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
          is_internal = table.Column<bool>(type: "boolean", nullable: false),
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
          table.PrimaryKey("pk_occurrence_comments", x => x.id);
          table.ForeignKey(
                    name: "fk_occurrence_comments_occurrences_occurrence_id",
                    column: x => x.occurrence_id,
                    principalSchema: "app",
                    principalTable: "occurrences",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateTable(
        name: "occurrence_priority_history",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          occurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
          previous_priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
          new_priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
          table.PrimaryKey("pk_occurrence_priority_history", x => x.id);
          table.ForeignKey(
                    name: "fk_occurrence_priority_history_occurrences_occurrence_id",
                    column: x => x.occurrence_id,
                    principalSchema: "app",
                    principalTable: "occurrences",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateTable(
        name: "occurrence_status_history",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          occurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
          previous_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
          new_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
          table.PrimaryKey("pk_occurrence_status_history", x => x.id);
          table.ForeignKey(
                    name: "fk_occurrence_status_history_occurrences_occurrence_id",
                    column: x => x.occurrence_id,
                    principalSchema: "app",
                    principalTable: "occurrences",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
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
        name: "ix_occurrence_assignment_history_occurrence_id",
        schema: "app",
        table: "occurrence_assignment_history",
        column: "occurrence_id");

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_assignment_history_organization_id_new_assigned_~",
        schema: "app",
        table: "occurrence_assignment_history",
        columns: new[] { "organization_id", "new_assigned_user_id" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_assignment_history_organization_id_occurrence_id~",
        schema: "app",
        table: "occurrence_assignment_history",
        columns: new[] { "organization_id", "occurrence_id", "created_at" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_attachments_occurrence_id",
        schema: "app",
        table: "occurrence_attachments",
        column: "occurrence_id");

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_attachments_organization_id_deleted_at",
        schema: "app",
        table: "occurrence_attachments",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_attachments_organization_id_document_id",
        schema: "app",
        table: "occurrence_attachments",
        columns: new[] { "organization_id", "document_id" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_attachments_organization_id_occurrence_id",
        schema: "app",
        table: "occurrence_attachments",
        columns: new[] { "organization_id", "occurrence_id" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_attachments_organization_id_occurrence_id_docume~",
        schema: "app",
        table: "occurrence_attachments",
        columns: new[] { "organization_id", "occurrence_id", "document_id" },
        unique: true);

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_comments_occurrence_id",
        schema: "app",
        table: "occurrence_comments",
        column: "occurrence_id");

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_comments_organization_id_created_by_user_id",
        schema: "app",
        table: "occurrence_comments",
        columns: new[] { "organization_id", "created_by_user_id" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_comments_organization_id_deleted_at",
        schema: "app",
        table: "occurrence_comments",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_comments_organization_id_occurrence_id_created_at",
        schema: "app",
        table: "occurrence_comments",
        columns: new[] { "organization_id", "occurrence_id", "created_at" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_priority_history_occurrence_id",
        schema: "app",
        table: "occurrence_priority_history",
        column: "occurrence_id");

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_priority_history_organization_id_new_priority",
        schema: "app",
        table: "occurrence_priority_history",
        columns: new[] { "organization_id", "new_priority" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_priority_history_organization_id_occurrence_id_c~",
        schema: "app",
        table: "occurrence_priority_history",
        columns: new[] { "organization_id", "occurrence_id", "created_at" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_status_history_occurrence_id",
        schema: "app",
        table: "occurrence_status_history",
        column: "occurrence_id");

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_status_history_organization_id_new_status",
        schema: "app",
        table: "occurrence_status_history",
        columns: new[] { "organization_id", "new_status" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrence_status_history_organization_id_occurrence_id_cre~",
        schema: "app",
        table: "occurrence_status_history",
        columns: new[] { "organization_id", "occurrence_id", "created_at" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_assigned_user_id",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "assigned_user_id" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_contract_id",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "contract_id" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_created_at",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "created_at" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_deleted_at",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_due_date",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "due_date" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_priority",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "priority" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_property_id",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "property_id" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_resident_id",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "resident_id" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_search_text",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "search_text" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_status",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "status" });

    migrationBuilder.CreateIndex(
        name: "ix_occurrences_organization_id_type",
        schema: "app",
        table: "occurrences",
        columns: new[] { "organization_id", "type" });

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
    ArgumentNullException.ThrowIfNull(migrationBuilder);

    migrationBuilder.DropTable(
        name: "occurrence_assignment_history",
        schema: "app");

    migrationBuilder.DropTable(
        name: "occurrence_attachments",
        schema: "app");

    migrationBuilder.DropTable(
        name: "occurrence_comments",
        schema: "app");

    migrationBuilder.DropTable(
        name: "occurrence_priority_history",
        schema: "app");

    migrationBuilder.DropTable(
        name: "occurrence_status_history",
        schema: "app");

    migrationBuilder.DropTable(
        name: "pet_document_links",
        schema: "app");

    migrationBuilder.DropTable(
        name: "occurrences",
        schema: "app");

    migrationBuilder.DropTable(
        name: "pets",
        schema: "app");
  }
}

#pragma warning restore CA1861
