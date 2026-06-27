using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1062, CA1861, IDE0161

namespace Alsappan.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class IdentityAccess : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "identity_memberships",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            user_id = table.Column<Guid>(type: "uuid", nullable: false),
            role_codes = table.Column<string[]>(type: "text[]", nullable: false),
            permission_codes = table.Column<string[]>(type: "text[]", nullable: false),
            status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
            table.PrimaryKey("pk_identity_memberships", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "identity_organizations",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
            display_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
            locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
            currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            concurrency_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_organizations", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "identity_permissions",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
            module = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            action = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
            description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
            concurrency_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_permissions", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "identity_role_permissions",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            role_id = table.Column<Guid>(type: "uuid", nullable: false),
            permission_id = table.Column<Guid>(type: "uuid", nullable: true),
            permission_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
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
            table.PrimaryKey("pk_identity_role_permissions", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "identity_roles",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
            display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
            is_system = table.Column<bool>(type: "boolean", nullable: false),
            is_assignable = table.Column<bool>(type: "boolean", nullable: false),
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
            table.PrimaryKey("pk_identity_roles", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "identity_users",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
            normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
            display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
            account_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
            password_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            failed_login_count = table.Column<int>(type: "integer", nullable: false),
            locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            concurrency_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("pk_identity_users", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "resident_account_links",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            user_id = table.Column<Guid>(type: "uuid", nullable: false),
            resident_id = table.Column<Guid>(type: "uuid", nullable: false),
            is_active = table.Column<bool>(type: "boolean", nullable: false),
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
            table.PrimaryKey("pk_resident_account_links", x => x.id);
          });

      migrationBuilder.CreateTable(
          name: "user_invitations",
          schema: "app",
          columns: table => new
          {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
            normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
            display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            role_id = table.Column<Guid>(type: "uuid", nullable: false),
            token_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            accepted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
            table.PrimaryKey("pk_user_invitations", x => x.id);
          });

      migrationBuilder.CreateIndex(
          name: "ix_identity_memberships_organization_id_user_id",
          schema: "app",
          table: "identity_memberships",
          columns: new[] { "organization_id", "user_id" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_identity_memberships_user_id_status",
          schema: "app",
          table: "identity_memberships",
          columns: new[] { "user_id", "status" });

      migrationBuilder.CreateIndex(
          name: "ix_identity_organizations_slug",
          schema: "app",
          table: "identity_organizations",
          column: "slug",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_identity_permissions_code",
          schema: "app",
          table: "identity_permissions",
          column: "code",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_identity_permissions_module_action",
          schema: "app",
          table: "identity_permissions",
          columns: new[] { "module", "action" });

      migrationBuilder.CreateIndex(
          name: "ix_identity_role_permissions_organization_id_role_id_permissio~",
          schema: "app",
          table: "identity_role_permissions",
          columns: new[] { "organization_id", "role_id", "permission_code" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_identity_roles_organization_id_code",
          schema: "app",
          table: "identity_roles",
          columns: new[] { "organization_id", "code" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_identity_users_account_type_status",
          schema: "app",
          table: "identity_users",
          columns: new[] { "account_type", "status" });

      migrationBuilder.CreateIndex(
          name: "ix_identity_users_normalized_email_account_type",
          schema: "app",
          table: "identity_users",
          columns: new[] { "normalized_email", "account_type" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_resident_account_links_organization_id_resident_id",
          schema: "app",
          table: "resident_account_links",
          columns: new[] { "organization_id", "resident_id" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "ix_resident_account_links_organization_id_user_id",
          schema: "app",
          table: "resident_account_links",
          columns: new[] { "organization_id", "user_id" });

      migrationBuilder.CreateIndex(
          name: "ix_user_invitations_organization_id_normalized_email_status",
          schema: "app",
          table: "user_invitations",
          columns: new[] { "organization_id", "normalized_email", "status" });

      migrationBuilder.CreateIndex(
          name: "ix_user_invitations_token_hash",
          schema: "app",
          table: "user_invitations",
          column: "token_hash",
          unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "identity_memberships",
          schema: "app");

      migrationBuilder.DropTable(
          name: "identity_organizations",
          schema: "app");

      migrationBuilder.DropTable(
          name: "identity_permissions",
          schema: "app");

      migrationBuilder.DropTable(
          name: "identity_role_permissions",
          schema: "app");

      migrationBuilder.DropTable(
          name: "identity_roles",
          schema: "app");

      migrationBuilder.DropTable(
          name: "identity_users",
          schema: "app");

      migrationBuilder.DropTable(
          name: "resident_account_links",
          schema: "app");

      migrationBuilder.DropTable(
          name: "user_invitations",
          schema: "app");
    }
  }
}
