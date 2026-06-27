using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Identity;

public sealed class IdentityUserConfiguration : IEntityTypeConfiguration<IdentityUser>
{
  public void Configure(EntityTypeBuilder<IdentityUser> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("IdentityUsers");
    builder.HasKey(user => user.Id);

    builder.Property(user => user.Id).HasUserIdConversion();
    builder.Property(user => user.CreatedByUserId).HasNullableUserIdConversion();
    builder.Property(user => user.UpdatedByUserId).HasNullableUserIdConversion();
    builder.Property(user => user.DeletedByUserId).HasNullableUserIdConversion();
    IdentityConfiguration.ConfigureConcurrencyToken(builder);
    builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
    builder.Property(user => user.NormalizedEmail).HasMaxLength(320).IsRequired();
    builder.Property(user => user.DisplayName).HasMaxLength(160);
    builder.Property(user => user.AccountType).HasConversion<string>().HasMaxLength(32).IsRequired();
    builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
    builder.Property(user => user.PasswordHash).HasMaxLength(512);
    builder.Ignore(user => user.HasPassword);
    builder.Ignore(user => user.IsDeleted);

    builder.HasIndex(user => new { user.NormalizedEmail, user.AccountType }).IsUnique();
    builder.HasIndex(user => new { user.AccountType, user.Status });
  }
}

public sealed class IdentityOrganizationConfiguration : IEntityTypeConfiguration<IdentityOrganization>
{
  public void Configure(EntityTypeBuilder<IdentityOrganization> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("IdentityOrganizations");
    builder.HasKey(organization => organization.Id);

    builder.Property(organization => organization.Id).HasOrganizationIdConversion();
    builder.Property(organization => organization.CreatedByUserId).HasNullableUserIdConversion();
    builder.Property(organization => organization.UpdatedByUserId).HasNullableUserIdConversion();
    builder.Property(organization => organization.DeletedByUserId).HasNullableUserIdConversion();
    IdentityConfiguration.ConfigureConcurrencyToken(builder);
    builder.Property(organization => organization.Slug).HasMaxLength(96).IsRequired();
    builder.Property(organization => organization.Name).HasMaxLength(180).IsRequired();
    builder.Property(organization => organization.DisplayName).HasMaxLength(180).IsRequired();
    builder.Property(organization => organization.Locale).HasMaxLength(16).IsRequired();
    builder.Property(organization => organization.Currency).HasMaxLength(3).IsRequired();
    builder.Ignore(organization => organization.IsDeleted);

    builder.HasIndex(organization => organization.Slug).IsUnique();
  }
}

public sealed class IdentityMembershipConfiguration : IEntityTypeConfiguration<IdentityMembership>
{
  public void Configure(EntityTypeBuilder<IdentityMembership> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("IdentityMemberships");
    builder.HasKey(membership => membership.Id);

    builder.Property(membership => membership.Id).HasEntityIdConversion();
    builder.Property(membership => membership.OrganizationId).HasOrganizationIdConversion();
    builder.Property(membership => membership.UserId).HasUserIdConversion();
    builder.Property(membership => membership.CreatedByUserId).HasNullableUserIdConversion();
    builder.Property(membership => membership.UpdatedByUserId).HasNullableUserIdConversion();
    builder.Property(membership => membership.DeletedByUserId).HasNullableUserIdConversion();
    IdentityConfiguration.ConfigureConcurrencyToken(builder);
    builder.Property(membership => membership.RoleCodes).HasColumnType("text[]").IsRequired();
    builder.Property(membership => membership.PermissionCodes).HasColumnType("text[]").IsRequired();
    builder.Property(membership => membership.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
    builder.Ignore(membership => membership.IsActive);
    builder.Ignore(membership => membership.IsDeleted);

    builder.HasIndex(membership => new { membership.OrganizationId, membership.UserId }).IsUnique();
    builder.HasIndex(membership => new { membership.UserId, membership.Status });
  }
}

public sealed class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
  public void Configure(EntityTypeBuilder<IdentityRole> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("IdentityRoles");
    builder.HasKey(role => role.Id);

    builder.Property(role => role.Id).HasEntityIdConversion();
    builder.Property(role => role.OrganizationId).HasOrganizationIdConversion();
    builder.Property(role => role.CreatedByUserId).HasNullableUserIdConversion();
    builder.Property(role => role.UpdatedByUserId).HasNullableUserIdConversion();
    builder.Property(role => role.DeletedByUserId).HasNullableUserIdConversion();
    IdentityConfiguration.ConfigureConcurrencyToken(builder);
    builder.Property(role => role.Code).HasMaxLength(96).IsRequired();
    builder.Property(role => role.DisplayName).HasMaxLength(120).IsRequired();
    builder.Property(role => role.Description).HasMaxLength(400);
    builder.Ignore(role => role.IsDeleted);

    builder.HasIndex(role => new { role.OrganizationId, role.Code }).IsUnique();
  }
}

