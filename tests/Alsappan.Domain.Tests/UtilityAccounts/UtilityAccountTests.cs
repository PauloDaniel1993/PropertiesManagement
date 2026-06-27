using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.UtilityAccounts;

namespace Alsappan.Domain.Tests.UtilityAccounts;

public sealed class UtilityAccountTests
{
  [Fact]
  public void MarkPaidSettlesAccountAndLinksReceipt()
  {
    var account = CreateAccount();
    var receiptId = EntityId.New();
    var now = DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    account.MarkPaid(
      new Money(300m, "BRL"),
      new DateOnly(2026, 6, 27),
      "Pix",
      "PIX-1",
      receiptId,
      "Pagamento recebido",
      now,
      null);

    Assert.Equal(UtilityAccountStatus.Paid, account.Status);
    Assert.Equal(0m, account.Balance().Amount);
    var receipt = Assert.Single(account.DocumentLinks);
    Assert.Equal(receiptId, receipt.DocumentId);
    Assert.Equal(UtilityDocumentKind.Receipt, receipt.Kind);
  }

  [Fact]
  public void EffectiveStatusMarksPastDueOpenAccountAsOverdue()
  {
    var account = CreateAccount(dueDate: new DateOnly(2026, 6, 20));

    Assert.Equal(UtilityAccountStatus.Overdue, account.GetEffectiveStatus(new DateOnly(2026, 6, 27)));
  }

  [Fact]
  public void ArchiveAndRestoreKeepsLifecycleConsistent()
  {
    var account = CreateAccount();
    var now = DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    account.Archive(now, null);

    Assert.True(account.IsDeleted);
    Assert.Equal(UtilityAccountStatus.Archived, account.GetEffectiveStatus(new DateOnly(2026, 6, 27)));

    account.Restore(now.AddMinutes(1), null);

    Assert.False(account.IsDeleted);
    Assert.Equal(UtilityAccountStatus.Open, account.Status);
  }

  private static UtilityAccount CreateAccount(DateOnly? dueDate = null) =>
    UtilityAccount.Create(
      EntityId.New(),
      OrganizationId.New(),
      EntityId.New(),
      EntityId.New(),
      EntityId.New(),
      UtilityAccountType.Electricity,
      UtilityResponsibility.Contract,
      "Energia junho",
      "Conta de energia",
      new DateOnly(2026, 6, 1),
      new DateOnly(2026, 6, 30),
      dueDate ?? new DateOnly(2026, 6, 30),
      new Money(300m, "BRL"),
      null,
      "Casa Calabria",
      "Contrato Casa Calabria",
      "Joao da Silva",
      DateTimeOffset.Parse("2026-06-01T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
