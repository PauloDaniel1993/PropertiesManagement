using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Payments;

namespace Alsappan.Domain.Tests.Payments;

public sealed class PaymentChargeTests
{
  [Fact]
  public void RecordTransactionPartiallyAndFullySettlesCharge()
  {
    var charge = CreateCharge();
    var now = DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    charge.RecordTransaction(
      EntityId.New(),
      new Money(400m, "BRL"),
      PaymentMethod.Pix,
      new DateOnly(2026, 6, 27),
      "PIX-1",
      "mock-pix",
      "provider-1",
      null,
      "Pagamento parcial",
      now,
      null);

    Assert.Equal(PaymentStatus.PartiallyPaid, charge.Status);
    Assert.Equal(400m, charge.SettledAmount().Amount);
    Assert.Equal(600m, charge.Balance().Amount);

    charge.RecordTransaction(
      EntityId.New(),
      new Money(600m, "BRL"),
      PaymentMethod.Pix,
      new DateOnly(2026, 6, 28),
      "PIX-2",
      "mock-pix",
      "provider-2",
      null,
      null,
      now.AddMinutes(1),
      null);

    Assert.Equal(PaymentStatus.Paid, charge.Status);
    Assert.Equal(0m, charge.Balance().Amount);
  }

  [Fact]
  public void RecordTransactionRejectsOverSettlement()
  {
    var charge = CreateCharge();

    Assert.Throws<ArgumentOutOfRangeException>(() => charge.RecordTransaction(
      EntityId.New(),
      new Money(1000.01m, "BRL"),
      PaymentMethod.BankTransfer,
      new DateOnly(2026, 6, 27),
      null,
      null,
      null,
      null,
      null,
      DateTimeOffset.UtcNow,
      null));
  }

  [Fact]
  public void EffectiveStatusMarksUnpaidPastDueChargeAsOverdue()
  {
    var charge = CreateCharge(dueDate: new DateOnly(2026, 6, 20));

    Assert.Equal(PaymentStatus.Overdue, charge.GetEffectiveStatus(new DateOnly(2026, 6, 27)));
  }

  [Fact]
  public void ReversingTransactionReopensBalanceAndArchiveRestoreKeepsLifecycleConsistent()
  {
    var charge = CreateCharge();
    var now = DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    var transaction = charge.RecordTransaction(
      EntityId.New(),
      new Money(1000m, "BRL"),
      PaymentMethod.Boleto,
      new DateOnly(2026, 6, 27),
      null,
      "mock-boleto",
      "provider-1",
      EntityId.New(),
      null,
      now,
      null);

    charge.ReverseTransaction(transaction.Id, now.AddMinutes(1), null, "Estorno");

    Assert.Equal(PaymentStatus.Pending, charge.Status);
    Assert.Equal(1000m, charge.Balance().Amount);
    Assert.Single(charge.ReceiptLinks);

    charge.Archive(now.AddMinutes(2), null);
    Assert.True(charge.IsDeleted);
    Assert.Equal(PaymentStatus.Archived, charge.GetEffectiveStatus(new DateOnly(2026, 6, 27)));

    charge.Restore(now.AddMinutes(3), null);
    Assert.False(charge.IsDeleted);
    Assert.Equal(PaymentStatus.Pending, charge.Status);
  }

  private static PaymentCharge CreateCharge(DateOnly? dueDate = null) =>
    PaymentCharge.Create(
      EntityId.New(),
      OrganizationId.New(),
      EntityId.New(),
      EntityId.New(),
      EntityId.New(),
      null,
      "Aluguel junho",
      "Mensalidade",
      dueDate ?? new DateOnly(2026, 6, 30),
      new Money(1000m, "BRL"),
      null,
      null,
      PaymentMethod.Pix,
      PaymentReconciliationStatus.Pending,
      "Observacoes",
      "Contrato Casa",
      "Casa Calabria",
      "Joao da Silva",
      DateTimeOffset.Parse("2026-06-01T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