public sealed class IdentityPermissionConfiguration : IEntityTypeConfiguration<IdentityPermission>
{
  public void Configure(EntityTypeBuilder<IdentityPermission> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("IdentityPermissions");
    builder.HasKey(permission => permission.Id);

    builder.Property(permission => permission.Id).HasEntityIdConversion();
    IdentityConfiguration.ConfigureConcurrencyToken(builder);
    builder.Property(permission => permission.Code).HasMaxLength(128).IsRequired();
    builder.Property(permission => permission.Module).HasMaxLength(96).IsRequired();
    builder.Property(permission => permission.Action).HasMaxLength(48).IsRequired();
    builder.Property(permission => permission.Description).HasMaxLength(400);

    builder.HasIndex(permission => permission.Code).IsUnique();
    builder.HasIndex(permission => new { permission.Module, permission.Action });
  }
}

public sealed class IdentityRolePermissionConfiguration : IEntityTypeConfiguration<IdentityRolePermission>
{
  public void Configure(EntityTypeBuilder<IdentityRolePermission> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("IdentityRolePermissions");
    builder.HasKey(rolePermission => rolePermission.Id);

    builder.Property(rolePermission => rolePermission.Id).HasEntityIdConversion();
    builder.Property(rolePermission => rolePermission.OrganizationId).HasOrganizationIdConversion();
    builder.Property(rolePermission => rolePermission.RoleId).HasEntityIdConversion();
    builder.Property(rolePermission => rolePermission.PermissionId).HasNullableEntityIdConversion();
    builder.Property(rolePermission => rolePermission.CreatedByUserId).HasNullableUserIdConversion();
    builder.Property(rolePermission => rolePermission.UpdatedByUserId).HasNullableUserIdConversion();
    builder.Property(rolePermission => rolePermission.DeletedByUserId).HasNullableUserIdConversion();
    IdentityConfiguration.ConfigureConcurrencyToken(builder);
    builder.Property(rolePermission => rolePermission.PermissionCode).HasMaxLength(128).IsRequired();
    builder.Ignore(rolePermission => rolePermission.IsDeleted);

    builder.HasIndex(rolePermission => new
    {
      rolePermission.OrganizationId,
      rolePermission.RoleId,
      rolePermission.PermissionCode
    }).IsUnique();
  }
}

file static class IdentityConfiguration
{
  public static void ConfigureConcurrencyToken<TEntity>(EntityTypeBuilder<TEntity> builder)
    where TEntity : class, IConcurrencyTracked
  {
    builder.Property(entity => entity.ConcurrencyToken)
      .HasConversion(token => token.Value, value => new ConcurrencyToken(value))
      .HasMaxLength(64)
      .IsRequired();
  }
}

internal static class IdentityPropertyBuilderExtensions
{
  public static PropertyBuilder<EntityId> HasEntityIdConversion(this PropertyBuilder<EntityId> property) =>
    property.HasConversion(id => id.Value, value => new EntityId(value));

  public static PropertyBuilder<EntityId?> HasNullableEntityIdConversion(this PropertyBuilder<EntityId?> property) =>
    property.HasConversion<Guid?>(
      id => id.HasValue ? id.Value.Value : null,
      value => value.HasValue ? new EntityId(value.Value) : null);

  public static PropertyBuilder<OrganizationId> HasOrganizationIdConversion(
    this PropertyBuilder<OrganizationId> property) =>
    property.HasConversion(id => id.Value, value => new OrganizationId(value));

  public static PropertyBuilder<UserId> HasUserIdConversion(this PropertyBuilder<UserId> property) =>
    property.HasConversion(id => id.Value, value => new UserId(value));

  public static PropertyBuilder<UserId?> HasNullableUserIdConversion(this PropertyBuilder<UserId?> property) =>
    property.HasConversion<Guid?>(
      id => id.HasValue ? id.Value.Value : null,
      value => value.HasValue ? new UserId(value.Value) : null);
}
