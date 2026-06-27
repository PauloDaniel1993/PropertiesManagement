using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Seeding;
using Alsappan.Application.Identity.Security;
using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Providers;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Inspections;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Payments;
using Alsappan.Domain.Pets;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Domain.UtilityAccounts;
using Alsappan.Domain.Vehicles;
using Alsappan.Infrastructure.Audit;
using Alsappan.Infrastructure.Notifications;
using Alsappan.Infrastructure.Payments;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Timeline;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Alsappan.Infrastructure.Seeding;

public sealed record DemoSeedEnvironment(string EnvironmentName);

public sealed class DemoSeedContributor : IDatabaseSeedContributor
{
  internal const string EnableDemoDataPath = "Alsappan:Seeding:EnableDemoData";

  private const string DemoPassword = "alsappan";
  private const string Locale = "pt-BR";
  private const string Currency = "BRL";

  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly DateTimeOffset DemoNow = new(2026, 6, 27, 9, 0, 0, TimeSpan.Zero);
  private static readonly OrganizationId DemoOrganizationId = new(new Guid("11111111-1111-1111-1111-111111111111"));
  private static readonly UserId AdminUserId = new(new Guid("22222222-2222-2222-2222-222222222222"));
  private static readonly UserId StaffUserId = UserId("staff-user");
  private static readonly UserId ResidentUserId = UserId("resident-user");

  private readonly DemoSeedEnvironment _environment;
  private readonly SeedingOptions _seedingOptions;

  public DemoSeedContributor(
    IOptions<AlsappanOptions> options,
    DemoSeedEnvironment environment)
  {
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(environment);

    _seedingOptions = options.Value.Seeding;
    _environment = environment;
  }

  public string Name => "tenant-demo-data";

  public string Version => "2026.06.27";

  public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(serviceProvider);
    EnsureEnabled();

    var dbContext = serviceProvider.GetRequiredService<AlsappanDbContext>();
    var passwordHashService = serviceProvider.GetRequiredService<IPasswordHashService>();

    await EnsureDemoIdentityAsync(dbContext, passwordHashService, cancellationToken).ConfigureAwait(false);

    var propertyAurora = await EnsurePropertyAuroraAsync(dbContext, cancellationToken).ConfigureAwait(false);
    var propertySerena = await EnsurePropertySerenaAsync(dbContext, cancellationToken).ConfigureAwait(false);
    var residentAna = await EnsureResidentAnaAsync(dbContext, cancellationToken).ConfigureAwait(false);
    var residentCarlos = await EnsureResidentCarlosAsync(dbContext, cancellationToken).ConfigureAwait(false);
    var contract = await EnsureContractAsync(dbContext, propertyAurora, residentAna, residentCarlos, cancellationToken)
      .ConfigureAwait(false);
    var documents = await EnsureDocumentsAsync(
        dbContext,
        propertyAurora,
        residentAna,
        contract,
        cancellationToken)
      .ConfigureAwait(false);

    await EnsurePaymentsAsync(dbContext, contract, propertyAurora, residentAna, documents, cancellationToken)
      .ConfigureAwait(false);
    await EnsureUtilityAccountsAsync(dbContext, contract, propertyAurora, residentAna, documents, cancellationToken)
      .ConfigureAwait(false);
    await EnsurePetAsync(dbContext, propertyAurora, residentAna, contract, documents, cancellationToken)
      .ConfigureAwait(false);
    await EnsureVehicleAsync(dbContext, propertyAurora, residentAna, contract, cancellationToken).ConfigureAwait(false);
    var occurrence = await EnsureOccurrenceAsync(dbContext, propertyAurora, residentAna, contract, documents, cancellationToken)
      .ConfigureAwait(false);
    var inspection = await EnsureInspectionAsync(dbContext, propertyAurora, residentAna, contract, documents, cancellationToken)
      .ConfigureAwait(false);

    await EnsureTimelineNotificationsAndAuditAsync(
        dbContext,
        propertyAurora,
        propertySerena,
        residentAna,
        contract,
        occurrence,
        inspection,
        cancellationToken)
      .ConfigureAwait(false);

    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private static async Task EnsureDemoIdentityAsync(
    AlsappanDbContext dbContext,
    IPasswordHashService passwordHashService,
    CancellationToken cancellationToken)
  {
    if (!await dbContext.IdentityOrganizations
      .IgnoreQueryFilters()
      .AnyAsync(organization => organization.Id == DemoOrganizationId, cancellationToken)
      .ConfigureAwait(false))
    {
      dbContext.IdentityOrganizations.Add(IdentityOrganization.Create(
        DemoOrganizationId,
        "alsappan",
        "Alsappan",
        DemoNow,
        displayName: "Alsappan",
        locale: Locale,
        currency: Currency));
    }

    await EnsureUserAsync(
        dbContext,
        passwordHashService,
        StaffUserId,
        "operador@alsappan.local",
        "Bruno Operador",
        UserAccountType.Admin,
        RoleCodes.OrganizationStaff,
        cancellationToken)
      .ConfigureAwait(false);

    await EnsureUserAsync(
        dbContext,
        passwordHashService,
        ResidentUserId,
        "ana.moradora@alsappan.local",
        "Ana Beatriz Lima",
        UserAccountType.Resident,
        RoleCodes.ResidentUser,
        cancellationToken)
      .ConfigureAwait(false);

    if (!await dbContext.IdentityUsers
      .IgnoreQueryFilters()
      .AnyAsync(user => user.Id == AdminUserId, cancellationToken)
      .ConfigureAwait(false))
    {
      var admin = IdentityUser.Create(
        AdminUserId,
        "admin@alsappan.local",
        "Administrador Alsappan",
        UserAccountType.Admin,
        DemoNow,
        status: UserStatus.Active);
      admin.SetPasswordHash(passwordHashService.HashPassword(admin, DemoPassword), DemoNow, AdminUserId);
      dbContext.IdentityUsers.Add(admin);
    }
  }

