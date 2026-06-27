using Alsappan.Application.Payments;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Payments;

public sealed class PaymentChargeConfiguration : IEntityTypeConfiguration<PaymentCharge>
{
  public void Configure(EntityTypeBuilder<PaymentCharge> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("payment_charges");
    builder.HasKey(charge => charge.Id);

    builder.Property(charge => charge.OrganizationId)
      .IsRequired();
    builder.Property(charge => charge.ContractId);
    builder.Property(charge => charge.PropertyId);
    builder.Property(charge => charge.ResidentId);
    builder.Property(charge => charge.UtilityAccountId);
    builder.Property(charge => charge.Title)
      .HasMaxLength(200)
      .IsRequired();
    builder.Property(charge => charge.Description)
      .HasMaxLength(1000);
    builder.Property(charge => charge.DueDate)
      .IsRequired();
    builder.Property(charge => charge.PreferredMethod)
      .HasConversion(
        method => PaymentCatalog.ToMethodCode(method),
        code => ParseMethod(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(charge => charge.Status)
      .HasConversion(
        status => PaymentCatalog.ToStatusCode(status),
        code => ParseStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(charge => charge.ReconciliationStatus)
      .HasConversion(
        status => PaymentCatalog.ToReconciliationStatusCode(status),
        code => ParseReconciliationStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(charge => charge.ProviderCode)
      .HasMaxLength(80);
    builder.Property(charge => charge.ProviderReference)
      .HasMaxLength(160);
    builder.Property(charge => charge.ProviderMetadataJson)
      .HasMaxLength(8000);
    builder.Property(charge => charge.Notes)
      .HasMaxLength(1000);
    builder.Property(charge => charge.SearchText)
      .HasMaxLength(PaymentCode.MaxSearchTextLength)
      .IsRequired();
    builder.Property(charge => charge.CreatedAt)
      .IsRequired();
    builder.Property(charge => charge.CreatedByUserId);
    builder.Property(charge => charge.UpdatedAt);
    builder.Property(charge => charge.UpdatedByUserId);
    builder.Property(charge => charge.DeletedAt);
    builder.Property(charge => charge.DeletedByUserId);
    builder.Property(charge => charge.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.OwnsOne(charge => charge.Amount, money =>
    {
      money.Property<EntityId>("PaymentChargeId")
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

    builder.OwnsOne(charge => charge.DiscountAmount, money =>
    {
      money.Property<EntityId>("PaymentChargeId")
        .HasColumnName("id");
      money.Property(value => value.Amount)
        .HasColumnName("discount_amount")
        .HasPrecision(18, 2)
        .IsRequired();
      money.Property(value => value.Currency)
        .HasColumnName("discount_currency")
        .HasMaxLength(3)
        .IsRequired();
    });

    builder.OwnsOne(charge => charge.PenaltyAmount, money =>
    {
      money.Property<EntityId>("PaymentChargeId")
        .HasColumnName("id");
      money.Property(value => value.Amount)
        .HasColumnName("penalty_amount")
        .HasPrecision(18, 2)
        .IsRequired();
      money.Property(value => value.Currency)
        .HasColumnName("penalty_currency")
        .HasMaxLength(3)
        .IsRequired();
    });

    builder.HasMany(charge => charge.Transactions)
      .WithOne()
      .HasForeignKey(transaction => transaction.ChargeId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(charge => charge.Transactions)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasMany(charge => charge.ReceiptLinks)
      .WithOne()
      .HasForeignKey(link => link.ChargeId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(charge => charge.ReceiptLinks)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(charge => new { charge.OrganizationId, charge.ContractId });
    builder.HasIndex(charge => new { charge.OrganizationId, charge.PropertyId });
    builder.HasIndex(charge => new { charge.OrganizationId, charge.ResidentId });
    builder.HasIndex(charge => new { charge.OrganizationId, charge.UtilityAccountId });
    builder.HasIndex(charge => new { charge.OrganizationId, charge.DueDate });
    builder.HasIndex(charge => new { charge.OrganizationId, charge.Status });
    builder.HasIndex(charge => new { charge.OrganizationId, charge.ProviderCode, charge.ProviderReference });
    builder.HasIndex(charge => new { charge.OrganizationId, charge.SearchText });
    builder.HasIndex(charge => new { charge.OrganizationId, charge.DeletedAt });
  }

  private static PaymentStatus ParseStatus(string code) =>
    PaymentCatalog.TryParseStatus(code, out var status) ? status : PaymentStatus.Pending;

  private static PaymentMethod ParseMethod(string code) =>
    PaymentCatalog.TryParseMethod(code, out var method) ? method : PaymentMethod.Other;

  private static PaymentReconciliationStatus ParseReconciliationStatus(string code) =>
    PaymentCatalog.TryParseReconciliationStatus(code, out var status)
      ? status
      : PaymentReconciliationStatus.NotRequired;
}
