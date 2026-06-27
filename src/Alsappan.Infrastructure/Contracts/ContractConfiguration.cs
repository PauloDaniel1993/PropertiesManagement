using Alsappan.Application.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Contracts;

public sealed class ContractConfiguration : IEntityTypeConfiguration<LeaseContract>
{
  public void Configure(EntityTypeBuilder<LeaseContract> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("contracts");
    builder.HasKey(contract => contract.Id);

    builder.Property(contract => contract.OrganizationId)
      .IsRequired();
    builder.Property(contract => contract.PropertyId)
      .IsRequired();
    builder.Property(contract => contract.PrimaryResidentId)
      .IsRequired();
    builder.Property(contract => contract.Status)
      .HasConversion(
        status => ContractCatalog.ToStatusCode(status),
        code => ParseStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(contract => contract.StartDate)
      .IsRequired();
    builder.Property(contract => contract.EndDate);
    builder.Property(contract => contract.DueDay)
      .IsRequired();
    builder.Property(contract => contract.AdjustmentIndex)
      .HasConversion(
        index => ContractCatalog.ToAdjustmentIndexCode(index),
        code => ParseAdjustmentIndex(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(contract => contract.AdjustmentIntervalMonths)
      .IsRequired();
    builder.Property(contract => contract.NextAdjustmentDate);
    builder.Property(contract => contract.PenaltyNotes)
      .HasMaxLength(1000);
    builder.Property(contract => contract.DiscountNotes)
      .HasMaxLength(1000);
    builder.Property(contract => contract.GeneratePaymentsAutomatically)
      .IsRequired();
    builder.Property(contract => contract.Notes)
      .HasMaxLength(2000);
    builder.Property(contract => contract.SearchText)
      .HasMaxLength(ContractCode.MaxSearchTextLength)
      .IsRequired();
    builder.Property(contract => contract.CreatedAt)
      .IsRequired();
    builder.Property(contract => contract.CreatedByUserId);
    builder.Property(contract => contract.UpdatedAt);
    builder.Property(contract => contract.UpdatedByUserId);
    builder.Property(contract => contract.DeletedAt);
    builder.Property(contract => contract.DeletedByUserId);
    builder.Property(contract => contract.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.OwnsOne(contract => contract.MonthlyRent, money =>
    {
      money.Property<EntityId>("LeaseContractId")
        .HasColumnName("id");
      money.Property(value => value.Amount)
        .HasColumnName("monthly_rent_amount")
        .HasPrecision(18, 2)
        .IsRequired();
      money.Property(value => value.Currency)
        .HasColumnName("monthly_rent_currency")
        .HasMaxLength(3)
        .IsRequired();
    });

    builder.OwnsOne(contract => contract.DepositAmount, money =>
    {
      money.Property<EntityId>("LeaseContractId")
        .HasColumnName("id");
      money.Property(value => value.Amount)
        .HasColumnName("deposit_amount")
        .HasPrecision(18, 2);
      money.Property(value => value.Currency)
        .HasColumnName("deposit_currency")
        .HasMaxLength(3);
    });

    builder.HasMany(contract => contract.Residents)
      .WithOne()
      .HasForeignKey(resident => resident.ContractId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(contract => new { contract.OrganizationId, contract.PropertyId });
    builder.HasIndex(contract => new { contract.OrganizationId, contract.PrimaryResidentId });
    builder.HasIndex(contract => new { contract.OrganizationId, contract.Status });
    builder.HasIndex(contract => new { contract.OrganizationId, contract.StartDate });
    builder.HasIndex(contract => new { contract.OrganizationId, contract.EndDate });
    builder.HasIndex(contract => new { contract.OrganizationId, contract.SearchText });
    builder.HasIndex(contract => new { contract.OrganizationId, contract.DeletedAt });
  }

  private static ContractStatus ParseStatus(string code) =>
    ContractCatalog.TryParseStoredStatus(code, out var status) ? status : ContractStatus.Draft;

  private static ContractAdjustmentIndex ParseAdjustmentIndex(string code) =>
    ContractCatalog.TryParseAdjustmentIndex(code, out var index) ? index : ContractAdjustmentIndex.Other;
}
