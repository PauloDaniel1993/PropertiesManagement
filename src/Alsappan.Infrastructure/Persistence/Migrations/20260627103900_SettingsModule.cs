using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0161 // Keep EF-generated namespace shape stable.
#pragma warning disable CA1861 // EF Core migrations intentionally use generated array literals.

namespace Alsappan.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class SettingsModule : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      ArgumentNullException.ThrowIfNull(migrationBuilder);

      migrationBuilder.CreateTable(
          name: "domain_catalog_settings",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            catalog_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
            code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            label_pt_br = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            label_en_us = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            sort_order = table.Column<int>(type: "integer", nullable: false),
            is_enabled = table.Column<bool>(type: "boolean", nullable: false),
            is_system = table.Column<bool>(type: "boolean", nullable: false),
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
            table.PrimaryKey("pk_domain_catalog_settings", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "organization_settings",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            contact_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
            contact_phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
            contact_website = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
            time_zone = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            default_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
            fallback_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
            enabled_locales = table.Column<string[]>(type: "text[]", nullable: false),
            allow_organization_switching = table.Column<bool>(type: "boolean", nullable: false),
            require_active_organization = table.Column<bool>(type: "boolean", nullable: false),
            strict_tenant_isolation = table.Column<bool>(type: "boolean", nullable: false),
            resident_portal_enabled = table.Column<bool>(type: "boolean", nullable: false),
            resident_occurrence_creation_enabled = table.Column<bool>(type: "boolean", nullable: false),
            resident_document_upload_enabled = table.Column<bool>(type: "boolean", nullable: false),
            resident_profile_update_request_enabled = table.Column<bool>(type: "boolean", nullable: false),
            session_timeout_minutes = table.Column<int>(type: "integer", nullable: false),
            password_minimum_length = table.Column<int>(type: "integer", nullable: false),
            password_require_uppercase = table.Column<bool>(type: "boolean", nullable: false),
            password_require_lowercase = table.Column<bool>(type: "boolean", nullable: false),
            password_require_digit = table.Column<bool>(type: "boolean", nullable: false),
            password_require_symbol = table.Column<bool>(type: "boolean", nullable: false),
            mfa_policy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
            enabled_notification_categories = table.Column<string[]>(type: "text[]", nullable: false),
            enabled_notification_channels = table.Column<string[]>(type: "text[]", nullable: false),
            in_app_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
            email_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
            whats_app_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
            brand_display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
            logo_storage_key = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
            logo_file_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
            logo_content_type = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
            logo_size_bytes = table.Column<long>(type: "bigint", nullable: true),
            logo_width = table.Column<int>(type: "integer", nullable: true),
            logo_height = table.Column<int>(type: "integer", nullable: true),
            logo_alt = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
            logo_url = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
            primary_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
            primary_foreground_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
            accent_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
            accent_foreground_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
            support_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
            support_phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
            support_url = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
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
            table.PrimaryKey("pk_organization_settings", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "user_locale_preferences",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            user_id = table.Column<Guid>(type: "uuid", nullable: false),
            locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
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
            table.PrimaryKey("pk_user_locale_preferences", x => x.id);
          });

      migrationBuilder.CreateIndex(
          name: "ix_domain_catalog_settings_organization_id_catalog_type",
          schema: "app",
          table: "domain_catalog_settings",
          columns: new[] { "organization_id", "catalog_type" });

      migrationBuilder.CreateIndex(
          name: "ix_domain_catalog_settings_organization_id_catalog_type_code",
          schema: "app",
          table: "domain_catalog_settings",
          columns: new[] { "organization_id", "catalog_type", "code" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_domain_catalog_settings_organization_id_catalog_type_sort_o~",
          schema: "app",
          table: "domain_catalog_settings",
          columns: new[] { "organization_id", "catalog_type", "sort_order" });

      migrationBuilder.CreateIndex(
          name: "ix_domain_catalog_settings_organization_id_deleted_at",
          schema: "app",
          table: "domain_catalog_settings",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_organization_settings_organization_id",
          schema: "app",
          table: "organization_settings",
          column: "organization_id",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_organization_settings_organization_id_default_locale",
          schema: "app",
          table: "organization_settings",
          columns: new[] { "organization_id", "default_locale" });

      migrationBuilder.CreateIndex(
          name: "ix_organization_settings_organization_id_deleted_at",
          schema: "app",
          table: "organization_settings",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_user_locale_preferences_organization_id_deleted_at",
          schema: "app",
          table: "user_locale_preferences",
          columns: new[] { "organization_id", "deleted_at" });

      migrationBuilder.CreateIndex(
          name: "ix_user_locale_preferences_organization_id_locale",
          schema: "app",
          table: "user_locale_preferences",
          columns: new[] { "organization_id", "locale" });

      migrationBuilder.CreateIndex(
          name: "ix_user_locale_preferences_organization_id_user_id",
          schema: "app",
          table: "user_locale_preferences",
          columns: new[] { "organization_id", "user_id" },
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      ArgumentNullException.ThrowIfNull(migrationBuilder);

      migrationBuilder.DropTable(
          name: "domain_catalog_settings",
          schema: "app");

      migrationBuilder.DropTable(
          name: "organization_settings",
          schema: "app");

      migrationBuilder.DropTable(
          name: "user_locale_preferences",
          schema: "app");
    }
  }
}

#pragma warning restore CA1861
#pragma warning restore IDE0161
