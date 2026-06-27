using Alsappan.Application.Pets;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Pets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Pets;

public sealed class PetConfiguration : IEntityTypeConfiguration<Pet>
{
  public void Configure(EntityTypeBuilder<Pet> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("pets");
    builder.HasKey(pet => pet.Id);

    builder.Property(pet => pet.OrganizationId)
      .IsRequired();
    builder.Property(pet => pet.ResidentId)
      .IsRequired();
    builder.Property(pet => pet.PropertyId);
    builder.Property(pet => pet.ContractId);
    builder.Property(pet => pet.Name)
      .HasMaxLength(160)
      .IsRequired();
    builder.Property(pet => pet.Species)
      .HasConversion(
        species => PetCatalog.ToSpeciesLabel(species).Code,
        code => ParseSpecies(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(pet => pet.Breed)
      .HasMaxLength(120);
    builder.Property(pet => pet.AuthorizationStatus)
      .HasConversion(
        status => PetCatalog.ToAuthorizationStatusLabel(status).Code,
        code => ParseAuthorizationStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(pet => pet.AuthorizationNotes)
      .HasMaxLength(1000);
    builder.Property(pet => pet.Notes)
      .HasMaxLength(2000);
    builder.Property(pet => pet.SearchText)
      .HasMaxLength(4000)
      .IsRequired();
    builder.Property(pet => pet.CreatedAt)
      .IsRequired();
    builder.Property(pet => pet.CreatedByUserId);
    builder.Property(pet => pet.UpdatedAt);
    builder.Property(pet => pet.UpdatedByUserId);
    builder.Property(pet => pet.DeletedAt);
    builder.Property(pet => pet.DeletedByUserId);
    builder.Property(pet => pet.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.HasMany(pet => pet.DocumentLinks)
      .WithOne()
      .HasForeignKey(link => link.PetId)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(pet => pet.DocumentLinks)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(pet => new { pet.OrganizationId, pet.ResidentId });
    builder.HasIndex(pet => new { pet.OrganizationId, pet.PropertyId });
    builder.HasIndex(pet => new { pet.OrganizationId, pet.ContractId });
    builder.HasIndex(pet => new { pet.OrganizationId, pet.Species });
    builder.HasIndex(pet => new { pet.OrganizationId, pet.AuthorizationStatus });
    builder.HasIndex(pet => new { pet.OrganizationId, pet.SearchText });
    builder.HasIndex(pet => new { pet.OrganizationId, pet.DeletedAt });
  }

  private static PetSpecies ParseSpecies(string code) =>
    PetCatalog.TryParseSpecies(code, out var species) ? species : PetSpecies.Other;

  private static PetAuthorizationStatus ParseAuthorizationStatus(string code) =>
    PetCatalog.TryParseAuthorizationStatus(code, out var status)
      ? status
      : PetAuthorizationStatus.Pending;
}
