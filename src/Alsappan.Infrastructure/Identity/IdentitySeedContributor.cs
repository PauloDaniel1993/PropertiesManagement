using System.Security.Cryptography;
using System.Text;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Seeding;
using Alsappan.Application.Identity.Security;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Identity;

public sealed class IdentitySeedContributor : IDatabaseSeedContributor
{
  private static readonly OrganizationId DefaultOrganizationId =
    new(new Guid("11111111-1111-1111-1111-111111111111"));

  private static readonly UserId DefaultAdminUserId =
    new(new Guid("22222222-2222-2222-2222-222222222222"));

  private static readonly (string Code, string DisplayName, string? Description, bool Assignable)[] StarterRoles =
  [
    (RoleCodes.OrganizationAdmin, "Administrador", "Acesso administrativo completo.", true),
    (RoleCodes.OrganizationManager, "Gestor", "Gestao operacional com acesso ampliado.", true),
    (RoleCodes.OrganizationStaff, "Operador", "Operacao diaria dos modulos principais.", true),
    (RoleCodes.OrganizationViewer, "Leitura", "Acesso somente leitura aos modulos permitidos.", true),
    (RoleCodes.ResidentUser, "Morador", "Acesso ao portal do morador.", false)
  ];

  private static readonly string[] Modules =
  [
    PermissionModules.Administrators,
    PermissionModules.Audit,
    PermissionModules.Branding,
    PermissionModules.Contracts,
    PermissionModules.Dashboard,
    PermissionModules.Documents,
    PermissionModules.Identity,
    PermissionModules.Inspections,
    PermissionModules.Notifications,
    PermissionModules.Occurrences,
    PermissionModules.Payments,
    PermissionModules.Pets,
    PermissionModules.Properties,
    PermissionModules.Residents,
    PermissionModules.Settings,
    PermissionModules.Timeline,
    PermissionModules.UtilityAccounts,
    PermissionModules.Vehicles
  ];

  private static readonly string[] Actions =
  [
    PermissionActions.Read,
    PermissionActions.Write,
    PermissionActions.Manage,
    PermissionActions.Archive,
    PermissionActions.Approve
  ];

  public string Name => "identity-starter-data";

  public string Version => "2026.06.27";