  private static async Task EnsureUserAsync(
    AlsappanDbContext dbContext,
    IPasswordHashService passwordHashService,
    UserId userId,
    string email,
    string displayName,
    UserAccountType accountType,
    string roleCode,
    CancellationToken cancellationToken)
  {
    if (!await dbContext.IdentityUsers
      .IgnoreQueryFilters()
      .AnyAsync(user => user.Id == userId, cancellationToken)
      .ConfigureAwait(false))
    {
      var user = IdentityUser.Create(userId, email, displayName, accountType, DemoNow, AdminUserId, UserStatus.Active);
      user.SetPasswordHash(passwordHashService.HashPassword(user, DemoPassword), DemoNow, AdminUserId);
      dbContext.IdentityUsers.Add(user);
    }

    if (!await dbContext.IdentityMemberships
      .IgnoreQueryFilters()
      .AnyAsync(
        membership => membership.OrganizationId == DemoOrganizationId && membership.UserId == userId,
        cancellationToken)
      .ConfigureAwait(false))
    {
      dbContext.IdentityMemberships.Add(IdentityMembership.Create(
        EntityId($"membership:{userId.Value:N}"),
        DemoOrganizationId,
        userId,
        [roleCode],
        DemoNow,
        AdminUserId));
    }
  }

  private static async Task<RentalProperty> EnsurePropertyAuroraAsync(
    AlsappanDbContext dbContext,
    CancellationToken cancellationToken)
  {
    var propertyId = EntityId("property:aurora-1201");
    var existing = await dbContext.Properties
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(property => property.Id == propertyId, cancellationToken)
      .ConfigureAwait(false);

    if (existing is not null)
    {
      return existing;
    }

    var property = RentalProperty.Create(
      propertyId,
      DemoOrganizationId,
      "Apartamento Aurora 1201",
      PropertyType.Apartment,
      "Apartamento mobiliado proximo ao metro, preparado para demonstracao.",
      new Address("Rua Haddock Lobo", "1201", "Apto 1201", "Cerqueira Cesar", "Sao Paulo", "SP", "01414-003"),
      PropertyStatus.Rented,
      new Money(3200m, Currency),
      1,
      "Vaga A-1201",
      "Contrato ativo com contas e ocorrencias vinculadas.",
      DemoNow.AddDays(-180),
      AdminUserId);
    dbContext.Properties.Add(property);
    return property;
  }

  private static async Task<RentalProperty> EnsurePropertySerenaAsync(
    AlsappanDbContext dbContext,
    CancellationToken cancellationToken)
  {
    var propertyId = EntityId("property:vila-serena");
    var existing = await dbContext.Properties
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(property => property.Id == propertyId, cancellationToken)
      .ConfigureAwait(false);

    if (existing is not null)
    {
      return existing;
    }

    var property = RentalProperty.Create(
      propertyId,
      DemoOrganizationId,
      "Casa Vila Serena",
      PropertyType.House,
      "Casa em manutencao preventiva antes de nova locacao.",
      new Address("Rua das Laranjeiras", "245", null, "Vila Mariana", "Sao Paulo", "SP", "04040-000"),
      PropertyStatus.Maintenance,
      new Money(4500m, Currency),
      2,
      "Garagem 01; Garagem 02",
      "Imovel reservado para demonstrar status de manutencao.",
      DemoNow.AddDays(-90),
      StaffUserId);
    dbContext.Properties.Add(property);
    return property;
  }

  private static async Task<Resident> EnsureResidentAnaAsync(
    AlsappanDbContext dbContext,
    CancellationToken cancellationToken)
  {
    var residentId = EntityId("resident:ana-beatriz-lima");
    var existing = await dbContext.Residents
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(resident => resident.Id == residentId, cancellationToken)
      .ConfigureAwait(false);

    if (existing is not null)
    {
      return existing;
    }

    var resident = Resident.Create(
      residentId,
      DemoOrganizationId,
      "Ana Beatriz Lima",
      "Ana",
      "ana.moradora@alsappan.local",
      "(11) 98888-1234",
      null,
      "CPF",
      "321.654.987-00",
      new DateOnly(1989, 4, 12),
      "Marcos Lima",
      "Irmao",
      "(11) 97777-0001",
      ResidentStatus.Active,
      ResidentPortalStatus.Active,
      ResidentPrivacyOptions.IdentificationData,
      "Responsavel principal pelo contrato do Apartamento Aurora 1201.",
      ResidentUserId,
      DemoNow.AddDays(-175),
      StaffUserId);
    dbContext.Residents.Add(resident);

    if (!await dbContext.ResidentAccountLinks
      .IgnoreQueryFilters()
      .AnyAsync(link => link.OrganizationId == DemoOrganizationId && link.ResidentId == residentId, cancellationToken)
      .ConfigureAwait(false))
    {
      dbContext.ResidentAccountLinks.Add(new ResidentAccountLink(
        EntityId("resident-account-link:ana-beatriz-lima"),
        DemoOrganizationId,
        ResidentUserId,
        residentId,
        DemoNow.AddDays(-174),
        StaffUserId));
    }

    return resident;
  }

