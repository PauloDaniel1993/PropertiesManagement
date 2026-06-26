using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Auth;

public sealed class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
  public void Configure(EntityTypeBuilder<RefreshSession> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("RefreshSessions");
    builder.HasKey(session => session.Id);

    builder.Property(session => session.Id)
      .HasConversion(id => id.Value, value => new(value));
    builder.Property(session => session.UserId)
      .HasConversion(id => id.Value, value => new(value));
    builder.Property(session => session.ActiveOrganizationId)
      .HasConversion<Guid?>(
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new(value.Value) : null);
    builder.Property(session => session.ReplacedBySessionId)
      .HasConversion<Guid?>(
        id => id.HasValue ? id.Value.Value : null,
        value => value.HasValue ? new(value.Value) : null);

    builder.Property(session => session.TokenHash).HasMaxLength(256).IsRequired();
    builder.Property(session => session.UserAgent).HasMaxLength(500);
    builder.Property(session => session.IpAddress).HasMaxLength(96);
    builder.Property(session => session.RevokedReason).HasMaxLength(240);

    builder.Ignore(session => session.IsRevoked);

    builder.HasIndex(session => session.TokenHash).IsUnique();
    builder.HasIndex(session => new { session.UserId, session.ExpiresAt });
    builder.HasIndex(session => new { session.ActiveOrganizationId, session.UserId });
  }
}
