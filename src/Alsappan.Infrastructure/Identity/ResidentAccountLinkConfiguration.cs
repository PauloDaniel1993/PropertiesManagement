using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Identity;

public sealed class ResidentAccountLinkConfiguration : IEntityTypeConfiguration<ResidentAccountLink>
{
  public void Configure(EntityTypeBuilder<ResidentAccountLink> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("ResidentAccountLinks");
    builder.HasKey(link => link.Id);

    builder.Property(link => link.Id).HasEntityIdConversion();
    builder.Property(link => link.OrganizationId).HasOrganizationIdConversion();
    builder.Property(link => link.UserId).HasUserIdConversion();
    builder.Property(link => link.ResidentId).HasEntityIdConversion();
    builder.Property(link => link.CreatedByUserId).HasNullableUserIdConversion();
    builder.Property(link => link.UpdatedByUserId).HasNullableUserIdConversion();
    builder.Property(link => link.DeletedByUserId).HasNullableUserIdConversion();
    builder.Property(link => link.ConcurrencyToken)
      .HasConversion(token => token.Value, value => new ConcurrencyToken(value))
      .HasMaxLength(64)
      .IsRequired();
    builder.Ignore(link => link.IsDeleted);

    builder.HasIndex(link => new { link.OrganizationId, link.UserId });
    builder.HasIndex(link => new { link.OrganizationId, link.ResidentId }).IsUnique();
  }
}