  private static async Task<Resident> EnsureResidentCarlosAsync(
    AlsappanDbContext dbContext,
    CancellationToken cancellationToken)
  {
    var residentId = EntityId("resident:carlos-eduardo-rocha");
    var existing = await dbContext.Residents
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(resident => resident.Id == residentId, cancellationToken)
      .ConfigureAwait(false);

    if (existing is not null)
    {
      return existing;
    }

    var resident = Resident.Create(
      residentId,
      DemoOrganizationId,
      "Carlos Eduardo Rocha",
      "Carlos",
      "carlos.rocha@example.local",
      "(11) 97777-4321",
      "(11) 96666-4321",
      "CPF",
      "654.987.321-00",
      new DateOnly(1987, 11, 3),
      "Patricia Rocha",
      "Conjuge",
      "(11) 95555-4321",
      ResidentStatus.Active,
      ResidentPortalStatus.NotInvited,
      ResidentPrivacyOptions.None,
      "Morador adicional vinculado ao contrato ativo.",
      null,
      DemoNow.AddDays(-175),
      StaffUserId);
    dbContext.Residents.Add(resident);
    return resident;
  }

  private static async Task<LeaseContract> EnsureContractAsync(
    AlsappanDbContext dbContext,
    RentalProperty property,
    Resident residentAna,
    Resident residentCarlos,
    CancellationToken cancellationToken)
  {
    var contractId = EntityId("contract:aurora-2026");
    var existing = await dbContext.Contracts
      .IgnoreQueryFilters()
      .Include(contract => contract.Residents)
      .FirstOrDefaultAsync(contract => contract.Id == contractId, cancellationToken)
      .ConfigureAwait(false);

    if (existing is not null)
    {
      return existing;
    }

    var contract = LeaseContract.Create(
      contractId,
      DemoOrganizationId,
      property.Id,
      residentAna.Id,
      [residentAna.Id, residentCarlos.Id],
      new DateOnly(2026, 1, 1),
      new DateOnly(2026, 12, 31),
      new Money(3200m, Currency),
      10,
      new Money(3200m, Currency),
      ContractAdjustmentIndex.Ipca,
      12,
      new DateOnly(2027, 1, 1),
      "Multa de 2% apos vencimento e juros pro rata.",
      "Desconto de R$ 80 para pagamento ate o quinto dia util.",
      true,
      "Contrato ativo para demonstracao de relacoes entre modulos.",
      property.Name,
      $"{residentAna.FullName} {residentCarlos.FullName}",
      DemoNow.AddDays(-170),
      StaffUserId);
    contract.Activate(DemoNow.AddDays(-169), StaffUserId);
    dbContext.Contracts.Add(contract);
    return contract;
  }

  private static async Task<DemoDocuments> EnsureDocumentsAsync(
    AlsappanDbContext dbContext,
    RentalProperty property,
    Resident resident,
    LeaseContract contract,
    CancellationToken cancellationToken)
  {
    var signedContract = await EnsureDocumentAsync(
        dbContext,
        EntityId("document:signed-contract"),
        DocumentCategory.Contract,
        "Contrato assinado - Apartamento Aurora 1201",
        "Contrato de locacao assinado digitalmente.",
        "contrato-aurora-1201.pdf",
        "application/pdf",
        245_760,
        "demo/contratos/contrato-aurora-1201.pdf",
        "Versao assinada para demonstracao.",
        [
          new DocumentLinkDraft("contract", contract.Id, "Contrato ativo"),
          new DocumentLinkDraft("property", property.Id, property.Name),
          new DocumentLinkDraft("resident", resident.Id, resident.FullName)
        ],
        DemoNow.AddDays(-168),
        cancellationToken)
      .ConfigureAwait(false);

    var paymentReceipt = await EnsureDocumentAsync(
        dbContext,
        EntityId("document:payment-receipt-june"),
        DocumentCategory.PaymentReceipt,
        "Recibo de aluguel - Junho 2026",
        "Recibo do pagamento de aluguel quitado via Pix.",
        "recibo-aluguel-junho-2026.pdf",
        "application/pdf",
        86_016,
        "demo/pagamentos/recibo-aluguel-junho-2026.pdf",
        "Recibo importado para demonstracao.",
        [new DocumentLinkDraft("payment", EntityId("payment:rent-june-2026"), "Aluguel junho")],
        DemoNow.AddDays(-15),
        cancellationToken)
      .ConfigureAwait(false);

    var utilityBill = await EnsureDocumentAsync(
        dbContext,
        EntityId("document:utility-electricity-june"),
        DocumentCategory.UtilityAccount,
        "Conta de energia - Junho 2026",
        "Conta de energia vinculada ao contrato ativo.",
        "conta-energia-junho-2026.pdf",
        "application/pdf",
        91_200,
        "demo/contas/conta-energia-junho-2026.pdf",
        "Arquivo de demonstracao.",
        [new DocumentLinkDraft("utility-account", EntityId("utility:electricity-june-2026"), "Energia junho")],
        DemoNow.AddDays(-18),
        cancellationToken)
      .ConfigureAwait(false);

    var petVaccination = await EnsureDocumentAsync(
        dbContext,
        EntityId("document:pet-vaccination-luna"),
        DocumentCategory.Pet,
        "Carteira de vacinacao - Luna",
        "Comprovante de vacinacao do pet autorizado.",
        "vacinacao-luna.pdf",
        "application/pdf",
        72_704,
        "demo/pets/vacinacao-luna.pdf",
        "Documento anexado ao cadastro do pet.",
        [new DocumentLinkDraft("pet", EntityId("pet:luna"), "Vacinacao")],
        DemoNow.AddDays(-45),
        cancellationToken)
      .ConfigureAwait(false);

    var occurrencePhoto = await EnsureDocumentAsync(
        dbContext,
        EntityId("document:occurrence-leak-photo"),
        DocumentCategory.Occurrence,
        "Foto do vazamento - Cozinha",
        "Imagem enviada pela moradora para a ocorrencia.",
        "foto-vazamento-cozinha.jpg",
        "image/jpeg",
        512_000,
        "demo/ocorrencias/foto-vazamento-cozinha.jpg",
        "Anexo enviado pelo portal do morador.",
        [new DocumentLinkDraft("occurrence", EntityId("occurrence:kitchen-leak"), "Foto inicial")],
        DemoNow.AddDays(-2),
        cancellationToken)
      .ConfigureAwait(false);

    var inspectionReport = await EnsureDocumentAsync(
        dbContext,
        EntityId("document:inspection-report-aurora"),
        DocumentCategory.Inspection,
        "Relatorio de vistoria - Apartamento Aurora 1201",
        "Relatorio estruturado da vistoria periodica.",
        "vistoria-aurora-1201.pdf",
        "application/pdf",
        188_416,
        "demo/vistorias/vistoria-aurora-1201.pdf",
        "Relatorio de demonstracao.",
        [new DocumentLinkDraft("inspection", EntityId("inspection:aurora-periodic"), "Relatorio final")],
        DemoNow.AddDays(-1),
        cancellationToken)
      .ConfigureAwait(false);

    return new DemoDocuments(
      signedContract,
      paymentReceipt,
      utilityBill,
      petVaccination,
      occurrencePhoto,
      inspectionReport);
  }

