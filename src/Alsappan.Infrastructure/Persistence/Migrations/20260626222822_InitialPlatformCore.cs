using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1062, CA1861, IDE0161

namespace Alsappan.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class InitialPlatformCore : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.EnsureSchema(
          name: "app");

      migrationBuilder.CreateTable(
          name: "audit_log_entries",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            organization_id = table.Column<Guid>(type: "uuid", nullable: false),
            action = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            actor_kind = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
            actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            actor_display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
            target_entity_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            target_entity_id = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            target_display_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
            changed_fields_json = table.Column<string>(type: "jsonb", nullable: false),
            context_json = table.Column<string>(type: "jsonb", nullable: false),
            correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_audit_log_entries", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "notification_records",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            event_id = table.Column<Guid>(type: "uuid", nullable: false),
            organization_id = table.Column<Guid>(type: "uuid", nullable: false),
            recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            category = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            event_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            channel = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
            delivery_status = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
            payload_json = table.Column<string>(type: "jsonb", nullable: false),
            subject_entity_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            subject_entity_id = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            subject_display_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
            occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            is_read = table.Column<bool>(type: "boolean", nullable: false),
            read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_notification_records", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "outbox_messages",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            event_id = table.Column<Guid>(type: "uuid", nullable: false),
            organization_id = table.Column<Guid>(type: "uuid", nullable: false),
            module_name = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            event_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            consumer_mask = table.Column<int>(type: "integer", nullable: false),
            payload_json = table.Column<string>(type: "jsonb", nullable: false),
            correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
            causation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
            enqueued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            attempt_count = table.Column<int>(type: "integer", nullable: false),
            last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_outbox_messages", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "refresh_sessions",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            user_id = table.Column<Guid>(type: "uuid", nullable: false),
            token_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            active_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
            user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            ip_address = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
            revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            revoked_reason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
            replaced_by_session_id = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_refresh_sessions", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "seed_history_records",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            executed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_seed_history_records", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "timeline_entries",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            event_id = table.Column<Guid>(type: "uuid", nullable: false),
            organization_id = table.Column<Guid>(type: "uuid", nullable: false),
            module_name = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            event_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            actor_kind = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
            actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            actor_display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
            subject_entity_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            subject_entity_id = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            subject_display_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
            related_entities_json = table.Column<string>(type: "jsonb", nullable: false),
            data_json = table.Column<string>(type: "jsonb", nullable: false),
            correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_timeline_entries", x => x.id);
          });

      migrationBuilder.CreateIndex(
          name: "ix_audit_log_entries_organization_id_actor_user_id_occurred_at",
          schema: "app",
          table: "audit_log_entries",
          columns: new[] { "organization_id", "actor_user_id", "occurred_at" });

      migrationBuilder.CreateIndex(
          name: "ix_audit_log_entries_organization_id_category_occurred_at",
          schema: "app",
          table: "audit_log_entries",
          columns: new[] { "organization_id", "category", "occurred_at" });

      migrationBuilder.CreateIndex(
          name: "ix_audit_log_entries_organization_id_occurred_at",
          schema: "app",
          table: "audit_log_entries",
          columns: new[] { "organization_id", "occurred_at" });

      migrationBuilder.CreateIndex(
          name: "ix_audit_log_entries_organization_id_target_entity_type_target~",
          schema: "app",
          table: "audit_log_entries",
          columns: new[] { "organization_id", "target_entity_type", "target_entity_id" });

      migrationBuilder.CreateIndex(
          name: "ix_notification_records_organization_id_category_occurred_at",
          schema: "app",
          table: "notification_records",
          columns: new[] { "organization_id", "category", "occurred_at" });

      migrationBuilder.CreateIndex(
          name: "ix_notification_records_organization_id_recipient_user_id_is_r~",
          schema: "app",
          table: "notification_records",
          columns: new[] { "organization_id", "recipient_user_id", "is_read" });

      migrationBuilder.CreateIndex(
          name: "ix_notification_records_organization_id_subject_entity_type_su~",
          schema: "app",
          table: "notification_records",
          columns: new[] { "organization_id", "subject_entity_type", "subject_entity_id" });

      migrationBuilder.CreateIndex(
          name: "ix_outbox_messages_event_id",
          schema: "app",
          table: "outbox_messages",
          column: "event_id",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_outbox_messages_organization_id_module_name_event_name",
          schema: "app",
          table: "outbox_messages",
          columns: new[] { "organization_id", "module_name", "event_name" });

      migrationBuilder.CreateIndex(
          name: "ix_outbox_messages_organization_id_processed_at_enqueued_at",
          schema: "app",
          table: "outbox_messages",
          columns: new[] { "organization_id", "processed_at", "enqueued_at" });

      migrationBuilder.CreateIndex(
          name: "ix_refresh_sessions_active_organization_id_user_id",
          schema: "app",
          table: "refresh_sessions",
          columns: new[] { "active_organization_id", "user_id" });

      migrationBuilder.CreateIndex(
          name: "ix_refresh_sessions_token_hash",
          schema: "app",
          table: "refresh_sessions",
          column: "token_hash",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_refresh_sessions_user_id_expires_at",
          schema: "app",
          table: "refresh_sessions",
          columns: new[] { "user_id", "expires_at" });

      migrationBuilder.CreateIndex(
          name: "ix_seed_history_records_name_version",
          schema: "app",
          table: "seed_history_records",
          columns: new[] { "name", "version" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_timeline_entries_organization_id_actor_user_id_occurred_at",
          schema: "app",
          table: "timeline_entries",
          columns: new[] { "organization_id", "actor_user_id", "occurred_at" });

      migrationBuilder.CreateIndex(
          name: "ix_timeline_entries_organization_id_event_name_occurred_at",
          schema: "app",
          table: "timeline_entries",
          columns: new[] { "organization_id", "event_name", "occurred_at" });

      migrationBuilder.CreateIndex(
          name: "ix_timeline_entries_organization_id_occurred_at",
          schema: "app",
          table: "timeline_entries",
          columns: new[] { "organization_id", "occurred_at" });

      migrationBuilder.CreateIndex(
          name: "ix_timeline_entries_organization_id_subject_entity_type_subjec~",
          schema: "app",
          table: "timeline_entries",
          columns: new[] { "organization_id", "subject_entity_type", "subject_entity_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "audit_log_entries",
          schema: "app");

      migrationBuilder.DropTable(
          name: "notification_records",
          schema: "app");

      migrationBuilder.DropTable(
          name: "outbox_messages",
          schema: "app");

      migrationBuilder.DropTable(
          name: "refresh_sessions",
          schema: "app");

      migrationBuilder.DropTable(
          name: "seed_history_records",
          schema: "app");

      migrationBuilder.DropTable(
          name: "timeline_entries",
          schema: "app");
    }
  }
}
