using Alsappan.Application.Properties;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Properties;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<RentalProperty>
{
  public void Configure(EntityTypeBuilder<RentalProperty> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("properties");
    builder.HasKey(property => property.Id);

    builder.Property(property => property.OrganizationId)
      .IsRequired();
    builder.Property(property => property.Name)
      .HasMaxLength(160)
      .IsRequired();
    builder.Property(property => property.Description)
      .HasMaxLength(500);
    builder.Property(property => property.Type)
      .HasConversion(
        type => PropertyCatalog.ToTypeCode(type),
        code => ParseType(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(property => property.Status)
      .HasConversion(
        status => PropertyCatalog.ToStatusCode(status),
        code => ParseStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(property => property.GarageSpaceCount)
      .IsRequired();
    builder.Property(property => property.GarageSpaceIdentifiers)
      .HasMaxLength(250);
    builder.Property(property => property.Notes)
      .HasMaxLength(2000);
    builder.Property(property => property.SearchText)
      .HasMaxLength(1500)
      .IsRequired();
    builder.Property(property => property.CreatedAt)
      .IsRequired();
    builder.Property(property => property.CreatedByUserId);
    builder.Property(property => property.UpdatedAt);
    builder.Property(property => property.UpdatedByUserId);
    builder.Property(property => property.DeletedAt);
    builder.Property(property => property.DeletedByUserId);
    builder.Property(property => property.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.Ignore(property => property.HasGarage);

    builder.OwnsOne(property => property.Address, address =>
    {
      address.Property<EntityId>("RentalPropertyId")
        .HasColumnName("id");
      address.Property(value => value.StreetLine)
        .HasColumnName("street_line")
        .HasMaxLength(160)
        .IsRequired();
      address.Property(value => value.Number)
        .HasColumnName("street_number")
        .HasMaxLength(40)
        .IsRequired();
      address.Property(value => value.Complement)
        .HasColumnName("address_complement")
        .HasMaxLength(120);
      address.Property(value => value.Neighborhood)
        .HasColumnName("neighborhood")
        .HasMaxLength(120)
        .IsRequired();
      address.Property(value => value.City)
        .HasColumnName("city")
        .HasMaxLength(120)
        .IsRequired();
      address.Property(value => value.StateCode)
        .HasColumnName("state_code")
        .HasMaxLength(2)
        .IsRequired();
      address.Property(value => value.PostalCode)
        .HasColumnName("postal_code")
        .HasMaxLength(20);
      address.Property(value => value.CountryCode)
        .HasColumnName("country_code")
        .HasMaxLength(2)
        .IsRequired();
    });

    builder.OwnsOne(property => property.SuggestedRent, money =>
    {
      money.Property<EntityId>("RentalPropertyId")
        .HasColumnName("id");
      money.Property(value => value.Amount)
        .HasColumnName("suggested_rent_amount")
        .HasPrecision(18, 2)
        .IsRequired();
      money.Property(value => value.Currency)
        .HasColumnName("suggested_rent_currency")
        .HasMaxLength(3)
        .IsRequired();
    });

    builder.HasIndex(property => new { property.OrganizationId, property.Name });
    builder.HasIndex(property => new { property.OrganizationId, property.Status });
    builder.HasIndex(property => new { property.OrganizationId, property.Type });
    builder.HasIndex(property => new { property.OrganizationId, property.GarageSpaceCount });
    builder.HasIndex(property => new { property.OrganizationId, property.SearchText });
    builder.HasIndex(property => new { property.OrganizationId, property.DeletedAt });
  }

  private static PropertyType ParseType(string code) =>
    PropertyCatalog.TryParseType(code, out var type) ? type : PropertyType.Other;

  private static PropertyStatus ParseStatus(string code) =>
    PropertyCatalog.TryParseStatus(code, out var status) ? status : PropertyStatus.Inactive;
}
