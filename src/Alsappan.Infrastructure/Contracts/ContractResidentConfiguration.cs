using Alsappan.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Contracts;

public sealed class ContractResidentConfiguration : IEntityTypeConfiguration<ContractResident>
{
  public void Configure(EntityTypeBuilder<ContractResident> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("contract_residents");
    builder.HasKey(contractResident => contractResident.Id);

    builder.Property(contractResident => contractResident.OrganizationId)
      .IsRequired();
    builder.Property(contractResident => contractResident.ContractId)
      .IsRequired();
    builder.Property(contractResident => contractResident.ResidentId)
      .IsRequired();
    builder.Property(contractResident => contractResident.IsPrimary)
      .IsRequired();
    builder.Property(contractResident => contractResident.CreatedAt)
      .IsRequired();
    builder.Property(contractResident => contractResident.CreatedByUserId);
    builder.Property(contractResident => contractResident.UpdatedAt);
    builder.Property(contractResident => contractResident.UpdatedByUserId);
    builder.Property(contractResident => contractResident.DeletedAt);
    builder.Property(contractResident => contractResident.DeletedByUserId);
    builder.Property(contractResident => contractResident.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasIndex(contractResident => new { contractResident.OrganizationId, contractResident.ContractId });
    builder.HasIndex(contractResident => new { contractResident.OrganizationId, contractResident.ResidentId });
    builder.HasIndex(contractResident => new { contractResident.OrganizationId, contractResident.ResidentId, contractResident.ContractId })
      .IsUnique();
    builder.HasIndex(contractResident => new { contractResident.OrganizationId, contractResident.IsPrimary });
    builder.HasIndex(contractResident => new { contractResident.OrganizationId, contractResident.DeletedAt });
  }
}