  private static async Task<DocumentRecord> EnsureDocumentAsync(
    AlsappanDbContext dbContext,
    EntityId documentId,
    DocumentCategory category,
    string title,
    string description,
    string fileName,
    string contentType,
    long sizeBytes,
    string storageKey,
    string versionNotes,
    IReadOnlyCollection<DocumentLinkDraft> links,
    DateTimeOffset createdAt,
    CancellationToken cancellationToken)
  {
    var existing = await dbContext.Documents
      .IgnoreQueryFilters()
      .Include(document => document.Links)
      .Include(document => document.Versions)
      .FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken)
      .ConfigureAwait(false);

    if (existing is not null)
    {
      return existing;
    }

    var document = DocumentRecord.Create(
      documentId,
      DemoOrganizationId,
      category,
      title,
      description,
      fileName,
      contentType,
      sizeBytes,
      storageKey,
      versionNotes,
      links,
      createdAt,
      StaffUserId);
    dbContext.Documents.Add(document);
    return document;
  }

  private static async Task EnsurePaymentsAsync(
    AlsappanDbContext dbContext,
    LeaseContract contract,
    RentalProperty property,
    Resident resident,
    DemoDocuments documents,
    CancellationToken cancellationToken)
  {
    var paidChargeId = EntityId("payment:rent-june-2026");
    if (!await dbContext.PaymentCharges
      .IgnoreQueryFilters()
      .AnyAsync(charge => charge.Id == paidChargeId, cancellationToken)
      .ConfigureAwait(false))
    {
      var paidCharge = PaymentCharge.Create(
        paidChargeId,
        DemoOrganizationId,
        contract.Id,
        property.Id,
        resident.Id,
        null,
        "Aluguel - Junho 2026",
        "Mensalidade do Apartamento Aurora 1201.",
        new DateOnly(2026, 6, 10),
        new Money(3200m, Currency),
        new Money(80m, Currency),
        Money.Zero(Currency),
        PaymentMethod.Pix,
        PaymentReconciliationStatus.Pending,
        "Quitado com desconto de pontualidade.",
        "Contrato Apartamento Aurora 1201",
        property.Name,
        resident.FullName,
        DemoNow.AddDays(-30),
        StaffUserId);
      var pixInstruction = await CreateInstructionAsync(
          new MockPixPaymentInstructionProvider(),
          paidCharge,
          resident.FullName,
          cancellationToken)
        .ConfigureAwait(false);
      paidCharge.SetProviderInstruction(
        pixInstruction.ProviderCode,
        pixInstruction.ProviderReference,
        SerializeInstruction(pixInstruction),
        DemoNow.AddDays(-29),
        StaffUserId);
      paidCharge.RecordTransaction(
        EntityId("payment-transaction:rent-june-2026"),
        paidCharge.GrossAmount(),
        PaymentMethod.Pix,
        new DateOnly(2026, 6, 7),
        "PIX-E2E-DEMO-20260607",
        pixInstruction.ProviderCode,
        pixInstruction.ProviderReference,
        documents.PaymentReceipt.Id,
        "Pagamento conciliado automaticamente pelo provedor mock.",
        DemoNow.AddDays(-20),
        StaffUserId);
      dbContext.PaymentCharges.Add(paidCharge);
    }

    var pendingChargeId = EntityId("payment:rent-july-2026");
    if (!await dbContext.PaymentCharges
      .IgnoreQueryFilters()
      .AnyAsync(charge => charge.Id == pendingChargeId, cancellationToken)
      .ConfigureAwait(false))
    {
      var pendingCharge = PaymentCharge.Create(
        pendingChargeId,
        DemoOrganizationId,
        contract.Id,
        property.Id,
        resident.Id,
        null,
        "Aluguel - Julho 2026",
        "Mensalidade em aberto com instrucao de boleto mock.",
        new DateOnly(2026, 7, 10),
        new Money(3200m, Currency),
        Money.Zero(Currency),
        Money.Zero(Currency),
        PaymentMethod.Boleto,
        PaymentReconciliationStatus.Pending,
        "Aguardando pagamento pelo portal do morador.",
        "Contrato Apartamento Aurora 1201",
        property.Name,
        resident.FullName,
        DemoNow.AddDays(-3),
        StaffUserId);
      var boletoInstruction = await CreateInstructionAsync(
          new MockBoletoPaymentInstructionProvider(),
          pendingCharge,
          resident.FullName,
          cancellationToken)
        .ConfigureAwait(false);
      pendingCharge.SetProviderInstruction(
        boletoInstruction.ProviderCode,
        boletoInstruction.ProviderReference,
        SerializeInstruction(boletoInstruction),
        DemoNow.AddDays(-2),
        StaffUserId);
      dbContext.PaymentCharges.Add(pendingCharge);
    }
  }

  private static async Task EnsureUtilityAccountsAsync(
    AlsappanDbContext dbContext,
    LeaseContract contract,
    RentalProperty property,
    Resident resident,
    DemoDocuments documents,
    CancellationToken cancellationToken)
  {
    var utilityId = EntityId("utility:electricity-june-2026");
    if (!await dbContext.UtilityAccounts
      .IgnoreQueryFilters()
      .AnyAsync(account => account.Id == utilityId, cancellationToken)
      .ConfigureAwait(false))
    {
      var utility = UtilityAccount.Create(
        utilityId,
        DemoOrganizationId,
        property.Id,
        contract.Id,
        resident.Id,
        UtilityAccountType.Electricity,
        UtilityResponsibility.Contract,
        "Energia - Junho 2026",
        "Conta de energia repassada ao contrato ativo.",
        new DateOnly(2026, 6, 1),
        new DateOnly(2026, 6, 30),
        new DateOnly(2026, 7, 8),
        new Money(286.72m, Currency),
        "Leitura conferida com o relogio do apartamento.",
        property.Name,
        "Contrato Apartamento Aurora 1201",
        resident.FullName,
        DemoNow.AddDays(-10),
        StaffUserId);
      utility.LinkDocument(documents.UtilityBill.Id, UtilityDocumentKind.Bill, "Conta de energia", DemoNow.AddDays(-9), StaffUserId);
      dbContext.UtilityAccounts.Add(utility);
    }

    var paidUtilityId = EntityId("utility:condominium-june-2026");
    if (!await dbContext.UtilityAccounts
      .IgnoreQueryFilters()
      .AnyAsync(account => account.Id == paidUtilityId, cancellationToken)
      .ConfigureAwait(false))
    {
      var utility = UtilityAccount.Create(
        paidUtilityId,
        DemoOrganizationId,
        property.Id,
        contract.Id,
        resident.Id,
        UtilityAccountType.CondominiumFee,
        UtilityResponsibility.Organization,
        "Condominio - Junho 2026",
        "Taxa condominial paga pela administracao.",
        new DateOnly(2026, 6, 1),
        new DateOnly(2026, 6, 30),
        new DateOnly(2026, 6, 15),
        new Money(620m, Currency),
        "Registro usado para demonstrar contas quitadas.",
        property.Name,
        "Contrato Apartamento Aurora 1201",
        resident.FullName,
        DemoNow.AddDays(-30),
        StaffUserId);
      utility.MarkPaid(
        new Money(620m, Currency),
        new DateOnly(2026, 6, 14),
        "Transferencia bancaria",
        "TED-DEMO-20260614",
        null,
        "Pagamento conferido no extrato.",
        DemoNow.AddDays(-13),
        StaffUserId);
      dbContext.UtilityAccounts.Add(utility);
    }
  }

  private static async Task EnsurePetAsync(
    AlsappanDbContext dbContext,
    RentalProperty property,
    Resident resident,
    LeaseContract contract,
    DemoDocuments documents,
    CancellationToken cancellationToken)
  {
    var petId = EntityId("pet:luna");
    if (await dbContext.Pets
      .IgnoreQueryFilters()
      .AnyAsync(pet => pet.Id == petId, cancellationToken)
      .ConfigureAwait(false))
    {
      return;
    }

    var pet = Pet.Create(
      petId,
      DemoOrganizationId,
      resident.Id,
      property.Id,
      contract.Id,
      "Luna",
      PetSpecies.Dog,
      "Shih-tzu",
      PetAuthorizationStatus.Authorized,
      "Autorizada conforme regras do condominio.",
      "Pet de pequeno porte cadastrado no contrato.",
      resident.FullName,
      property.Name,
      "Contrato Apartamento Aurora 1201",
      DemoNow.AddDays(-50),
      StaffUserId);
    pet.LinkDocument(documents.PetVaccination.Id, PetDocumentKind.VaccinationRecord, "Carteira de vacinacao", DemoNow.AddDays(-45), StaffUserId);
    dbContext.Pets.Add(pet);
  }

  private static async Task EnsureVehicleAsync(
    AlsappanDbContext dbContext,
    RentalProperty property,
    Resident resident,
    LeaseContract contract,
    CancellationToken cancellationToken)
  {
    var vehicleId = EntityId("vehicle:ana-civic");
    if (await dbContext.Vehicles
      .IgnoreQueryFilters()
      .AnyAsync(vehicle => vehicle.Id == vehicleId, cancellationToken)
      .ConfigureAwait(false))
    {
      return;
    }

    dbContext.Vehicles.Add(Vehicle.Create(
      vehicleId,
      DemoOrganizationId,
      resident.Id,
      property.Id,
      contract.Id,
      "BRA2E26",
      VehicleType.Car,
      "Prata",
      "Honda",
      "Civic",
      2021,
      VehicleAuthorizationStatus.Authorized,
      "A-1201",
      "Vaga coberta vinculada ao apartamento.",
      "Veiculo autorizado para demonstracao de garagem.",
      resident.FullName,
      property.Name,
      "Contrato Apartamento Aurora 1201",
      DemoNow.AddDays(-40),
      StaffUserId));
  }

  private static async Task<Occurrence> EnsureOccurrenceAsync(
    AlsappanDbContext dbContext,
    RentalProperty property,
    Resident resident,
    LeaseContract contract,
    DemoDocuments documents,
    CancellationToken cancellationToken)
  {
    var occurrenceId = EntityId("occurrence:kitchen-leak");
    var existing = await dbContext.Occurrences
      .IgnoreQueryFilters()
      .Include(occurrence => occurrence.Comments)
      .Include(occurrence => occurrence.Attachments)
      .Include(occurrence => occurrence.StatusHistory)
      .Include(occurrence => occurrence.PriorityHistory)
      .Include(occurrence => occurrence.AssignmentHistory)
      .FirstOrDefaultAsync(occurrence => occurrence.Id == occurrenceId, cancellationToken)
      .ConfigureAwait(false);

    if (existing is not null)
    {
      return existing;
    }

    var occurrence = Occurrence.Create(
      occurrenceId,
      DemoOrganizationId,
      "Vazamento na cozinha",
      "Moradora relatou vazamento recorrente proximo a pia.",
      OccurrenceType.Maintenance,
      OccurrencePriority.High,
      property.Id,
      resident.Id,
      contract.Id,
      StaffUserId,
      new DateOnly(2026, 6, 30),
      property.Name,
      resident.FullName,
      "Contrato Apartamento Aurora 1201",
      "Bruno Operador",
      DemoNow.AddDays(-2),
      ResidentUserId);
    occurrence.AddComment("Equipe acionada para vistoria tecnica.", true, DemoNow.AddDays(-2).AddHours(3), StaffUserId);
    occurrence.LinkDocument(documents.OccurrencePhoto.Id, "Foto enviada pela moradora", DemoNow.AddDays(-2).AddHours(4), ResidentUserId);
    occurrence.ChangeStatus(OccurrenceStatus.InProgress, "Atendimento agendado com prestador.", DemoNow.AddDays(-1), StaffUserId);
    dbContext.Occurrences.Add(occurrence);
    return occurrence;
  }

  private static async Task<Inspection> EnsureInspectionAsync(
    AlsappanDbContext dbContext,
    RentalProperty property,
    Resident resident,
    LeaseContract contract,
    DemoDocuments documents,
    CancellationToken cancellationToken)
  {
    var inspectionId = EntityId("inspection:aurora-periodic");
    var existing = await dbContext.Inspections
      .IgnoreQueryFilters()
      .Include(inspection => inspection.ChecklistItems)
      .Include(inspection => inspection.DocumentLinks)
      .Include(inspection => inspection.SignatureSlots)
      .FirstOrDefaultAsync(inspection => inspection.Id == inspectionId, cancellationToken)
      .ConfigureAwait(false);

    if (existing is not null)
    {
      return existing;
    }

    var inspection = Inspection.Create(
      inspectionId,
      DemoOrganizationId,
      InspectionType.Periodic,
      property.Id,
      contract.Id,
      resident.Id,
      DemoNow.AddDays(-1),
      StaffUserId,
      "Bruno Operador",
      "Vistoria periodica - Apartamento Aurora 1201",
      "Vistoria realizada antes da renovacao semestral.",
      property.Name,
      "Contrato Apartamento Aurora 1201",
      resident.FullName,
      DemoNow.AddDays(-8),
      StaffUserId);
    inspection.AddChecklistItem(
      "Cozinha",
      "Pia e gabinete",
      true,
      InspectionConditionRating.Attention,
      "Umidade identificada proxima ao sifao; ocorrencia aberta.",
      1,
      DemoNow.AddDays(-1).AddHours(1),
      StaffUserId);
    inspection.AddChecklistItem(
      "Sala",
      "Piso e pintura",
      true,
      InspectionConditionRating.Good,
      "Ambiente em bom estado geral.",
      2,
      DemoNow.AddDays(-1).AddHours(1),
      StaffUserId);
    inspection.LinkDocument(documents.InspectionReport.Id, InspectionDocumentKind.Report, null, "Relatorio", DemoNow.AddDays(-1).AddHours(2), StaffUserId);
    inspection.ReplaceSignatureSlots(
      [
        new InspectionSignatureSlotDraft("Morador", resident.FullName, true),
        new InspectionSignatureSlotDraft("Responsavel", "Bruno Operador", true)
      ],
      DemoNow.AddDays(-1).AddHours(2),
      StaffUserId);

    foreach (var slot in inspection.SignatureSlots)
    {
      slot.Sign(
        slot.SignerName ?? "Assinante",
        null,
        "Assinatura coletada na vistoria demo.",
        DemoNow.AddDays(-1).AddHours(3),
        StaffUserId);
    }

    inspection.Start(DemoNow.AddDays(-1).AddHours(1), StaffUserId);
    inspection.Complete(
      "Vistoria concluida com uma observacao convertida em ocorrencia.",
      DemoNow.AddDays(-1).AddHours(4),
      StaffUserId);
    dbContext.Inspections.Add(inspection);
    return inspection;
  }

  private static async Task EnsureTimelineNotificationsAndAuditAsync(
    AlsappanDbContext dbContext,
    RentalProperty propertyAurora,
    RentalProperty propertySerena,
    Resident resident,
    LeaseContract contract,
    Occurrence occurrence,
    Inspection inspection,
    CancellationToken cancellationToken)
  {
    var events = new[]
    {
      DemoEvent(
        "property-aurora-created",
        "properties",
        "property.created",
        DemoNow.AddDays(-180),
        EntityReference.FromGuid("property", propertyAurora.Id.Value, propertyAurora.Name),
        new Dictionary<string, string> { ["status"] = "rented", ["type"] = "apartment" }),
      DemoEvent(
        "property-serena-maintenance",
        "properties",
        "property.status-changed",
        DemoNow.AddDays(-20),
        EntityReference.FromGuid("property", propertySerena.Id.Value, propertySerena.Name),
        new Dictionary<string, string> { ["status"] = "maintenance" }),
      DemoEvent(
        "contract-aurora-activated",
        "contracts",
        "contract.activated",
        DemoNow.AddDays(-169),
        EntityReference.FromGuid("contract", contract.Id.Value, "Contrato Apartamento Aurora 1201"),
        new Dictionary<string, string> { ["status"] = "active", ["rent"] = "3200.00" },
        [
          EntityReference.FromGuid("property", propertyAurora.Id.Value, propertyAurora.Name),
          EntityReference.FromGuid("resident", resident.Id.Value, resident.FullName)
        ]),
      DemoEvent(
        "occurrence-kitchen-leak-assigned",
        "occurrences",
        "occurrence.assigned",
        DemoNow.AddDays(-2),
        EntityReference.FromGuid("occurrence", occurrence.Id.Value, occurrence.Title),
        new Dictionary<string, string> { ["priority"] = "high", ["status"] = "in-progress" },
        [
          EntityReference.FromGuid("property", propertyAurora.Id.Value, propertyAurora.Name),
          EntityReference.FromGuid("resident", resident.Id.Value, resident.FullName)
        ]),
      DemoEvent(
        "inspection-aurora-completed",
        "inspections",
        "inspection.completed",
        DemoNow.AddDays(-1),
        EntityReference.FromGuid("inspection", inspection.Id.Value, inspection.Title),
        new Dictionary<string, string> { ["status"] = "completed", ["condition"] = "attention" },
        [
          EntityReference.FromGuid("property", propertyAurora.Id.Value, propertyAurora.Name),
          EntityReference.FromGuid("occurrence", occurrence.Id.Value, occurrence.Title)
        ])
    };

    foreach (var envelope in events)
    {
      if (!await dbContext.TimelineEntries
        .IgnoreQueryFilters()
        .AnyAsync(entry => entry.EventId == envelope.EventId, cancellationToken)
        .ConfigureAwait(false))
      {
        dbContext.TimelineEntries.Add(TimelineEntry.FromEnvelope(envelope, envelope.OccurredAt.AddSeconds(1)));
      }

      if (!await dbContext.NotificationRecords
        .IgnoreQueryFilters()
        .AnyAsync(
          notification => notification.EventId == envelope.EventId && notification.RecipientUserId == AdminUserId,
          cancellationToken)
        .ConfigureAwait(false))
      {
        dbContext.NotificationRecords.Add(NotificationRecord.FromEnvelope(
          envelope,
          envelope.OccurredAt.AddSeconds(2),
          AdminUserId));
      }

      if (!await dbContext.AuditLogEntries
        .IgnoreQueryFilters()
        .AnyAsync(
          entry => entry.CorrelationId == envelope.CorrelationId &&
            entry.Action == envelope.EventName &&
            entry.TargetEntityId == envelope.Subject.EntityId,
          cancellationToken)
        .ConfigureAwait(false))
      {
        dbContext.AuditLogEntries.Add(AuditLogEntry.FromDraft(AuditEntryDraft.FromModuleEvent(envelope)));
      }
    }

    await EnsureResidentNotificationAsync(dbContext, contract, cancellationToken).ConfigureAwait(false);
    await EnsureSecurityAuditAsync(dbContext, cancellationToken).ConfigureAwait(false);
  }

  private static async Task EnsureResidentNotificationAsync(
    AlsappanDbContext dbContext,
    LeaseContract contract,
    CancellationToken cancellationToken)
  {
    var envelope = DemoEvent(
      "payment-july-open-resident",
      "payments",
      "payment.instruction-issued",
      DemoNow.AddDays(-2),
      EntityReference.FromGuid("payment", EntityId("payment:rent-july-2026").Value, "Aluguel - Julho 2026"),
      new Dictionary<string, string> { ["provider"] = PaymentCatalog.MockBoletoProvider, ["dueDate"] = "2026-07-10" },
      [EntityReference.FromGuid("contract", contract.Id.Value, "Contrato Apartamento Aurora 1201")],
      ModuleEventConsumer.Timeline | ModuleEventConsumer.Notifications);

    if (!await dbContext.NotificationRecords
      .IgnoreQueryFilters()
      .AnyAsync(
        notification => notification.EventId == envelope.EventId && notification.RecipientUserId == ResidentUserId,
        cancellationToken)
      .ConfigureAwait(false))
    {
      dbContext.NotificationRecords.Add(NotificationRecord.FromEnvelope(
        envelope,
        envelope.OccurredAt.AddSeconds(2),
        ResidentUserId));
    }

    if (!await dbContext.TimelineEntries
      .IgnoreQueryFilters()
      .AnyAsync(entry => entry.EventId == envelope.EventId, cancellationToken)
      .ConfigureAwait(false))
    {
      dbContext.TimelineEntries.Add(TimelineEntry.FromEnvelope(envelope, envelope.OccurredAt.AddSeconds(1)));
    }
  }

  private static async Task EnsureSecurityAuditAsync(
    AlsappanDbContext dbContext,
    CancellationToken cancellationToken)
  {
    const string correlationId = "demo-seed:identity-login";
    if (await dbContext.AuditLogEntries
      .IgnoreQueryFilters()
      .AnyAsync(entry => entry.CorrelationId == correlationId, cancellationToken)
      .ConfigureAwait(false))
    {
      return;
    }

    dbContext.AuditLogEntries.Add(AuditLogEntry.FromDraft(new AuditEntryDraft(
      DemoOrganizationId,
      "identity.login",
      AuditEntryCategory.Security,
      EventActor.User(AdminUserId, "Administrador Alsappan"),
      EntityReference.FromGuid("identityUser", AdminUserId.Value, "Administrador Alsappan"),
      DemoNow.AddHours(-2),
      new Dictionary<string, string> { ["result"] = "success" },
      new Dictionary<string, string> { ["source"] = "demo-seed", ["transport"] = "password" },
      correlationId)));
  }

  private static ModuleEventEnvelope DemoEvent(
    string key,
    string moduleName,
    string eventName,
    DateTimeOffset occurredAt,
    EntityReference subject,
    IReadOnlyDictionary<string, string> data,
    IReadOnlyCollection<EntityReference>? relatedEntities = null,
    ModuleEventConsumer consumers = ModuleEventConsumer.Audit |
      ModuleEventConsumer.Timeline |
      ModuleEventConsumer.Notifications) =>
    new(
      GuidFor($"event:{key}"),
      DemoOrganizationId,
      moduleName,
      eventName,
      occurredAt,
      EventActor.User(StaffUserId, "Bruno Operador"),
      subject,
      consumers,
      data,
      relatedEntities,
      $"demo-seed:{key}",
      locale: Locale);

  internal static bool IsEnabled(IConfiguration? configuration, string? environmentName)
  {
    if (configuration is null || string.IsNullOrWhiteSpace(environmentName))
    {
      return false;
    }

    return bool.TryParse(configuration[EnableDemoDataPath], out var enabled) &&
      enabled &&
      IsAllowedDemoEnvironment(environmentName);
  }

  private void EnsureEnabled()
  {
    if (IsEnabled(_seedingOptions, _environment))
    {
      return;
    }

    throw new InvalidOperationException(
      $"Demo seed data is disabled. Set {EnableDemoDataPath}=true only in Development, Local, or Demo environments.");
  }

  private static bool IsEnabled(SeedingOptions seedingOptions, DemoSeedEnvironment environment) =>
    seedingOptions.EnableDemoData && IsAllowedDemoEnvironment(environment.EnvironmentName);

  private static bool IsAllowedDemoEnvironment(string? environmentName)
  {
    if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) ||
      string.Equals(environmentName, "Local", StringComparison.OrdinalIgnoreCase) ||
      string.Equals(environmentName, "Demo", StringComparison.OrdinalIgnoreCase);
  }

  private static async Task<PaymentInstructionDto> CreateInstructionAsync(
    IPaymentInstructionProvider provider,
    PaymentCharge charge,
    string payerSummary,
    CancellationToken cancellationToken)
  {
    var request = new PaymentProviderInstructionRequest(
      charge.Id.Value,
      provider.ProviderCode,
      provider.Kind,
      new PaymentMoneyDto(charge.GrossAmount().Amount, charge.GrossAmount().Currency),
      charge.DueDate,
      payerSummary,
      Locale);

    return await provider.CreateInstructionAsync(request, cancellationToken).ConfigureAwait(false);
  }

  private static string SerializeInstruction(PaymentInstructionDto instruction) =>
    JsonSerializer.Serialize(instruction, JsonOptions);

  private static EntityId EntityId(string value) => new(GuidFor(value));

  private static UserId UserId(string value) => new(GuidFor($"user:{value}"));

  private static Guid GuidFor(string value)
  {
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return new Guid(hash[..16]);
  }

  private sealed record DemoDocuments(
    DocumentRecord SignedContract,
    DocumentRecord PaymentReceipt,
    DocumentRecord UtilityBill,
    DocumentRecord PetVaccination,
    DocumentRecord OccurrencePhoto,
    DocumentRecord InspectionReport);
}
