using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Outbox;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
  public void Configure(EntityTypeBuilder<OutboxMessage> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("OutboxMessages");
    builder.HasKey(message => message.Id);

    builder.Property(message => message.ModuleName).HasMaxLength(96).IsRequired();
    builder.Property(message => message.EventName).HasMaxLength(160).IsRequired();
    builder.Property(message => message.PayloadJson).HasColumnType("jsonb").IsRequired();
    builder.Property(message => message.CorrelationId).HasMaxLength(128);
    builder.Property(message => message.CausationId).HasMaxLength(128);
    builder.Property(message => message.LastError).HasMaxLength(1_000);
    builder.Ignore(message => message.IsProcessed);

    builder.HasIndex(message => message.EventId).IsUnique();
    builder.HasIndex(message => new { message.OrganizationId, message.ProcessedAt, message.EnqueuedAt });
    builder.HasIndex(message => new { message.OrganizationId, message.ModuleName, message.EventName });
  }
}
