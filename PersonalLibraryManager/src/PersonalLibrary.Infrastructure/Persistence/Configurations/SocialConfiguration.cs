using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalLibrary.Domain.Social;
using PersonalLibrary.Domain.Social.Enums;
using PersonalLibrary.Infrastructure.Identity;

namespace PersonalLibrary.Infrastructure.Persistence.Configurations;

public sealed class UserFriendConfiguration : IEntityTypeConfiguration<UserFriend>
{
    public void Configure(EntityTypeBuilder<UserFriend> builder)
    {
        builder.ToTable("UserFriends");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status)
            .HasDefaultValue(FriendshipStatus.Pending).IsRequired();
        builder.Property(x => x.RequestedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.RequesterUserId, x.AddresseeUserId })
            .IsUnique();
        builder.HasIndex(x => x.RequesterUserId);
        builder.HasIndex(x => x.AddresseeUserId);
        builder.HasIndex(x => x.Status);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_UserFriends_DifferentUsers",
            "[RequesterUserId] <> [AddresseeUserId]"));

        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.AddresseeUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
