using System.Linq.Expressions;
using System.Text;
using Alsappan.Application.Common.Configuration;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Audit;
using Alsappan.Infrastructure.Auth;
using Alsappan.Infrastructure.Notifications;
using Alsappan.Infrastructure.Outbox;
using Alsappan.Infrastructure.Seeding;
using Alsappan.Infrastructure.Timeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Alsappan.Infrastructure.Persistence;

public sealed class AlsappanDbContext : DbContext
{
  private static readonly ValueConverter<EntityId, Guid> EntityIdConverter =
    new(id => id.Value, value => new EntityId(value));

  private static readonly ValueConverter<EntityId?, Guid?> NullableEntityIdConverter =
    new(
      id => id.HasValue ? id.Value.Value : null,
      value => value.HasValue ? new EntityId(value.Value) : null);

  private static readonly ValueConverter<OrganizationId, Guid> OrganizationIdConverter =
    new(id => id.Value, value => new OrganizationId(value));

  private static readonly ValueConverter<OrganizationId?, Guid?> NullableOrganizationIdConverter =
    new(
      id => id.HasValue ? id.Value.Value : null,
      value => value.HasValue ? new OrganizationId(value.Value) : null);

  private static readonly ValueConverter<UserId, Guid> UserIdConverter =
    new(id => id.Value, value => new UserId(value));

  private static readonly ValueConverter<UserId?, Guid?> NullableUserIdConverter =
    new(
      id => id.HasValue ? id.Value.Value : null,
      value => value.HasValue ? new UserId(value.Value) : null);

  private static readonly ValueConverter<ConcurrencyToken, string> ConcurrencyTokenConverter =
    new(token => token.Value, value => new ConcurrencyToken(value));

  private readonly IActiveOrganizationContext _activeOrganizationContext;

  public AlsappanDbContext(DbContextOptions<AlsappanDbContext> options)
    : this(options, NoActiveOrganizationContext.Instance, new DatabaseOptions())
  {
  }

  public AlsappanDbContext(
    DbContextOptions<AlsappanDbContext> options,
    IActiveOrganizationContext activeOrganizationContext,
    DatabaseOptions? databaseOptions = null)
    : base(options)
  {
    _activeOrganizationContext = activeOrganizationContext ??
      throw new ArgumentNullException(nameof(activeOrganizationContext));
    Schema = NormalizeSchema(databaseOptions?.Schema);
  }

  public string Schema { get; }

  public OrganizationId? ActiveOrganizationId => _activeOrganizationContext.OrganizationId;

  public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

  public DbSet<IdentityOrganization> IdentityOrganizations => Set<IdentityOrganization>();

  public DbSet<IdentityUser> IdentityUsers => Set<IdentityUser>();

  public DbSet<IdentityMembership> IdentityMemberships => Set<IdentityMembership>();

  public DbSet<IdentityRole> IdentityRoles => Set<IdentityRole>();

  public DbSet<IdentityPermission> IdentityPermissions => Set<IdentityPermission>();

  public DbSet<IdentityRolePermission> IdentityRolePermissions => Set<IdentityRolePermission>();

  public DbSet<ResidentAccountLink> ResidentAccountLinks => Set<ResidentAccountLink>();

  public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();

  public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

  public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

  public DbSet<TimelineEntry> TimelineEntries => Set<TimelineEntry>();

  public DbSet<NotificationRecord> NotificationRecords => Set<NotificationRecord>();

  public DbSet<NotificationPreferenceRecord> NotificationPreferenceRecords =>
    Set<NotificationPreferenceRecord>();

  public DbSet<RentalProperty> Properties => Set<RentalProperty>();

  public DbSet<Resident> Residents => Set<Resident>();

  public DbSet<SeedHistoryRecord> SeedHistoryRecords => Set<SeedHistoryRecord>();

  public static string NormalizeSchema(string? schema) =>
    string.IsNullOrWhiteSpace(schema) ? "app" : schema.Trim();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    ArgumentNullException.ThrowIfNull(modelBuilder);