  public async Task SeedAsync(
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(serviceProvider);

    var dbContext = serviceProvider.GetRequiredService<AlsappanDbContext>();
    var passwordHashService = serviceProvider.GetRequiredService<IPasswordHashService>();
    var rolePermissionCatalog = serviceProvider.GetRequiredService<IRolePermissionCatalog>();
    var clock = serviceProvider.GetRequiredService<TimeProvider>();
    var now = clock.GetUtcNow();

    if (!await dbContext.IdentityOrganizations
      .IgnoreQueryFilters()
      .AnyAsync(organization => organization.Id == DefaultOrganizationId, cancellationToken)
      .ConfigureAwait(false))
    {
      dbContext.IdentityOrganizations.Add(IdentityOrganization.Create(
        DefaultOrganizationId,
        "alsappan",
        "Alsappan",
        now,
        displayName: "Alsappan",
        locale: IdentityDefaults.DefaultLocale,
        currency: IdentityDefaults.DefaultCurrency));
    }

    var permissionIdsByCode = await EnsurePermissionsAsync(dbContext, cancellationToken).ConfigureAwait(false);
    var roleIdsByCode = await EnsureRolesAsync(dbContext, now, cancellationToken).ConfigureAwait(false);
    await EnsureRolePermissionsAsync(
        dbContext,
        rolePermissionCatalog,
        roleIdsByCode,
        permissionIdsByCode,
        now,
        cancellationToken)
      .ConfigureAwait(false);

    if (!await dbContext.IdentityUsers
      .IgnoreQueryFilters()
      .AnyAsync(user => user.Id == DefaultAdminUserId, cancellationToken)
      .ConfigureAwait(false))
    {
      var admin = IdentityUser.Create(
        DefaultAdminUserId,
        "admin@alsappan.local",
        "Administrador Alsappan",
        UserAccountType.Admin,
        now,
        status: UserStatus.Active);
      admin.SetPasswordHash(passwordHashService.HashPassword(admin, "alsappan"), now, admin.Id);
      dbContext.IdentityUsers.Add(admin);
    }

    if (!await dbContext.IdentityMemberships
      .IgnoreQueryFilters()
      .AnyAsync(
        membership => membership.OrganizationId == DefaultOrganizationId &&
          membership.UserId == DefaultAdminUserId,
        cancellationToken)
      .ConfigureAwait(false))
    {
      dbContext.IdentityMemberships.Add(IdentityMembership.Create(
        EntityId.New(),
        DefaultOrganizationId,
        DefaultAdminUserId,
        [RoleCodes.OrganizationAdmin],
        now,
        DefaultAdminUserId));
    }

    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private static async Task<Dictionary<string, EntityId>> EnsurePermissionsAsync(
    AlsappanDbContext dbContext,
    CancellationToken cancellationToken)
  {
    var existingPermissions = await dbContext.IdentityPermissions
      .ToDictionaryAsync(
        permission => permission.Code,
        permission => permission.Id,
        StringComparer.OrdinalIgnoreCase,
        cancellationToken)
      .ConfigureAwait(false);

    foreach (var permissionCode in EnumeratePermissionCodes())
    {
      if (existingPermissions.ContainsKey(permissionCode))
      {
        continue;
      }

      var (module, action) = SplitPermission(permissionCode);
      var permission = IdentityPermission.Create(
        DeterministicEntityId($"permission:{permissionCode}"),
        permissionCode,
        module,
        action);
      dbContext.IdentityPermissions.Add(permission);
      existingPermissions[permission.Code] = permission.Id;
    }

    return existingPermissions;
  }

  private static async Task<Dictionary<string, EntityId>> EnsureRolesAsync(
    AlsappanDbContext dbContext,
    DateTimeOffset now,
    CancellationToken cancellationToken)
  {
    var existingRoles = await dbContext.IdentityRoles
      .IgnoreQueryFilters()
      .Where(role => role.OrganizationId == DefaultOrganizationId)
      .ToDictionaryAsync(role => role.Code, role => role.Id, StringComparer.OrdinalIgnoreCase, cancellationToken)
      .ConfigureAwait(false);

    foreach (var starterRole in StarterRoles)
    {
      if (existingRoles.ContainsKey(starterRole.Code))
      {
        continue;
      }

      var role = IdentityRole.Create(
        DeterministicEntityId($"role:{DefaultOrganizationId}:{starterRole.Code}"),
        DefaultOrganizationId,
        starterRole.Code,
        starterRole.DisplayName,
        now,
        description: starterRole.Description,
        isAssignable: starterRole.Assignable);
      dbContext.IdentityRoles.Add(role);
      existingRoles[role.Code] = role.Id;
    }

    return existingRoles;
  }

  private static async Task EnsureRolePermissionsAsync(
    AlsappanDbContext dbContext,
    IRolePermissionCatalog rolePermissionCatalog,
    Dictionary<string, EntityId> roleIdsByCode,
    Dictionary<string, EntityId> permissionIdsByCode,
    DateTimeOffset now,
    CancellationToken cancellationToken)
  {
    var existingKeys = await dbContext.IdentityRolePermissions
      .IgnoreQueryFilters()
      .Where(rolePermission => rolePermission.OrganizationId == DefaultOrganizationId)
      .Select(rolePermission => new
      {
        rolePermission.RoleId,
        rolePermission.PermissionCode
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var existingSet = existingKeys
      .Select(item => $"{item.RoleId}:{item.PermissionCode}")
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var roleCode in StarterRoles.Select(role => role.Code))
    {
      var roleId = roleIdsByCode[roleCode];
      foreach (var permissionCode in rolePermissionCatalog.GetPermissionsForRoles([roleCode]))
      {
        if (!permissionIdsByCode.TryGetValue(permissionCode, out var permissionId))
        {
          continue;
        }

        var key = $"{roleId}:{permissionCode}";
        if (existingSet.Contains(key))
        {
          continue;
        }

        dbContext.IdentityRolePermissions.Add(IdentityRolePermission.Create(
          EntityId.New(),
          DefaultOrganizationId,
          roleId,
          permissionCode,
          now,
          permissionId: permissionId));
        existingSet.Add(key);
      }
    }
  }

  private static IEnumerable<string> EnumeratePermissionCodes()
  {
    yield return PermissionCodes.Wildcard;
    yield return PermissionCodes.OrganizationsSwitch;
    yield return PermissionCodes.PlatformManage;

    foreach (var module in Modules)
    {
      foreach (var action in Actions)
      {
        yield return PermissionCodes.For(module, action);
      }
    }
  }

  private static (string Module, string Action) SplitPermission(string permissionCode)
  {
    if (permissionCode == PermissionCodes.Wildcard)
    {
      return ("*", "*");
    }

    var dotIndex = permissionCode.LastIndexOf('.');
    return dotIndex < 0
      ? (permissionCode, PermissionActions.Manage)
      : (permissionCode[..dotIndex], permissionCode[(dotIndex + 1)..]);
  }

  private static EntityId DeterministicEntityId(string value)
  {
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return new EntityId(new Guid(hash[..16]));
  }
}
