using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1062, CA1861

namespace Alsappan.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class NotificationPreferences : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.CreateTable(
        name: "notification_preference_records",
        schema: "app",
        columns: table => new
        {
          id = table.Column<Guid>(type: "uuid", nullable: false),
          organization_id = table.Column<Guid>(type: "uuid", nullable: false),
          user_id = table.Column<Guid>(type: "uuid", nullable: false),
          category = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
          channel = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
          is_enabled = table.Column<bool>(type: "boolean", nullable: false),
          is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
          created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
          updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        },
        constraints: table =>
        {
          table.PrimaryKey("pk_notification_preference_records", x => x.id);
        });

    migrationBuilder.CreateIndex(
        name: "ix_notification_preference_records_organization_id_user_id",
        schema: "app",
        table: "notification_preference_records",
        columns: new[] { "organization_id", "user_id" });

    migrationBuilder.CreateIndex(
        name: "ix_notification_preference_records_organization_id_user_id_cat~",
        schema: "app",
        table: "notification_preference_records",
        columns: new[] { "organization_id", "user_id", "category", "channel" },
        unique: true);
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.DropTable(
        name: "notification_preference_records",
        schema: "app");
  }
}
