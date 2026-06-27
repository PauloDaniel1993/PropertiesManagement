using Alsappan.Application.Vehicles;
using Alsappan.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Vehicles;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
  public void Configure(EntityTypeBuilder<Vehicle> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("vehicles");
    builder.HasKey(vehicle => vehicle.Id);

    builder.Property(vehicle => vehicle.OrganizationId)
      .IsRequired();
    builder.Property(vehicle => vehicle.ResidentId)
      .IsRequired();
    builder.Property(vehicle => vehicle.PropertyId);
    builder.Property(vehicle => vehicle.ContractId);
    builder.Property(vehicle => vehicle.Plate)
      .HasMaxLength(20)
      .IsRequired();
    builder.Property(vehicle => vehicle.NormalizedPlate)
      .HasMaxLength(20)
      .IsRequired();
    builder.Property(vehicle => vehicle.Type)
      .HasConversion(
        type => VehicleCatalog.ToTypeLabel(type).Code,
        code => ParseType(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(vehicle => vehicle.Color)
      .HasMaxLength(80);
    builder.Property(vehicle => vehicle.Brand)
      .HasMaxLength(120);
    builder.Property(vehicle => vehicle.Model)
      .HasMaxLength(120);
    builder.Property(vehicle => vehicle.Year);
    builder.Property(vehicle => vehicle.AuthorizationStatus)
      .HasConversion(
        status => VehicleCatalog.ToAuthorizationStatusLabel(status).Code,
        code => ParseAuthorizationStatus(code))
      .HasMaxLength(40)
      .IsRequired();
    builder.Property(vehicle => vehicle.ParkingSpaceIdentifier)
      .HasMaxLength(80);
    builder.Property(vehicle => vehicle.NormalizedParkingSpaceIdentifier)
      .HasMaxLength(80);
    builder.Property(vehicle => vehicle.ParkingAllocationNotes)
      .HasMaxLength(500);
    builder.Property(vehicle => vehicle.Notes)
      .HasMaxLength(2000);
    builder.Property(vehicle => vehicle.SearchText)
      .HasMaxLength(4000)
      .IsRequired();
    builder.Property(vehicle => vehicle.CreatedAt)
      .IsRequired();
    builder.Property(vehicle => vehicle.CreatedByUserId);
    builder.Property(vehicle => vehicle.UpdatedAt);
    builder.Property(vehicle => vehicle.UpdatedByUserId);
    builder.Property(vehicle => vehicle.DeletedAt);
    builder.Property(vehicle => vehicle.DeletedByUserId);
    builder.Property(vehicle => vehicle.ConcurrencyToken)
      .HasMaxLength(64)
      .IsConcurrencyToken();

    builder.Ignore(vehicle => vehicle.HasParkingAllocation);

    builder.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.NormalizedPlate });
    builder.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.ResidentId });
    builder.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.PropertyId });
    builder.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.ContractId });
    builder.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.Type });
    builder.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.AuthorizationStatus });
    builder.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.PropertyId, vehicle.NormalizedParkingSpaceIdentifier });
    builder.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.SearchText });
    builder.HasIndex(vehicle => new { vehicle.OrganizationId, vehicle.DeletedAt });
  }

  private static VehicleType ParseType(string code) =>
    VehicleCatalog.TryParseType(code, out var type) ? type : VehicleType.Other;

  private static VehicleAuthorizationStatus ParseAuthorizationStatus(string code) =>
    VehicleCatalog.TryParseAuthorizationStatus(code, out var status)
      ? status
      : VehicleAuthorizationStatus.Pending;
}
