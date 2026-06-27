using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Seeding;
using Alsappan.Application.Identity.Security;
using Alsappan.Application.Payments;
using Alsappan.Application.Settings.Repositories;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Inspections;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Payments;
using Alsappan.Domain.UtilityAccounts;
using Alsappan.Infrastructure;
using Alsappan.Infrastructure.Authorization;
using Alsappan.Infrastructure.Identity;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Seeding;
using Alsappan.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Alsappan.Infrastructure.Tests.Seeding;

public sealed class DemoSeedContributorTests
{
  private static readonly Guid DemoOrganizationGuid = new("11111111-1111-1111-1111-111111111111");

  [Fact]
  public async Task DemoSeedContributorSeedsConnectedTenantDataIdempotently()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    await using var provider = CreateProvider(databaseName);

    await using (var scope = provider.CreateAsyncScope())
    {
      await new IdentitySeedContributor().SeedAsync(scope.ServiceProvider);
      await new SettingsSeedContributor().SeedAsync(scope.ServiceProvider);

      var contributor = scope.ServiceProvider.GetRequiredService<DemoSeedContributor>();
      await contributor.SeedAsync(scope.ServiceProvider);
      await contributor.SeedAsync(scope.ServiceProvider);
    }

    await using var assertionScope = provider.CreateAsyncScope();
    var dbContext = assertionScope.ServiceProvider.GetRequiredService<AlsappanDbContext>();
    var passwordHashService = assertionScope.ServiceProvider.GetRequiredService<IPasswordHashService>();
    var organizationId = new Domain.Common.Identifiers.OrganizationId(DemoOrganizationGuid);

