using Alsappan.Application.Residents;
using Alsappan.Domain.Residents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Residents;

public sealed class ResidentConfiguration : IEntityTypeConfiguration<Resident>
{
  public void Configure(EntityTypeBuilder<Resident> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("residents");
    builder.HasKey(resident => resident.Id);

    builder.Property(resident => resident.OrganizationId)
      .IsRequired();
    builder.Property(resident => resident.FullName)
      .HasMaxLength(180)
      .IsRequired();
    builder.Property(resident => resident.PreferredName)
      .HasMaxLength(120);
    builder.Property(resident => resident.Email)
      .HasMaxLength(320);
    builder.Property(resident => resident.NormalizedEmail)
      .HasMaxLength(320);
    builder.Property(resident => resident.Phone)
      .HasMaxLength(40);
    builder.Property(resident => resident.NormalizedPhone)
      .HasMaxLength(40);
    builder.Property(resident => resident.SecondaryPhone)
      .HasMaxLength(40);
    builder.Property(resident => resident.NormalizedSecondaryPhone)
      .HasMaxLength(40);
    builder.Property(resident => resident.DocumentType)
      .HasMaxLength(40);
    builder.Property(resident => resident.DocumentIdentifier)
      .HasMaxLength(80);
    builder.Property(resident => resident.NormalizedDocumentIdentifier)
      .HasMaxLength(80);
    builder.Property(resident => resident.EmergencyContactName)
      .HasMaxLength(160);
    builder.Property(resident => resident.EmergencyContactRelationship)
      .HasMaxLength(80);
    builder.Property(resident => resident.EmergencyContactPhone)
      .HasMaxLength(40);
    builder.Property(resident => resident.NormalizedEmergencyContactPhone)
      .HasMaxLength(40);
    builder.Property(resident => resident.Status)
      .HasConversion(
        status => ResidentCatalog.ToStatusCode(status),
        code => ParseStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(resident => resident.PortalStatus)
      .HasConversion(
        status => ResidentCatalog.ToPortalStatusCode(status),
        code => ParsePortalStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(resident => resident.PrivacyFlags)
      .IsRequired();
    builder.Property(resident => resident.Notes)
      .HasMaxLength(2000);
    builder.Property(resident => resident.LinkedUserId);
    builder.Property(resident => resident.SearchText)
      .HasMaxLength(1800)
      .IsRequired();
    builder.Property(resident => resident.CreatedAt)
      .IsRequired();
    builder.Property(resident => resident.CreatedByUserId);
    builder.Property(resident => resident.UpdatedAt);
    builder.Property(resident => resident.UpdatedByUserId);
    builder.Property(resident => resident.DeletedAt);
    builder.Property(resident => resident.DeletedByUserId);
    builder.Property(resident => resident.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.Ignore(resident => resident.HasPortalAccess);

    builder.HasIndex(resident => new { resident.OrganizationId, resident.FullName });
    builder.HasIndex(resident => new { resident.OrganizationId, resident.NormalizedEmail });
    builder.HasIndex(resident => new { resident.OrganizationId, resident.NormalizedPhone });
    builder.HasIndex(resident => new { resident.OrganizationId, resident.NormalizedDocumentIdentifier });
    builder.HasIndex(resident => new { resident.OrganizationId, resident.Status });
    builder.HasIndex(resident => new { resident.OrganizationId, resident.PortalStatus });
    builder.HasIndex(resident => new { resident.OrganizationId, resident.LinkedUserId });
    builder.HasIndex(resident => new { resident.OrganizationId, resident.SearchText });
    builder.HasIndex(resident => new { resident.OrganizationId, resident.DeletedAt });
  }

  private static ResidentStatus ParseStatus(string code) =>
    ResidentCatalog.TryParseStatus(code, out var status) ? status : ResidentStatus.Inactive;

  private static ResidentPortalStatus ParsePortalStatus(string code) =>
    ResidentCatalog.TryParsePortalStatus(code, out var status) ? status : ResidentPortalStatus.NotInvited;
}
