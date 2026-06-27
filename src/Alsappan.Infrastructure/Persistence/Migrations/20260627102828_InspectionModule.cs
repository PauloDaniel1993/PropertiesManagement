using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace Alsappan.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InspectionModule : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    ArgumentNullException.ThrowIfNull(migrationBuilder);

    migrationBuilder.CreateTable(
        name: "inspections",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          property_id = table.Column<Guid>(type: "uuid", nullable: false),
          contract_id = table.Column<Guid>(type: "uuid", nullable: true),
          resident_id = table.Column<Guid>(type: "uuid", nullable: true),
          scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
          assigned_user_id = table.Column<Guid>(type: "uuid", nullable: false),
          assigned_user_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
          status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
          notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
          started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
          started_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
          completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
          completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
          completion_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
          cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
          cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
          cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
          table.PrimaryKey("pk_inspections", x => x.id);
        });

    migrationBuilder.CreateTable(
        name: "inspection_checklist_items",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
          area_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
          item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
          is_required = table.Column<bool>(type: "boolean", nullable: false),
          condition_rating = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
          observations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
          sort_order = table.Column<int>(type: "integer", nullable: false),
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
          table.PrimaryKey("pk_inspection_checklist_items", x => x.id);
          table.ForeignKey(
                      name: "fk_inspection_checklist_items_inspections_inspection_id",
                      column: x => x.inspection_id,
                      principalSchema: "app",
                      principalTable: "inspections",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateTable(
        name: "inspection_document_links",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
          checklist_item_id = table.Column<Guid>(type: "uuid", nullable: true),
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
          table.PrimaryKey("pk_inspection_document_links", x => x.id);
          table.ForeignKey(
                      name: "fk_inspection_document_links_inspections_inspection_id",
                      column: x => x.inspection_id,
                      principalSchema: "app",
                      principalTable: "inspections",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateTable(
        name: "inspection_signature_slots",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
          signer_role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
          signer_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
          is_required = table.Column<bool>(type: "boolean", nullable: false),
          signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
          signature_document_id = table.Column<Guid>(type: "uuid", nullable: true),
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
          table.PrimaryKey("pk_inspection_signature_slots", x => x.id);
          table.ForeignKey(
                      name: "fk_inspection_signature_slots_inspections_inspection_id",
                      column: x => x.inspection_id,
                      principalSchema: "app",
                      principalTable: "inspections",
                      principalColumn: "id",
                      onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_checklist_items_inspection_id",
        schema: "app",
        table: "inspection_checklist_items",
        column: "inspection_id");

    migrationBuilder.CreateIndex(
        name: "ix_inspection_checklist_items_organization_id_condition_rating",
        schema: "app",
        table: "inspection_checklist_items",
        columns: new[] { "organization_id", "condition_rating" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_checklist_items_organization_id_deleted_at",
        schema: "app",
        table: "inspection_checklist_items",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_checklist_items_organization_id_inspection_id",
        schema: "app",
        table: "inspection_checklist_items",
        columns: new[] { "organization_id", "inspection_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_checklist_items_organization_id_inspection_id_ar~",
        schema: "app",
        table: "inspection_checklist_items",
        columns: new[] { "organization_id", "inspection_id", "area_name" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_document_links_inspection_id",
        schema: "app",
        table: "inspection_document_links",
        column: "inspection_id");

    migrationBuilder.CreateIndex(
        name: "ix_inspection_document_links_organization_id_checklist_item_id",
        schema: "app",
        table: "inspection_document_links",
        columns: new[] { "organization_id", "checklist_item_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_document_links_organization_id_deleted_at",
        schema: "app",
        table: "inspection_document_links",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_document_links_organization_id_document_id",
        schema: "app",
        table: "inspection_document_links",
        columns: new[] { "organization_id", "document_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_document_links_organization_id_inspection_id",
        schema: "app",
        table: "inspection_document_links",
        columns: new[] { "organization_id", "inspection_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_document_links_organization_id_inspection_id_doc~",
        schema: "app",
        table: "inspection_document_links",
        columns: new[] { "organization_id", "inspection_id", "document_id", "kind" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_signature_slots_inspection_id",
        schema: "app",
        table: "inspection_signature_slots",
        column: "inspection_id");

    migrationBuilder.CreateIndex(
        name: "ix_inspection_signature_slots_organization_id_deleted_at",
        schema: "app",
        table: "inspection_signature_slots",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_signature_slots_organization_id_inspection_id",
        schema: "app",
        table: "inspection_signature_slots",
        columns: new[] { "organization_id", "inspection_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspection_signature_slots_organization_id_signature_docume~",
        schema: "app",
        table: "inspection_signature_slots",
        columns: new[] { "organization_id", "signature_document_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspections_organization_id_assigned_user_id",
        schema: "app",
        table: "inspections",
        columns: new[] { "organization_id", "assigned_user_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspections_organization_id_contract_id",
        schema: "app",
        table: "inspections",
        columns: new[] { "organization_id", "contract_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspections_organization_id_deleted_at",
        schema: "app",
        table: "inspections",
        columns: new[] { "organization_id", "deleted_at" });

    migrationBuilder.CreateIndex(
        name: "ix_inspections_organization_id_property_id",
        schema: "app",
        table: "inspections",
        columns: new[] { "organization_id", "property_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspections_organization_id_resident_id",
        schema: "app",
        table: "inspections",
        columns: new[] { "organization_id", "resident_id" });

    migrationBuilder.CreateIndex(
        name: "ix_inspections_organization_id_scheduled_at",
        schema: "app",
        table: "inspections",
        columns: new[] { "organization_id", "scheduled_at" });

    migrationBuilder.CreateIndex(
        name: "ix_inspections_organization_id_search_text",
        schema: "app",
        table: "inspections",
        columns: new[] { "organization_id", "search_text" });

    migrationBuilder.CreateIndex(
        name: "ix_inspections_organization_id_status",
        schema: "app",
        table: "inspections",
        columns: new[] { "organization_id", "status" });

    migrationBuilder.CreateIndex(
        name: "ix_inspections_organization_id_status_scheduled_at",
        schema: "app",
        table: "inspections",
        columns: new[] { "organization_id", "status", "scheduled_at" });
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    ArgumentNullException.ThrowIfNull(migrationBuilder);

    migrationBuilder.DropTable(
        name: "inspection_checklist_items",
        schema: "app");

    migrationBuilder.DropTable(
        name: "inspection_document_links",
        schema: "app");

    migrationBuilder.DropTable(
        name: "inspection_signature_slots",
        schema: "app");

    migrationBuilder.DropTable(
        name: "inspections",
        schema: "app");
  }
}

#pragma warning restore CA1861