    Assert.Equal(2, await dbContext.Properties.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(2, await dbContext.Residents.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(1, await dbContext.Contracts.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(2, await dbContext.PaymentCharges.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(2, await dbContext.UtilityAccounts.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(6, await dbContext.Documents.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(1, await dbContext.Pets.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(1, await dbContext.Vehicles.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(1, await dbContext.Occurrences.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(1, await dbContext.Inspections.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(6, await dbContext.NotificationRecords.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(6, await dbContext.TimelineEntries.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.Equal(6, await dbContext.AuditLogEntries.IgnoreQueryFilters().CountAsync(item => item.OrganizationId == organizationId));
    Assert.True(await dbContext.NotificationRecords
      .IgnoreQueryFilters()
      .AnyAsync(item => item.CorrelationId == "demo-seed:payment-july-open-resident"));
    Assert.True(await dbContext.TimelineEntries
      .IgnoreQueryFilters()
      .AnyAsync(item => item.CorrelationId == "demo-seed:payment-july-open-resident"));
    Assert.False(await dbContext.AuditLogEntries
      .IgnoreQueryFilters()
      .AnyAsync(item => item.CorrelationId == "demo-seed:payment-july-open-resident"));

    var contract = await dbContext.Contracts
      .IgnoreQueryFilters()
      .Include(item => item.Residents)
      .SingleAsync(item => item.OrganizationId == organizationId);
    Assert.Equal(ContractStatus.Active, contract.Status);
    Assert.Equal(2, contract.Residents.Count);

    var residentUser = await dbContext.IdentityUsers
      .IgnoreQueryFilters()
      .SingleAsync(user => user.Email == "ana.moradora@alsappan.local");
    Assert.Equal(UserAccountType.Resident, residentUser.AccountType);
    Assert.NotEqual(
      PasswordVerificationResult.Failed,
      passwordHashService.VerifyPassword(residentUser, "alsappan", residentUser.PasswordHash!));

    var residentLink = await dbContext.ResidentAccountLinks
      .IgnoreQueryFilters()
      .SingleAsync(link => link.OrganizationId == organizationId);
    Assert.True(residentLink.IsActive);
    Assert.Equal(residentUser.Id, residentLink.UserId);

    var paidCharge = await dbContext.PaymentCharges
      .IgnoreQueryFilters()
      .Include(charge => charge.Transactions)
      .Include(charge => charge.ReceiptLinks)
      .SingleAsync(charge => charge.Title == "Aluguel - Junho 2026");
    Assert.Equal(PaymentStatus.Paid, paidCharge.Status);
    Assert.Equal(PaymentCatalog.MockPixProvider, paidCharge.ProviderCode);
    Assert.Single(paidCharge.Transactions);
    Assert.Single(paidCharge.ReceiptLinks);

    var pendingCharge = await dbContext.PaymentCharges
      .IgnoreQueryFilters()
      .SingleAsync(charge => charge.Title == "Aluguel - Julho 2026");
    Assert.Equal(PaymentStatus.Pending, pendingCharge.Status);
    Assert.Equal(PaymentCatalog.MockBoletoProvider, pendingCharge.ProviderCode);

    var openUtility = await dbContext.UtilityAccounts
      .IgnoreQueryFilters()
      .Include(account => account.DocumentLinks)
      .SingleAsync(account => account.Title == "Energia - Junho 2026");
    Assert.Equal(UtilityAccountStatus.Open, openUtility.Status);
    Assert.Single(openUtility.DocumentLinks);

    var occurrence = await dbContext.Occurrences
      .IgnoreQueryFilters()
      .Include(item => item.Comments)
      .Include(item => item.Attachments)
      .Include(item => item.StatusHistory)
      .SingleAsync(item => item.Title == "Vazamento na cozinha");
    Assert.Equal(OccurrenceStatus.InProgress, occurrence.Status);
    Assert.Single(occurrence.Comments);
    Assert.Single(occurrence.Attachments);
    Assert.True(occurrence.StatusHistory.Count >= 2);

    var inspection = await dbContext.Inspections
      .IgnoreQueryFilters()
      .Include(item => item.ChecklistItems)
      .Include(item => item.DocumentLinks)
      .Include(item => item.SignatureSlots)
      .SingleAsync(item => item.Title == "Vistoria periodica - Apartamento Aurora 1201");
    Assert.Equal(InspectionStatus.Completed, inspection.Status);
    Assert.Equal(2, inspection.ChecklistItems.Count);
    Assert.Single(inspection.DocumentLinks);
    Assert.All(inspection.SignatureSlots, slot => Assert.True(slot.IsSigned));
  }

  [Fact]
  public void InfrastructureRegistrationOmitsDemoSeedContributorUnlessExplicitlyEnabledInDemoEnvironment()
  {
    var disabledServices = CreateInfrastructureServices(enableDemoData: false, environmentName: "Development");
    var productionServices = CreateInfrastructureServices(enableDemoData: true, environmentName: "Production");
    var enabledServices = CreateInfrastructureServices(enableDemoData: true, environmentName: "Development");

    Assert.DoesNotContain(disabledServices, IsDemoSeedRegistration);
    Assert.DoesNotContain(productionServices, IsDemoSeedRegistration);
    Assert.Contains(enabledServices, IsDemoSeedRegistration);
  }

  [Fact]
  public async Task DemoSeedContributorRejectsDisabledOptionsWithoutSeedingDemoTenant()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    await using var provider = CreateProvider(databaseName, enableDemoData: false);
    await using var scope = provider.CreateAsyncScope();
    var contributor = scope.ServiceProvider.GetRequiredService<DemoSeedContributor>();

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => contributor.SeedAsync(scope.ServiceProvider));

    Assert.Contains("Alsappan:Seeding:EnableDemoData", exception.Message, StringComparison.Ordinal);
    var dbContext = scope.ServiceProvider.GetRequiredService<AlsappanDbContext>();
    var organizationId = new Domain.Common.Identifiers.OrganizationId(DemoOrganizationGuid);
    Assert.False(await dbContext.IdentityOrganizations
      .IgnoreQueryFilters()
      .AnyAsync(organization => organization.Id == organizationId));
  }

  private static ServiceProvider CreateProvider(
    string databaseName,
    bool enableDemoData = true,
    string environmentName = "Development")
  {
    var services = new ServiceCollection();
    services.AddSingleton(TimeProvider.System);
    services.AddSingleton(new DemoSeedEnvironment(environmentName));
    services.AddSingleton(Options.Create(new AlsappanOptions
    {
      Seeding = new SeedingOptions { EnableDemoData = enableDemoData }
    }));
    services.AddSingleton<IRolePermissionCatalog, DefaultRolePermissionCatalog>();
    services.AddScoped<IPasswordHashService, Pbkdf2PasswordHashService>();
    services.AddScoped<ISettingsRepository, EfSettingsRepository>();
    services.AddScoped<DemoSeedContributor>();
    services.AddDbContext<AlsappanDbContext>(options => options
      .UseInMemoryDatabase(databaseName)
      .ReplaceService<IModelCacheKeyFactory, AlsappanModelCacheKeyFactory>());

    return services.BuildServiceProvider();
  }

  private static ServiceCollection CreateInfrastructureServices(bool enableDemoData, string environmentName)
  {
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Alsappan:Seeding:EnableDemoData"] = enableDemoData ? "true" : "false"
      })
      .Build();

    services.AddInfrastructure(environmentName, configuration);
    return services;
  }

  private static bool IsDemoSeedRegistration(ServiceDescriptor descriptor) =>
    descriptor.ServiceType == typeof(IDatabaseSeedContributor) &&
    descriptor.ImplementationType == typeof(DemoSeedContributor);
}
