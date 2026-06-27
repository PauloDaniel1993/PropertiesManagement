using Alsappan.Application.Payments;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Payments;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
  public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("payment_transactions");
    builder.HasKey(transaction => transaction.Id);
    builder.Property(transaction => transaction.OrganizationId)
      .IsRequired();
    builder.Property(transaction => transaction.ChargeId)
      .IsRequired();
    builder.Property(transaction => transaction.Method)
      .HasConversion(
        method => PaymentCatalog.ToMethodCode(method),
        code => ParseMethod(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(transaction => transaction.SettledOn)
      .IsRequired();
    builder.Property(transaction => transaction.BankReference)
      .HasMaxLength(160);
    builder.Property(transaction => transaction.ProviderCode)
      .HasMaxLength(80);
    builder.Property(transaction => transaction.ProviderReference)
      .HasMaxLength(160);
    builder.Property(transaction => transaction.ReceiptDocumentId);
    builder.Property(transaction => transaction.Notes)
      .HasMaxLength(1000);
    builder.Property(transaction => transaction.Status)
      .HasConversion(
        status => status == PaymentTransactionStatus.Reversed ? "reversed" : "active",
        code => string.Equals(code, "reversed", StringComparison.Ordinal)
          ? PaymentTransactionStatus.Reversed
          : PaymentTransactionStatus.Active)
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(transaction => transaction.CreatedAt)
      .IsRequired();
    builder.Property(transaction => transaction.CreatedByUserId);
    builder.Property(transaction => transaction.UpdatedAt);
    builder.Property(transaction => transaction.UpdatedByUserId);
    builder.Property(transaction => transaction.DeletedAt);
    builder.Property(transaction => transaction.DeletedByUserId);
    builder.Property(transaction => transaction.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.OwnsOne(transaction => transaction.Amount, money =>
    {
      money.Property<EntityId>("PaymentTransactionId")
        .HasColumnName("id");
      money.Property(value => value.Amount)
        .HasColumnName("amount")
        .HasPrecision(18, 2)
        .IsRequired();
      money.Property(value => value.Currency)
        .HasColumnName("currency")
        .HasMaxLength(3)
        .IsRequired();
    });

    builder.HasIndex(transaction => new { transaction.OrganizationId, transaction.ChargeId });
    builder.HasIndex(transaction => new { transaction.OrganizationId, transaction.SettledOn });
    builder.HasIndex(transaction => new { transaction.OrganizationId, transaction.ProviderCode, transaction.ProviderReference });
    builder.HasIndex(transaction => new { transaction.OrganizationId, transaction.DeletedAt });
  }

  private static PaymentMethod ParseMethod(string code) =>
    PaymentCatalog.TryParseMethod(code, out var method) ? method : PaymentMethod.Other;
}
