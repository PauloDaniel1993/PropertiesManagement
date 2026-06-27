using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alsappan.Infrastructure.Identity;

public sealed class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
{
  public void Configure(EntityTypeBuilder<UserInvitation> builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.ToTable("UserInvitations");
    builder.HasKey(invitation => invitation.Id);

    builder.Property(invitation => invitation.Id).HasEntityIdConversion();
    builder.Property(invitation => invitation.OrganizationId).HasOrganizationIdConversion();
    builder.Property(invitation => invitation.RoleId).HasEntityIdConversion();
    builder.Property(invitation => invitation.CreatedByUserId).HasNullableUserIdConversion();
    builder.Property(invitation => invitation.UpdatedByUserId).HasNullableUserIdConversion();
    builder.Property(invitation => invitation.DeletedByUserId).HasNullableUserIdConversion();
    builder.Property(invitation => invitation.AcceptedByUserId).HasNullableUserIdConversion();
    builder.Property(invitation => invitation.Email).HasMaxLength(254).IsRequired();
    builder.Property(invitation => invitation.NormalizedEmail).HasMaxLength(254).IsRequired();
    builder.Property(invitation => invitation.DisplayName).HasMaxLength(160).IsRequired();
    builder.Property(invitation => invitation.TokenHash).HasMaxLength(256).IsRequired();
    builder.Property(invitation => invitation.Status).HasMaxLength(32).IsRequired();
    builder.Property(invitation => invitation.ConcurrencyToken)
      .HasConversion(token => token.Value, value => new ConcurrencyToken(value))
      .HasMaxLength(64)
      .IsRequired();
    builder.Ignore(invitation => invitation.IsDeleted);

    builder.HasIndex(invitation => invitation.TokenHash).IsUnique();
    builder.HasIndex(invitation => new { invitation.OrganizationId, invitation.NormalizedEmail, invitation.Status });
  }
}
