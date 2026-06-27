using Alsappan.Application.UtilityAccounts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.UtilityAccounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.UtilityAccounts;

public sealed class UtilityAccountConfiguration : IEntityTypeConfiguration<UtilityAccount>
{
  public void Configure(EntityTypeBuilder<UtilityAccount> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("utility_accounts");
    builder.HasKey(account => account.Id);

    builder.Property(account => account.OrganizationId)
      .IsRequired();
    builder.Property(account => account.PropertyId);
    builder.Property(account => account.ContractId);
    builder.Property(account => account.ResidentId);
    builder.Property(account => account.Type)
      .HasConversion(
        type => UtilityAccountCatalog.ToTypeLabel(type).Code,
        code => ParseType(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(account => account.Responsibility)
      .HasConversion(
        responsibility => UtilityAccountCatalog.ToResponsibilityLabel(responsibility).Code,
        code => ParseResponsibility(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(account => account.Title)
      .HasMaxLength(200)
      .IsRequired();
    builder.Property(account => account.Description)
      .HasMaxLength(1000);
    builder.Property(account => account.BillingPeriodStart)
      .IsRequired();
    builder.Property(account => account.BillingPeriodEnd)
      .IsRequired();
    builder.Property(account => account.DueDate)
      .IsRequired();
    builder.Property(account => account.PaidOn);
    builder.Property(account => account.PaymentMethod)
      .HasMaxLength(80);
    builder.Property(account => account.BankReference)
      .HasMaxLength(160);
    builder.Property(account => account.Status)
      .HasConversion(
        status => UtilityAccountCatalog.ToStatusLabel(status).Code,
        code => ParseStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(account => account.Notes)
      .HasMaxLength(1000);
    builder.Property(account => account.SearchText)
      .HasMaxLength(4000)
      .IsRequired();
    builder.Property(account => account.CreatedAt)
      .IsRequired();
    builder.Property(account => account.CreatedByUserId);
    builder.Property(account => account.UpdatedAt);
    builder.Property(account => account.UpdatedByUserId);
    builder.Property(account => account.DeletedAt);
    builder.Property(account => account.DeletedByUserId);
    builder.Property(account => account.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.OwnsOne(account => account.Amount, money =>
    {
      money.Property<EntityId>("UtilityAccountId")
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

    builder.OwnsOne(account => account.PaidAmount, money =>
    {
      money.Property<EntityId>("UtilityAccountId")
        .HasColumnName("id");
      money.Property(value => value.Amount)
        .HasColumnName("paid_amount")
        .HasPrecision(18, 2)
        .IsRequired();
      money.Property(value => value.Currency)
        .HasColumnName("paid_currency")
        .HasMaxLength(3)
        .IsRequired();
    });

    builder.HasMany(account => account.DocumentLinks)
      .WithOne()
      .HasForeignKey(link => link.UtilityAccountId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(account => account.DocumentLinks)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(account => new { account.OrganizationId, account.Type });
    builder.HasIndex(account => new { account.OrganizationId, account.Status });
    builder.HasIndex(account => new { account.OrganizationId, account.Responsibility });
    builder.HasIndex(account => new { account.OrganizationId, account.PropertyId });
    builder.HasIndex(account => new { account.OrganizationId, account.ContractId });
    builder.HasIndex(account => new { account.OrganizationId, account.ResidentId });
    builder.HasIndex(account => new { account.OrganizationId, account.BillingPeriodStart, account.BillingPeriodEnd });
    builder.HasIndex(account => new { account.OrganizationId, account.DueDate });
    builder.HasIndex(account => new { account.OrganizationId, account.SearchText });
    builder.HasIndex(account => new { account.OrganizationId, account.DeletedAt });
  }

  private static UtilityAccountType ParseType(string code) =>
    UtilityAccountCatalog.TryParseType(code, out var type) ? type : UtilityAccountType.Other;

  private static UtilityResponsibility ParseResponsibility(string code) =>
    UtilityAccountCatalog.TryParseResponsibility(code, out var responsibility)
      ? responsibility
      : UtilityResponsibility.Other;

  private static UtilityAccountStatus ParseStatus(string code) =>
    UtilityAccountCatalog.TryParseStatus(code, out var status) ? status : UtilityAccountStatus.Open;
}
