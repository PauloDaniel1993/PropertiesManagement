using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Seeding;

public sealed class SeedHistoryRecordConfiguration : IEntityTypeConfiguration<SeedHistoryRecord>
{
  public void Configure(EntityTypeBuilder<SeedHistoryRecord> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("SeedHistoryRecords");
    builder.HasKey(record => record.Id);

    builder.Property(record => record.Name).HasMaxLength(160).IsRequired();
    builder.Property(record => record.Version).HasMaxLength(64).IsRequired();

    builder.HasIndex(record => new { record.Name, record.Version }).IsUnique();
  }
}