    modelBuilder.HasDefaultSchema(Schema);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlsappanDbContext).Assembly);
    ApplyDomainValueObjectConversions(modelBuilder);
    ApplyTenantAndSoftDeleteFilters(modelBuilder);
    ApplySnakeCaseNames(modelBuilder);

    base.OnModelCreating(modelBuilder);
  }

  private static void ApplyDomainValueObjectConversions(ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      foreach (var property in entityType.GetProperties())
      {
        if (property.ClrType == typeof(EntityId))
        {
          property.SetValueConverter(EntityIdConverter);
        }
        else if (property.ClrType == typeof(EntityId?))
        {
          property.SetValueConverter(NullableEntityIdConverter);
        }
        else if (property.ClrType == typeof(OrganizationId))
        {
          property.SetValueConverter(OrganizationIdConverter);
        }
        else if (property.ClrType == typeof(OrganizationId?))
        {
          property.SetValueConverter(NullableOrganizationIdConverter);
        }
        else if (property.ClrType == typeof(UserId))
        {
          property.SetValueConverter(UserIdConverter);
        }
        else if (property.ClrType == typeof(UserId?))
        {
          property.SetValueConverter(NullableUserIdConverter);
        }
        else if (property.ClrType == typeof(ConcurrencyToken))
        {
          property.SetValueConverter(ConcurrencyTokenConverter);
          property.SetMaxLength(64);
        }
      }
    }
  }

  private void ApplyTenantAndSoftDeleteFilters(ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      if (entityType.ClrType is null || entityType.ClrType.IsAbstract)
      {
        continue;
      }

      var parameter = Expression.Parameter(entityType.ClrType, "entity");
      Expression? filter = null;

      if (typeof(IOrganizationScoped).IsAssignableFrom(entityType.ClrType))
      {
        var organizationProperty = Expression.Property(
          parameter,
          nameof(IOrganizationScoped.OrganizationId));
        var context = Expression.Constant(this);
        var activeOrganization = Expression.Property(
          context,
          nameof(ActiveOrganizationId));
        var hasActiveOrganization = Expression.Property(
          activeOrganization,
          nameof(Nullable<OrganizationId>.HasValue));
        var activeOrganizationValue = Expression.Property(
          activeOrganization,
          nameof(Nullable<OrganizationId>.Value));
        var matchesOrganization = Expression.Equal(organizationProperty, activeOrganizationValue);

        filter = Expression.AndAlso(hasActiveOrganization, matchesOrganization);
      }

      if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
      {
        var deletedAtProperty = Expression.Property(parameter, nameof(IDeletionAudited.DeletedAt));
        var notDeleted = Expression.Equal(
          deletedAtProperty,
          Expression.Constant(null, typeof(DateTimeOffset?)));
        filter = filter is null ? notDeleted : Expression.AndAlso(filter, notDeleted);
      }

      if (filter is not null)
      {
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
          Expression.Lambda(filter, parameter));
      }
    }
  }

  private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      var tableName = entityType.GetTableName();
      if (!string.IsNullOrWhiteSpace(tableName))
      {
        entityType.SetTableName(ToSnakeCase(tableName));
      }

      foreach (var property in entityType.GetProperties())
      {
        property.SetColumnName(ToSnakeCase(property.GetColumnName() ?? property.Name));
      }

      foreach (var key in entityType.GetKeys())
      {
        var keyName = key.GetName();
        if (!string.IsNullOrWhiteSpace(keyName))
        {
          key.SetName(ToSnakeCase(keyName));
        }
      }

      foreach (var foreignKey in entityType.GetForeignKeys())
      {
        var constraintName = foreignKey.GetConstraintName();
        if (!string.IsNullOrWhiteSpace(constraintName))
        {
          foreignKey.SetConstraintName(ToSnakeCase(constraintName));
        }
      }

      foreach (var index in entityType.GetIndexes())
      {
        var indexName = index.GetDatabaseName();
        if (!string.IsNullOrWhiteSpace(indexName))
        {
          index.SetDatabaseName(ToSnakeCase(indexName));
        }
      }
    }
  }

  public static string ToSnakeCase(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);

    var builder = new StringBuilder(value.Length + 8);
    var previousWasSeparator = false;

    for (var index = 0; index < value.Length; index++)
    {
      var character = value[index];
      if (character is '-' or ' ')
      {
        AppendSeparator(builder, ref previousWasSeparator);
        continue;
      }

      if (char.IsUpper(character))
      {
        if (index > 0 &&
          !previousWasSeparator &&
          (index + 1 < value.Length && char.IsLower(value[index + 1]) ||
            !char.IsUpper(value[index - 1])))
        {
          AppendSeparator(builder, ref previousWasSeparator);
        }

        builder.Append(char.ToLowerInvariant(character));
        previousWasSeparator = false;
        continue;
      }

      builder.Append(character);
      previousWasSeparator = character == '_';
    }

    return builder.ToString().Trim('_');

    static void AppendSeparator(StringBuilder builder, ref bool previousWasSeparator)
    {
      if (builder.Length > 0 && !previousWasSeparator)
      {
        builder.Append('_');
      }

      previousWasSeparator = true;
    }
  }
}
